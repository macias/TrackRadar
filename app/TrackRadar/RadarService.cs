using System;
using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using System.Threading;
using Geo;
using MathUnit;
using TrackRadar.Implementation;

namespace TrackRadar
{
    [Service]
    internal sealed partial class RadarService : Service, ILocationListener, IRadarService, ISignalCheckerService//, ISensorEventListener
    {
        private readonly object threadLock = new object();

        //private const double gpsAccuracy = 5; // meters
        private readonly Statistics statistics;
        private AlarmMaster alarmMaster;
        private AlarmSequencer alarmSequencer;
        private readonly ThreadSafe<IPreferences> __prefs;
        private IPreferences prefs => __prefs.Value;
        private GpsWatchdog gpsWatchdog;
        private LocationManager locationManager;
        private TimeStamper timeStamper;
        private WrapTimer TEST_timer;
        private RadarCore core;

        private HandlerThread handler;
        private RadarReceiver receiver;
        //   private LogFile serviceLog;
        private GpxWriter offTrackWriter;
        private GpxWriter crossroadsWriter;
        private int gpsLastStatus;
        private DisposableGuard guard;
        private int subscriptions;
        private GpxWriter traceWriter;
        private double longestUpdate;

        /*
private SensorManager sensorManager;
private long lastShakeTime;
private long mLastForce;
private int mShakeCount;
private float mLastX;
private float mLastY;
private float mLastZ;
private long mLastTime;
*/
        private bool hasSubscribers => this.subscriptions > 0;

        private TrackRadarApp app => (TrackRadarApp)Application;

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
            this.__prefs = new ThreadSafe<IPreferences>();
        }

        public override IBinder OnBind(Intent intent)
        {
            LogDebug(LogLevel.Error, $"{nameof(OnBind)} called");
            throw null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent _, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            try
            {
                this.guard = new DisposableGuard();
                this.subscriptions = 1;
                //  this.serviceLog = new LogFile(this, "service.log", DateTime.UtcNow.AddDays(-2));

                if (!(Java.Lang.Thread.DefaultUncaughtExceptionHandler is CustomExceptionHandler))
                    Java.Lang.Thread.DefaultUncaughtExceptionHandler
                        = new CustomExceptionHandler(Java.Lang.Thread.DefaultUncaughtExceptionHandler);//, this.serviceLog);

                /*{
                    // Get a sensor manager to listen for shakes
                    this.sensorManager = (SensorManager)GetSystemService(SensorService);

                    // Listen for shakes
                    Sensor accelerometer = sensorManager.GetDefaultSensor(SensorType.Accelerometer);
                    if (accelerometer != null)
                    {
                        sensorManager.RegisterListener(this, accelerometer, SensorDelay.Normal);
                    }
                }
                */

                this.offTrackWriter = new GpxWriter(this, "off-track.gpx", DateTime.UtcNow.AddDays(-2));
                this.crossroadsWriter = new GpxWriter(this, "crossroads.gpx", DateTime.UtcNow.AddDays(-2));

                this.handler = new HandlerThread("GPSHandler");
                this.handler.Start();

                this.timeStamper = new Implementation.TimeStamper();
                this.alarmMaster = new TrackRadar.Implementation.AlarmMaster(this.timeStamper);
                this.alarmSequencer = new AlarmSequencer(this, this.alarmMaster);

                loadPreferences();

                if (this.prefs.GpsDump)
                    this.traceWriter = new GpxWriter(this, "trace.gpx", DateTime.UtcNow.AddDays(-2));

                if (this.prefs.ShowTurnAhead)
                {
                    //this.TEST_timer = new WrapTimer(showTurnAhead);
                    //this.TEST_timer.Change(TimeSpan.FromSeconds(25), System.Threading.Timeout.InfiniteTimeSpan);
                }

                this.core = new RadarCore(this, alarmSequencer, timeStamper, app.TrackData,
                    totalClimbs: app.Prefs.TotalClimbs, app.Prefs.RidingDistance, app.Prefs.RidingTime, app.Prefs.TopSpeed);

                this.locationManager = (LocationManager)GetSystemService(Context.LocationService);

                this.gpsWatchdog = new GpsWatchdog(this, this.timeStamper);

                { // start tracking

                    this.statistics.Reset();
                    locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 0, 0, this, this.handler.Looper);
                }

                this.receiver = RadarReceiver.Create(this);
                receiver.UpdatePrefs += receiver_UpdatePrefs;
                receiver.InfoRequest += Receiver_InfoRequest;
                receiver.Subscribe += Receiver_Subscribe;
                receiver.Unsubscribe += Receiver_Unsubscribe;

                // preventing service from being killed by the system
                {
                    // https://stackoverflow.com/a/36018368/210342
                    LogDebug(LogLevel.Info, "Building notification");
                    Intent notificationIntent = new Intent(this, typeof(MainActivity));
                    // https://stackoverflow.com/questions/3378193/android-how-to-avoid-that-clicking-on-a-notification-calls-oncreate
                    notificationIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                    PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, notificationIntent, 0);
                    Notification notification = new Notification(Resource.Drawable.Icon, $"Starting {nameof(TrackRadar)} service");
                    // https://android.googlesource.com/platform/frameworks/support.git/+/f9fd97499795cd47473f0344e00db9c9837eea36/v4/gingerbread/android/support/v4/app/NotificationCompatGingerbread.java
                    notification.SetLatestEventInfo(this, nameof(TrackRadar), "Monitoring position...", pendingIntent);
                    StartForeground(1337, notification);
                }

