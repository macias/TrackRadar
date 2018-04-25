using System;
using System.Collections.Generic;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Gpx;
using System.Linq;
using System.Diagnostics;
using TrackRadar.Mocks;

namespace TrackRadar
{
    [Service]
    internal sealed class RadarService : Service, ILocationListener
    {
        private const double gpsAccuracy = 5; // meters
        private const string geoPointFormat = "0.000000";

        private readonly Statistics statistics;
        private readonly ServiceAlarms alarms;
        private readonly ThreadSafe<Preferences> prefs;
        private readonly ThreadSafe<IReadOnlyList<GpxTrackSegment>> trackSegments;
        private readonly ThreadSafe<SignalTimer> signalTimer;

        private LocationManager locationManager;
        //private LocationManagerMock locationManager;
        private long lastAlarmAt;
        private IGeoPoint lastOffTrackPoint;
        private HandlerThread handler;
        private ServiceReceiver receiver;
        private readonly ThreadSafe<LogFile> serviceLog;

        public RadarService()
        {
            this.statistics = new Statistics();
            this.alarms = new ServiceAlarms();
            this.prefs = new ThreadSafe<Preferences>();
            this.trackSegments = new ThreadSafe<IReadOnlyList<GpxTrackSegment>>();
            this.signalTimer = new ThreadSafe<SignalTimer>();
            this.serviceLog = new ThreadSafe<LogFile>();
        }

        public override IBinder OnBind(Intent intent)
        {
            throw null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            this.serviceLog.Value = new LogFile(this, "service.log", DateTime.UtcNow.AddDays(-2));

            this.handler = new HandlerThread("GPSHandler");
            this.handler.Start();

            this.trackSegments.Value = readGpx(Preferences.LoadTrackFileName(this));
            logDebug(LogLevel.Info, trackSegments.Value.Count.ToString() + " segs, with "
                + trackSegments.Value.Select(it => it.TrackPoints.Count()).Sum() + " points");

            this.locationManager = (LocationManager)GetSystemService(Context.LocationService);
            // this.locationManager = new LocationManagerMock(trackSegments.Value.First().TrackPoints.EndPoint);

            loadPreferences();

            { // start tracking
                Interlocked.Exchange(ref this.lastAlarmAt, 0);
                this.statistics.Reset();
                locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 0, 0, this, this.handler.Looper);
            }

            this.signalTimer.Value = new SignalTimer(logDebug,
                () => prefs.Value.NoGpsAlarmFirstTimeout,
                () => prefs.Value.NoGpsAlarmAgainInterval,
                () => alarms.Go(Alarm.GpsOn),
              () =>
              {
                  logDebug(LogLevel.Info, "gps off");
                  MainReceiver.SendAlarm(this, Message.NoSignalText);
                  alarms.Go(Alarm.GpsLost);

                  // weird, but RequestLocationUpdates does not force GPS provider to actually start providing updates
                  // thus such try -- we will see if requesting single update will start it
                  getLastKnownPosition();
                  // this gets OK location but is to weak to force GPS to start updating, if above will not work
                  // we would have to manually request update one after another and rework alarms
                  //this.locationManager.RequestSingleUpdate(LocationManager.GpsProvider, this, this.handler.Looper);
              });

            this.receiver = ServiceReceiver.Create(this);
            receiver.UpdatePrefs += Receiver_UpdatePrefs;
            receiver.InfoRequest += Receiver_InfoRequest;

            // push Android to update us with gps positions
            getLastKnownPosition();

            logDebug(LogLevel.Info, "service started");

            return StartCommandResult.Sticky;
        }

        private void getLastKnownPosition()
        {
            logDebug(LogLevel.Info, "Requesting last known position");
            Location loc = this.locationManager.GetLastKnownLocation(LocationManager.GpsProvider);
            if (loc == null)
                logDebug(LogLevel.Info, $"didn't receive any location");
            else
                logDebug(LogLevel.Info, $"last known pos {locationToString(loc)}");
        }

