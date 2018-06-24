//#define MOCK

using System;
using System.Collections.Generic;
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
    internal sealed partial class RadarService : Service, ILocationListener
    {
        private const double gpsAccuracy = 5; // meters
        private const string geoPointFormat = "0.000000";

        private readonly Statistics statistics;
        private readonly ServiceAlarms alarms;
        private readonly ThreadSafe<Preferences> prefs;
        private IReadOnlyList<GpxTrackSegment> trackSegments;
        private SignalTimer signalTimer;

#if MOCK
        private LocationManagerMock locationManager;
#else
        private LocationManager locationManager;
#endif
        private long lastAlarmAt;
        private RoundQueue<TimedGeoPoint> lastPoints;
        private bool wasRiding;
        private bool lastOnTrack;
        private HandlerThread handler;
        private ServiceReceiver receiver;
        private LogFile serviceLog;
        private HotWriter offTrackWriter;
        private int gpsLastStatus;

        public RadarService()
        {
            StrictMode.SetThreadPolicy(new StrictMode.ThreadPolicy.Builder()
                .DetectAll()
                .PenaltyLog()
                .Build());
            StrictMode.SetVmPolicy(new StrictMode.VmPolicy.Builder()
                       .DetectAll()
                       .PenaltyLog()
                       .Build());

            this.statistics = new Statistics();
            this.alarms = new ServiceAlarms();
            this.prefs = new ThreadSafe<Preferences>();
            // keeping window of 3 points seems like a good balance for measuring travelled distance (and speed)
            // too wide and we will not get proper speed value when rapidly stopping, 
            // too small and gps accurracy will play major role
            this.lastPoints = new RoundQueue<TimedGeoPoint>(size: 3);
        }

        public override IBinder OnBind(Intent intent)
        {
            logDebug(LogLevel.Error, $"{nameof(OnBind)} called");
            throw null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            this.serviceLog = new LogFile(this, "service.log", DateTime.UtcNow.AddDays(-2));

            if (!(Java.Lang.Thread.DefaultUncaughtExceptionHandler is CustomExceptionHandler))
                Java.Lang.Thread.DefaultUncaughtExceptionHandler 
                    = new CustomExceptionHandler(Java.Lang.Thread.DefaultUncaughtExceptionHandler, this.serviceLog);

            {
                this.offTrackWriter = new HotWriter(this, "off-track.gpx", DateTime.UtcNow.AddDays(-2), out bool appened);
                if (!appened)
                {
                    this.offTrackWriter.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    this.offTrackWriter.WriteLine("<gpx");
                    this.offTrackWriter.WriteLine("version=\"1.0\"");
                    this.offTrackWriter.WriteLine("creator=\"TrackRadar https://github.com/macias/TrackRadar\"");
                    this.offTrackWriter.WriteLine("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
                    this.offTrackWriter.WriteLine("xmlns=\"http://www.topografix.com/GPX/1/0\"");
                    this.offTrackWriter.WriteLine("xsi:schemaLocation=\"http://www.topografix.com/GPX/1/0 http://www.topografix.com/GPX/1/0/gpx.xsd\">");
                    this.offTrackWriter.WriteLine("<!-- CLOSE gpx TAG MANUALLY -->");
                }
            }

            this.handler = new HandlerThread("GPSHandler");
            this.handler.Start();

            {
                long now = Stopwatch.GetTimestamp();
                this.trackSegments = Common.ReadGpx(Preferences.LoadTrackFileName(this));
                logDebug(LogLevel.Info, $"{trackSegments.Count} segs, with {trackSegments.Select(it => it.TrackPoints.Count()).Sum()} points in {(Stopwatch.GetTimestamp() - now - 0.0) / Stopwatch.Frequency}s");
            }

#if MOCK
            this.locationManager = new LocationManagerMock(trackSegments.First().TrackPoints.Last());
#else
            this.locationManager = (LocationManager)GetSystemService(Context.LocationService);
#endif

            loadPreferences();

            { // start tracking
                this.lastAlarmAt = 0;
                this.statistics.Reset();
                locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 0, 0, this, this.handler.Looper);
            }

            // push Android to update us with gps positions
            getLastKnownPosition();

            this.signalTimer = new SignalTimer(logDebug,
                () => prefs.Value.NoGpsAlarmFirstTimeout,
                () => prefs.Value.NoGpsAlarmAgainInterval,
                () => alarms.Go(Alarm.PositiveAcknowledgement),
              () =>
              {
                  logDebug(LogLevel.Verbose, "gps off");
                  MainReceiver.SendAlarm(this, Message.NoSignalText);
                  if (!alarms.Go(Alarm.GpsLost))
                      logDebug(LogLevel.Error, "Audio alarm didn't started");

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

            logDebug(LogLevel.Info, "service started (+testing log)");

            return StartCommandResult.Sticky;
        }

        private void getLastKnownPosition()
        {
            logDebug(LogLevel.Verbose, "Requesting last known position");
            Location loc = this.locationManager.GetLastKnownLocation(LocationManager.GpsProvider);
            if (loc == null)
                logDebug(LogLevel.Verbose, $"didn't receive any location");
            else
                logDebug(LogLevel.Verbose, $"last known pos {locationToString(loc)}");
        }

        private void Receiver_InfoRequest(object sender, EventArgs e)
        {
            logLocal(LogLevel.Verbose, "Received info request");
            if (this.signalTimer.HasGpsSignal)
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
                p.AudioGpsOnEnabled ? Common.CreateMediaPlayer(this, p.GpsOnAudioFileName, Preferences.GpsOnDefaultAudioId) : null,
                p.AudioCrossroadsEnabled ? Common.CreateMediaPlayer(this, p.CrossroadsAudioFileName, Preferences.CrossroadsDefaultAudioId) : null
                );
        }
        private void Receiver_UpdatePrefs(object sender, EventArgs e)
        {
            logDebug(LogLevel.Verbose, "updating prefs");
            loadPreferences();
        }

        public override void OnDestroy()
        {
            try
            {
                logDebug(LogLevel.Info, "destroying service");

                this.signalTimer.Dispose();

                logDebug(LogLevel.Verbose, "removing events handlers");

                this.receiver.UpdatePrefs -= Receiver_UpdatePrefs;
                this.receiver.InfoRequest -= Receiver_InfoRequest;

                logDebug(LogLevel.Verbose, "unregistering receiver");

                UnregisterReceiver(this.receiver);
                this.receiver = null;

                logDebug(LogLevel.Verbose, "removing GPS updates");

                locationManager.RemoveUpdates(this);

                logDebug(LogLevel.Verbose, "disposing alarms");

                this.alarms.Dispose();

                logDebug(LogLevel.Verbose, "disposing handler");

                this.handler.Dispose();

                logDebug(LogLevel.Verbose, "service destroyed " + statistics.ToString());

                this.serviceLog.Dispose();
                this.offTrackWriter.Dispose();

                base.OnDestroy();
            }
            catch (Exception ex)
            {
                logDebug(LogLevel.Error, $"exception during destroying service {ex}");
            }
        }

        public void OnLocationChanged(Location location)
        {
            //            logDebug(LogLevel.Verbose, $"new loc {locationToString(location)}");

            if (!statistics.CanUpdate())
            {
                // don't alarm because we have already computing distance and it will sends the proper info
                // about if GPS-on alarm is OK
                this.signalTimer.Update(canAlarm: false);
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
                this.signalTimer.Update(canAlarm: dist <= 0);

                statistics.UpdateCompleted(dist, location.Accuracy);
                MainReceiver.SendDistance(this, statistics.SignedDistance);
            }
        }

        private static string locationToString(Location location)
        {
            return $"{location.Latitude}, {location.Longitude},a: {(location.HasAccuracy ? location.Accuracy : 0f)}, dt {Common.FormatShortDateTime(Common.FromTimeStampMs(location.Time))}";
        }

        /// <returns>negative value means on track</returns>
        private double updateLocation(Location location)
        {
            double dist;
            long now = Stopwatch.GetTimestamp();
            var point = new TimedGeoPoint(ticks: now) { Latitude = location.Latitude, Longitude = location.Longitude };
            bool on_track = isOnTrack(point, location.Accuracy, out dist);

            Movement movement;
            if (!this.lastPoints.Any())
            {
                movement = Movement.Stopping;
            }
            else
            {
                TimedGeoPoint last_point = this.lastPoints.Peek();

                double time_s_passed = (now - last_point.Ticks) * 1.0 / Stopwatch.Frequency;

                const double avg_walking_speed = 1.5; // in m/s: https://en.wikipedia.org/wiki/Walking
                const double avg_riding_speed = 3.5; // in m/s (between erderly and average): https://en.wikipedia.org/wiki/Bicycle_performance
                double traveled = point.GetDistance(last_point).Meters;
                if (traveled < avg_walking_speed * time_s_passed)
                    movement = Movement.Stopping;
                else if (traveled > avg_riding_speed * time_s_passed)
                    movement = Movement.Riding;
                else
                    movement = Movement.Walking;
            }

            bool was_riding = this.wasRiding;
            // "stopping" resets "riding"
            if (movement != Movement.Walking)
                this.wasRiding = movement == Movement.Riding;

            bool last_on_track = this.lastOnTrack;
            this.lastOnTrack = on_track;

            this.lastPoints.Enqueue(point);

            if (on_track)
            {
                if (!last_on_track)
                {
                    bool played = alarms.Go(Alarm.PositiveAcknowledgement);
                    logDebug(LogLevel.Verbose, $"Back on track, played {played}");
                }
                else if (was_riding && movement == Movement.Stopping)
                    alarms.Go(Alarm.PositiveAcknowledgement);

                return dist;
            }

            // do not trigger alarm if we stopped moving
            if (movement == Movement.Stopping)
                return dist;

            var passed = (now - this.lastAlarmAt) * 1.0 / Stopwatch.Frequency;
            if (passed < prefs.Value.OffTrackAlarmInterval.TotalSeconds)
                return dist;

            // do NOT try to be smart, and check if we are closing to the any of the tracks, this is because in real life
            // we can be closing to parallel track however with some fence between us, so when we are off the track
            // we are OFF THE TRACK and alarm the user about it -- user has info about environment, she/he sees if it possible
            // to take a shortcut, we don't see a thing

            this.lastAlarmAt = now;

            alarms.Go(Alarm.OffTrack);
            // it should be easier to make a GPX file out of it (we don't create it here because service crashes too often)
            offTrackWriter.WriteLine($"<wpt lat=\"{location.Latitude}\" lon=\"{location.Longitude}\"/>");

            return dist;
        }

        private void logDebug(LogLevel level, string message)
        {
            try
            {
                logLocal(level, message);
                MainReceiver.SendDebug(this, message);
            }
            catch (Exception ex)
            {
                Common.Log($"CRASH logDebug {ex}");
            }
        }

        private void logLocal(LogLevel level, string message)
        {
            try
            {
                if (level > LogLevel.Verbose)
                    Common.Log(message);
                this.serviceLog.WriteLine(level, message);
            }
            catch (Exception ex)
            {
                Common.Log($"CRASH {nameof(logLocal)} {ex}");
            }
        }

        /// <param name="dist">negative value means on track</param>
        private bool isOnTrack(TimedGeoPoint point, float accuracy, out double dist)
        {
            dist = double.MaxValue;
            int closest_track = 0;
            int closest_segment = 0;

            //float accuracy_offset = Math.Max(0, location.Accuracy-statistics.Accuracy);

            for (int t = 0; t < trackSegments.Count; ++t)
            {
                GpxTrackSegment seg = trackSegments[t];
                for (int s = seg.TrackPoints.Count - 1; s > 0; --s)
                {
                    double d = Math.Max(0, point.GetDistanceToArcSegment(seg.TrackPoints[s - 1],
                        seg.TrackPoints[s]).Meters - accuracy);

                    if (dist > d)
                    {
                        dist = d;
                        closest_track = t;
                        closest_segment = s;
                    }

                    if (d <= prefs.Value.OffTrackAlarmDistance)
                    {
                        //logDebug(LogLevel.Verbose, $"On [{s}]" + d.ToString("0.0") + " (" + seg.TrackPoints[s - 1].ToString(geoPointFormat) + " -- "
                        //  + seg.TrackPoints[s].ToString(geoPointFormat) + ") in " + watch.Elapsed.ToString());
                        dist = -dist;
                        return true;
                    }
                }
            }


            //this.serviceLog.WriteLine(LogLevel.Verbose, $"dist {dist.ToString("0.0")} point {point.ToString(geoPointFormat)}"
            //  + $" segment {trackSegments[closest_track].TrackPoints[closest_segment - 1].ToString(geoPointFormat)}"
            // + $" -- {trackSegments[closest_track].TrackPoints[closest_segment].ToString(geoPointFormat)}");
            //logDebug(LogLevel.Verbose, $"Off [{closest_segment}]" + dist.ToString("0.0") + " in " + watch.Elapsed.ToString());
            return false;
        }

        public void OnProviderDisabled(string provider)
        {
            logDebug(LogLevel.Verbose, "GPS OFF on service");
        }

        public void OnProviderEnabled(string provider)
        {
            logDebug(LogLevel.Verbose, "GPS ON on service");
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            if (provider == "gps" && System.Threading.Interlocked.Exchange(ref this.gpsLastStatus, (int)status) != (int)status)
                logDebug(LogLevel.Verbose, $"{provider} change on service {status}");
        }

    }
}