                if (this.prefs.ShowTurnAhead)
                {
                    LogDebug(LogLevel.Info, $"CPU eval {Mather.MakeCpuBusy()}s");
                }

                LogDebug(LogLevel.Info, "service started (+testing log)");

            }
            catch (Exception ex)
            {
                LogDebug(LogLevel.Error, $"Error on start {ex}");
            }
            return StartCommandResult.Sticky;
        }

        private void showTurnAhead()
        {
            StartActivity(typeof(TurnAheadActivity));
        }

        /*
        // https://stackoverflow.com/questions/5271448/how-to-detect-shake-event-with-android
        public void OnSensorChanged(SensorEvent evt)
        {
           const int SHAKE_TIMEOUT = 500;
        const long min_time_between_shakes_secs = 1;
            const float shake_threshold = 2.5f; // m/S**2
            const float threshold_compare_value = (shake_threshold + SensorManager.GravityEarth) * (shake_threshold + SensorManager.GravityEarth);

            if (evt.Sensor.Type != SensorType.Accelerometer)
                return;

            long now = Stopwatch.GetTimestamp();

            if ((now - mLastForce) > SHAKE_TIMEOUT)
                mShakeCount = 0;

            float x = evt.Values[0];
            float y = evt.Values[1];
            float z = evt.Values[2];

            long diff = now - mLastTime;
            double acc = Math.Abs(SensorManager.GravityEarth - Math.Sqrt(Math.Pow(x - mLastX, 2) + Math.Pow(y - mLastY, 2) + Math.Pow(z - mLastZ, 2)));
            float speed = acc / diff * 10000;
            if (speed > FORCE_THRESHOLD)
            {
                if ((++mShakeCount >= SHAKE_COUNT) && (now - mLastShake > SHAKE_DURATION))
                {
                    mLastShake = now;
                    mShakeCount = 0;
                    logDebug(LogLevel.Verbose, "Shake, Rattle, and Roll");
                    alarms.Go(Alarm.PositiveAcknowledgement);
                }
                mLastForce = now;
            }
            mLastTime = now;
            this.mLastX = x;
            this.mLastY = y;
            this.mLastZ = z;
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {
            ;
        }*/

        private void Receiver_Subscribe(object sender, EventArgs e)
        {
            using (this.guard.TryEnter(out bool allowed))
            {
                if (!allowed)
                    return;

                int sub = Interlocked.Increment(ref this.subscriptions);
                this.logLocal(LogLevel.Verbose, $"Subscribing");
                if (sub != 1)
                    this.logLocal(LogLevel.Error, $"Something wrong with sub {sub}");
            }
        }

        private void Receiver_Unsubscribe(object sender, EventArgs e)
        {
            using (this.guard.TryEnter(out bool allowed))
            {
                if (!allowed)
                    return;

                int sub = Interlocked.Decrement(ref this.subscriptions);
                this.logLocal(LogLevel.Verbose, $"Unsubscribing");
                if (sub != 0)
                    this.logLocal(LogLevel.Error, $"Something wrong with unsub {sub}");
            }
        }

        private void Receiver_InfoRequest(object sender, EventArgs _)
        {
            using (this.guard.TryEnter(out bool allowed))
            {
                if (!allowed)
                    return;

                logLocal(LogLevel.Verbose, "Received info request");
                if (this.gpsWatchdog.HasGpsSignal)
                {
                    lock (this.threadLock)
                    {
                        MainReceiver.SendDistance(this, statistics.FenceDistance,
                                totalClimbs: core.TotalClimbsReadout, ridingDistance: core.RidingDistanceReadout, ridingTime: core.RidingTimeReadout, topSpeed: core.TopSpeedReadout);
                    }
                }
                else
                    MainReceiver.SendAlarm(this, Message.NoSignalText);
            }
        }

        private void loadPreferences()
        {
            IPreferences p = app.Prefs;//. Preferences.Load(this);

            this.__prefs.Value = p;

            this.alarmMaster.Reset(p.UseVibration ? new AlarmVibrator((Vibrator)GetSystemService(Context.VibratorService)) : null,
                p.OffTrackAudioVolume > 0 ? Common.CreateMediaPlayer(this, AlarmSound.OffTrack, p.DistanceAudioFileName, Preferences.OffTrackDefaultAudioId) : null,
                p.GpsLostAudioVolume > 0 ? Common.CreateMediaPlayer(this, AlarmSound.GpsLost, p.GpsLostAudioFileName, Preferences.GpsLostDefaultAudioId) : null,
                p.AcknowledgementAudioVolume > 0 ? Common.CreateMediaPlayer(this, AlarmSound.BackOnTrack, p.GpsOnAudioFileName, Preferences.GpsOnDefaultAudioId) : null,
                disengage: p.DisengageAudioVolume > 0 ? Common.CreateMediaPlayer(this, AlarmSound.Disengage, p.DisengageAudioFileName, Preferences.DisengageDefaultAudioId) : null,
                p.TurnAheadAudioVolume > 0 ? Common.CreateMediaPlayer(this, AlarmSound.Crossroad, p.TurnAheadAudioFileName, Preferences.CrossroadsDefaultAudioId) : null,
                goAhead: p.TurnAheadAudioVolume > 0 ? Common.CreateMediaPlayer(this, AlarmSound.GoAhead, p.GoAheadAudioFileName, Preferences.GoAheadDefaultAudioId) : null,
                leftEasy: p.TurnAheadAudioVolume > 0 ? Common.CreateMediaPlayer(this, AlarmSound.LeftEasy, p.LeftEasyAudioFileName, Preferences.LeftEasyDefaultAudioId) : null,
                leftCross: p.TurnAheadAudioVolume > 0 ? Common.CreateMediaPlayer(this, AlarmSound.LeftCross, p.LeftCrossAudioFileName, Preferences.LeftCrossDefaultAudioId) : null,
                leftSharp: p.TurnAheadAudioVolume > 0 ? Common.CreateMediaPlayer(this, AlarmSound.LeftSharp, p.LeftSharpAudioFileName, Preferences.LeftSharpDefaultAudioId) : null,
                rightEasy: p.TurnAheadAudioVolume > 0 ? Common.CreateMediaPlayer(this, AlarmSound.RightEasy, p.RightEasyAudioFileName, Preferences.RightEasyDefaultAudioId) : null,
                rightCross: p.TurnAheadAudioVolume > 0 ? Common.CreateMediaPlayer(this, AlarmSound.RightCross, p.RightCrossAudioFileName, Preferences.RightCrossDefaultAudioId) : null,
                rightSharp: p.TurnAheadAudioVolume > 0 ? Common.CreateMediaPlayer(this, AlarmSound.RightSharp, p.RightSharpAudioFileName, Preferences.RightSharpDefaultAudioId) : null
                );
        }
        private void receiver_UpdatePrefs(object sender, EventArgs e)
        {
            using (this.guard.TryEnter(out bool allowed))
            {
                if (!allowed)
                    return;

                LogDebug(LogLevel.Verbose, "updating prefs");
                loadPreferences();
            }
        }

        public override void OnLowMemory()
        {
            LogDebug(LogLevel.Info, "OnLowMemory called");

            base.OnLowMemory();
        }

        public override void OnDestroy()
        {
            try
            {
                LogDebug(LogLevel.Info, "OnDestroy: before guard");

                this.guard.Dispose();

                LogDebug(LogLevel.Info, "OnDestroy: before stop");

                StopForeground(removeNotification: true);

                //sensorManager.UnregisterListener(this);

                LogDebug(LogLevel.Info, "OnDestroy: disposing signal");
                this.gpsWatchdog.Dispose();

                LogDebug(LogLevel.Verbose, "removing events handlers");

                this.receiver.UpdatePrefs -= receiver_UpdatePrefs;
                this.receiver.InfoRequest -= Receiver_InfoRequest;
                this.receiver.Subscribe -= Receiver_Subscribe;
                this.receiver.Unsubscribe -= Receiver_Unsubscribe;

                LogDebug(LogLevel.Verbose, "unregistering receiver");

                UnregisterReceiver(this.receiver);
                this.receiver = null;

                LogDebug(LogLevel.Verbose, "removing GPS updates");

                locationManager.RemoveUpdates(this);

                LogDebug(LogLevel.Verbose, "disposing alarms");

                this.alarmMaster.Dispose();

                LogDebug(LogLevel.Verbose, "disposing handler");

                this.handler.Dispose();

                LogDebug(LogLevel.Info, "OnDestroy: disposing test timer");

                this.TEST_timer?.Dispose();

                LogDebug(LogLevel.Verbose, $"service destroyed {statistics}");

                this.offTrackWriter.Dispose();
                this.crossroadsWriter.Dispose();
                this.traceWriter?.Dispose();

                LogDebug(LogLevel.Info, "OnDestroy: saving stats");

                lock (this.threadLock)
                {
                    this.app.Prefs.SaveRideStatistics(this, totalClimbs: core.TotalClimbsReadout, ridingDistance: core.RidingDistanceReadout,
                        ridingTime: core.RidingTimeReadout, topSpeed: core.TopSpeedReadout);
                }

                LogDebug(LogLevel.Info, "OnDestroy: done");

                /*{
                    IDisposable disp = this.serviceLog;
                    this.serviceLog = null;
                    disp.Dispose();
                }*/

                base.OnDestroy();
            }
            catch (Exception ex)
            {
                LogDebug(LogLevel.Error, $"exception during destroying service {ex}");
            }
        }

        public void OnLocationChanged(Location location)
        {
            using (this.guard.TryEnter(out bool allowed))
            {
                if (!allowed)
                    return;

                //LogDebug(LogLevel.Verbose, $"new loc {locationToString(location)}");
                this.traceWriter?.WriteLocation(latitudeDegrees: location.Latitude, longitudeDegrees: location.Longitude,
                    altitudeMeters: location.HasAltitude ? location.Altitude : (double?)null,
                    accuracyMeters: location.HasAccuracy ? location.Accuracy : (double?)null);

                if (!statistics.CanUpdate())
                {
                    // don't alarm because we have already computing distance and it will sends the proper info
                    // about if GPS-on alarm is OK
                    //this.signalChecker.UpdateGpsIsOn();
                    //LogDebug(LogLevel.Verbose, $"[TEMP] CANNOT UPDATE");
                    return;
                }

                double dist = 0;
                lock (this.threadLock)
                {
                    try
                    {
                        using (this.alarmSequencer.OpenAlarmContext(gpsAcquired: this.gpsWatchdog.UpdateGpsIsOn(),
                            hasGpsSignal: this.gpsWatchdog.HasGpsSignal))
                        {
                            long start = timeStamper.GetTimestamp();
                            dist = this.core.UpdateLocation(GeoPoint.FromDegrees(latitude: location.Latitude, longitude: location.Longitude),
                                altitude: location.HasAltitude ? Length.FromMeters(location.Altitude) : (Length?)null,
                                accuracy: location.HasAccuracy ? Length.FromMeters(location.Accuracy) : (Length?)null);
                            double passed = timeStamper.GetSecondsSpan(start);
                            if (this.longestUpdate < passed)
                            {
                                if (longestUpdate != 0)
                                    LogDebug(LogLevel.Verbose, $"loc update at {location.Latitude},{location.Longitude} took {(passed.ToString("0.####"))}s");
                                longestUpdate = passed;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug(LogLevel.Error, ex.Message);
                        offTrackWriter.WriteLocation(latitudeDegrees: location.Latitude, longitudeDegrees: location.Longitude, name: "crash");
                    }

                    // alarm about GPS only if there is no off-track alarm
                    /*if (this.gpsWatchdog.UpdateGpsIsOn() && dist <= 0)
                    {
                        // todo: here we could be resting, so there should be different alarm, so user is not confused
                        // add tests for this
                        bool played = alarms.TryPlay(Alarm.PositiveAcknowledgement, out string reason);
                        LogDebug(LogLevel.Info, $"ACK played {played}, reason {reason}, GPS back on");
                    }*/

                    statistics.UpdateCompleted(dist, location.Accuracy);
                    if (hasSubscribers)
                        MainReceiver.SendDistance(this, statistics.FenceDistance,
                            totalClimbs: core.TotalClimbsReadout, ridingDistance: core.RidingDistanceReadout, ridingTime: core.RidingTimeReadout, topSpeed: core.TopSpeedReadout);
                }
            }
        }


        private string locationToString(Location location)
        {
            return $"{(location.Latitude.ToString(RadarCore.GeoPointFormat))}, {(location.Longitude.ToString(RadarCore.GeoPointFormat))}, acc: {(location.HasAccuracy ? location.Accuracy.ToString("0.##") : "?")}, dt {Common.FormatShortDateTime(Common.FromTimeStampMs(location.Time))}, hw: {timeStamper.GetSecondsSpan(core.StartedAt)}s";
        }


        private void logLocal(LogLevel level, string message)
        {
            try
            {
                decorateMessage(ref message);

                Common.Log(level, message);
                //if (level > LogLevel.Verbose)
                //  this.serviceLog?.WriteLine(level, message);
            }
            catch (Exception ex)
            {
                Common.Log(LogLevel.Error, $"CRASH {nameof(logLocal)} {ex}");
            }
        }

        public void OnProviderDisabled(string provider)
        {
            using (this.guard.TryEnter(out bool allowed))
            {
                if (!allowed)
                    return;

                LogDebug(LogLevel.Verbose, "GPS provider switched to disabled");
            }
        }

        public void OnProviderEnabled(string provider)
        {
            using (this.guard.TryEnter(out bool allowed))
            {
                if (!allowed)
                    return;

                LogDebug(LogLevel.Verbose, "GPS provided switched to enabled");
            }
        }

        private static void decorateMessage(ref string message)
        {
            const string prefix = nameof(RadarService);
            if (!message.StartsWith(prefix))
                message = $"{prefix} {message}";
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            using (this.guard.TryEnter(out bool allowed))
            {
                if (!allowed)
                    return;

                if (provider == LocationManager.GpsProvider && System.Threading.Interlocked.Exchange(ref this.gpsLastStatus, (int)status) != (int)status)
                    LogDebug(LogLevel.Verbose, $"{provider} change on service {status}");
            }
        }

        void IRadarService.WriteCrossroad(double latitudeDegrees, double longitudeDegrees)
        {
            crossroadsWriter.WriteLocation(latitudeDegrees: latitudeDegrees, longitudeDegrees: longitudeDegrees);
        }

        void IRadarService.WriteOffTrack(double latitudeDegrees, double longitudeDegrees, string name)
        {
            offTrackWriter.WriteLocation(latitudeDegrees: latitudeDegrees, longitudeDegrees: longitudeDegrees, name: name);
        }

        public void LogDebug(LogLevel level, string message)
        {
            try
            {
                logLocal(level, message);
                if (this.hasSubscribers)
                    MainReceiver.SendDebug(this, message);
            }
            catch (Exception ex)
            {
                Common.Log(LogLevel.Error, $"CRASH logDebug {ex}");
            }
        }

        /*bool IRadarService.TryAlarm(Alarm alarm, out string reason)
        {
            return alarms.TryPlay(alarm, out reason);
        }*/

        ITimer ISignalCheckerService.CreateTimer(Action callback)
        {
            return new WrapTimer(callback);
        }

        void ISignalCheckerService.GpsOffAlarm(string message)
        {
            LogDebug(LogLevel.Warning, $"GPS OFF {message}");
            if (this.hasSubscribers)
                MainReceiver.SendAlarm(this, Message.NoSignalText);

            if (!alarmMaster.TryAlarm(Alarm.GpsLost, out string reason))
                LogDebug(LogLevel.Error, $"GPS lost alarm didn't play, reason {reason}");
        }

        void ISignalCheckerService.Log(LogLevel level, string message)
        {
            LogDebug(level, message);
        }

        TimeSpan IRadarService.OffTrackAlarmInterval => this.prefs.OffTrackAlarmInterval;
        TimeSpan IRadarService.TurnAheadAlarmInterval => this.prefs.TurnAheadAlarmInterval;
        Length IRadarService.OffTrackAlarmDistance => this.prefs.OffTrackAlarmDistance;
        TimeSpan IRadarService.TurnAheadAlarmDistance => this.prefs.TurnAheadAlarmDistance;
        Speed IRadarService.RestSpeedThreshold => this.prefs.RestSpeedThreshold;
        Speed IRadarService.RidingSpeedThreshold => this.prefs.RidingSpeedThreshold;
        /*bool IRadarService.TryGetLatestTurnAheadAlarmAt(out long timeStamp)
        {
            return this.alarms.TryGetLatestTurnAheadAlarmAt(out timeStamp);
        }*/


        TimeSpan ISignalCheckerService.NoGpsFirstTimeout => this.prefs.NoGpsAlarmFirstTimeout;
        TimeSpan ISignalCheckerService.NoGpsAgainInterval => this.prefs.NoGpsAlarmAgainInterval;
    }
}