        private void Receiver_InfoRequest(object sender, EventArgs e)
        {
            if (this.signalTimer.Value.HasGpsSignal)
                MainReceiver.SendDistance(this, statistics.SignedDistance);
            else
                MainReceiver.SendAlarm(this, Message.NoSignalText);
        }

        private void loadPreferences()
        {
            var p = Preferences.Load(this);

            this.prefs.Value = p;

            this.alarms.Reset(p.UseVibration ? (Vibrator)GetSystemService(Context.VibratorService) : null,
                p.AudioDistanceEnabled ? Common.CreateMediaPlayer(this, p.DistanceAudioFileName, Preferences.OffTrackDefaultAudioId) : null,
                p.AudioGpsLostEnabled ? Common.CreateMediaPlayer(this, p.GpsLostAudioFileName, Preferences.GpsLostDefaultAudioId) : null,
                p.AudioGpsOnEnabled ? Common.CreateMediaPlayer(this, p.GpsOnAudioFileName, Preferences.GpsOnDefaultAudioId) : null
                );
        }
        private void Receiver_UpdatePrefs(object sender, EventArgs e)
        {
            logDebug(LogLevel.Info, "updating prefs");
            loadPreferences();
        }

        public override void OnDestroy()
        {
            try
            {
                logDebug(LogLevel.Info, "destroying service");

                this.signalTimer.Value.Dispose();

                logDebug(LogLevel.Info, "removing events handlers");

                this.receiver.UpdatePrefs -= Receiver_UpdatePrefs;
                this.receiver.InfoRequest -= Receiver_InfoRequest;

                logDebug(LogLevel.Info, "unregistering receiver");

                UnregisterReceiver(this.receiver);
                this.receiver = null;

                logDebug(LogLevel.Info, "removing GPS updates");

                locationManager.RemoveUpdates(this);

                logDebug(LogLevel.Info, "disposing alarms");

                this.alarms.Dispose();

                logDebug(LogLevel.Info, "disposing handler");

                this.handler.Dispose();

                logDebug(LogLevel.Info, "service destroyed " + statistics.ToString());

                this.serviceLog.Value.Dispose();

                base.OnDestroy();
            }
            catch (Exception ex)
            {
                logDebug(LogLevel.Error, $"exception during destroying service {ex}");
            }
        }

        public void OnLocationChanged(Location location)
        {
            logDebug(LogLevel.Info, $"new loc {locationToString(location)}");

            if (!statistics.CanUpdate())
            {
                // don't alarm because we have already computing distance and it will sends the proper info
                // about if GPS-on alarm is OK
                this.signalTimer.Value.Update(canAlarm: false);
                return;
            }

            double dist = 0;
            try
            {
                dist = updateLocation(location);
            }
            finally
            {
                // alarm about GPS only if there is no off-track alarm
                this.signalTimer.Value.Update(canAlarm: dist <= 0);

                statistics.UpdateCompleted(dist, location.Accuracy);
                MainReceiver.SendDistance(this, statistics.SignedDistance);
            }
        }

        private static string locationToString(Location location)
        {
            return $"{location.Latitude}, {location.Longitude},a: {(location.HasAccuracy ? location.Accuracy : 0f)}, dt {Common.FormatShortDateTime(Common.FromTimeStampMs(location.Time))}";
        }

        private double updateLocation(Location location)
        {
            double dist;
            var point = new GeoPoint() { Latitude = location.Latitude, Longitude = location.Longitude };
            bool on_track = isOnTrack(point, location.Accuracy, out dist);

            if (on_track)
                return dist;

            // do not trigger alarm if we stopped moving
            IGeoPoint last_point = Interlocked.CompareExchange(ref this.lastOffTrackPoint, null, null);
            if (last_point != null && point.GetDistance(last_point).Meters < gpsAccuracy)
                return dist;

            Interlocked.Exchange(ref this.lastOffTrackPoint, point);

            var now = Stopwatch.GetTimestamp();
            var passed = (now - Interlocked.CompareExchange(ref this.lastAlarmAt, 0, 0)) * 1.0 / Stopwatch.Frequency;
            if (passed < prefs.Value.OffTrackAlarmInterval.TotalSeconds)
                return dist;

            // do NOT try to be smart, and check if we are closing to the any of the tracks, this is because in real life
            // we can be closing to parallel track however with some fence between us, so when we are off the track
            // we are OFF THE TRACK and alarm the user about it -- user has info about environment, she/he sees if it possible
            // to take a shortcut, we don't see a thing

            Interlocked.Exchange(ref this.lastAlarmAt, now);

            alarms.Go(Alarm.OffTrack);

            return dist;
        }

        private void logDebug(LogLevel level, string message)
        {
            try
            {
                if (level > LogLevel.Info)
                    Common.Log(message);
                this.serviceLog.Value.WriteLine(level, message);
                MainReceiver.SendDebug(this, message);
            }
            catch (Exception ex)
            {
                Common.Log($"CRASH logDebug {ex}");
            }
        }

        private bool isOnTrack(IGeoPoint point, float accuracy, out double dist)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            dist = double.MaxValue;
            int closest_track = 0;
            int closest_segment = 0;

            //float accuracy_offset = Math.Max(0, location.Accuracy-statistics.Accuracy);

            for (int t = 0; t < trackSegments.Value.Count; ++t)
            {
                GpxTrackSegment seg = trackSegments.Value[t];
                for (int s = seg.TrackPoints.Count - 1; s > 0; --s)
                {
                    double d = Math.Max(0, point.GetDistanceToArcSegment(seg.TrackPoints[s - 1],
                        seg.TrackPoints[s]).Meters - accuracy);

                    if (dist > d)
                    {
                        dist = d;
                        closest_segment = s;
                        closest_track = t;
                    }
                    if (d <= prefs.Value.OffTrackAlarmDistance)
                    {
                        watch.Stop();
                        logDebug(LogLevel.Info, $"On [{s}]" + d.ToString("0.0") + " (" + seg.TrackPoints[s - 1].ToString(geoPointFormat) + " -- "
                            + seg.TrackPoints[s].ToString(geoPointFormat) + ") in " + watch.Elapsed.ToString());
                        dist = -dist;
                        return true;
                    }
                }
            }


            watch.Stop();
            this.serviceLog.Value.WriteLine(LogLevel.Info, $"dist {dist.ToString("0.0")} point {point.ToString(geoPointFormat)}"
                + $" segment {trackSegments.Value[closest_track].TrackPoints[closest_segment - 1].ToString(geoPointFormat)}"
                + $" -- {trackSegments.Value[closest_track].TrackPoints[closest_segment].ToString(geoPointFormat)}");
            logDebug(LogLevel.Info, $"Off [{closest_segment}]" + dist.ToString("0.0") + " in " + watch.Elapsed.ToString());
            return false;
        }

        public void OnProviderDisabled(string provider)
        {
            logDebug(LogLevel.Info, "GPS OFF on service");
        }

        public void OnProviderEnabled(string provider)
        {
            logDebug(LogLevel.Info, "GPS ON on service");
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            logDebug(LogLevel.Info, "GPS change on service " + status);
        }

        private static List<GpxTrackSegment> readGpx(string filename)
        {
            var result = new List<GpxTrackSegment>();

            using (var input = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                using (GpxReader reader = new GpxReader(input))
                {
                    while (reader.Read())
                    {
                        switch (reader.ObjectType)
                        {
                            case GpxObjectType.Metadata:
                                break;
                            case GpxObjectType.WayPoint:
                                break;
                            case GpxObjectType.Route:
                                break;
                            case GpxObjectType.Track:
                                result.AddRange(reader.Track.Segments);
                                break;
                        }
                    }

                }

            }

            return result;
        }
    }
}