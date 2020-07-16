using Android.App;
using Android.Widget;
using Android.OS;
using Android.Views;
using System;
using Android.Content;
using Android.Locations;
using Android.Runtime;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gpx;
using MathUnit;

namespace TrackRadar
{
    // player.setDataSource(getAssets().openFD("raw/...").getFileDescriptor());
    [Activity(Label = "TrackRadar", MainLauncher = true, Icon = "@drawable/icon")]
    public sealed class MainActivity : ListActivity, GpsStatus.IListener
    {
        private const int SelectTrackCode = 1;

        private Button enableButton;
        private TextView trackFileNameTextView;
        private TextView trackInfoTextView;
        private TextView gpsInfoTextView;
        private TextView alarmInfoTextView;
        private TextView ridingDistanceTextView;
        private TextView averageSpeedTextView;
        private TextView topSpeedTextView;
        private TextView totalClimbsTextView;
        private TextView infoTextView;
        private ArrayAdapter<string> adapter;
        private Button trackButton;
        private bool debugMode;
        private LogFile log_writer;
        private GpsEvent lastGpsEvent_debug;

        private Intent radarServiceIntent;
        private MainReceiver receiver;

        private TrackRadarApp app => (TrackRadarApp)Application;

        public MainActivity()
        {
        }

        // https://developer.android.com/guide/components/activities/activity-lifecycle
        // SHORT_LIFECYCLE
        // there are some parts marked with SHORT_LIFECYCLE prefix
        // normally I would put them in their respective places, but according to spec and according to real life it might happen that the app is killed rapidly
        // and then recreated. It happed when I didn't indicate for notification it should re-use existing activity so I guess it might happen again
        // Thus leaving the code prepared for short loop BEGIN-OnCreate-OnStart-OnResume-WORKING-OnPause-END/KILLED, second life: BEGIN-OnCreate-...

        protected override void OnCreate(Bundle bundle)
        {
            try
            {
                // https://stackoverflow.com/questions/704311/android-how-do-i-investigate-an-anr
                // https://stackoverflow.com/questions/5513457/anr-keydispatchingtimedout-error/5513623#5513623
                StrictMode.SetThreadPolicy(new StrictMode.ThreadPolicy.Builder()
                           .DetectAll()
                   .PenaltyLog()
                   .Build());
                StrictMode.SetVmPolicy(new StrictMode.VmPolicy.Builder()
                           .DetectAll()
                           .PenaltyLog()
                           .Build());

                base.OnCreate(bundle);

                this.debugMode = Common.IsDebugMode(this);

                logDebug(LogLevel.Info, "app started (+testing log)");

                this.radarServiceIntent = new Intent(this, typeof(RadarService));

                SetContentView(Resource.Layout.Main);

#if DEBUG
                this.Title = $"{nameof(TrackRadar)} DEBUG";
#endif

                this.enableButton = FindViewById<Button>(Resource.Id.EnableButton);
                this.trackFileNameTextView = FindViewById<TextView>(Resource.Id.TrackFileNameTextView);
                this.gpsInfoTextView = FindViewById<TextView>(Resource.Id.GpsInfoTextView);
                this.trackInfoTextView = FindViewById<TextView>(Resource.Id.TrackInfoTextView);
                this.alarmInfoTextView = FindViewById<TextView>(Resource.Id.AlarmInfoTextView);

                this.ridingDistanceTextView = FindViewById<TextView>(Resource.Id.RidingDistanceTextView);
                this.averageSpeedTextView = FindViewById<TextView>(Resource.Id.AverageSpeedTextView);
                this.topSpeedTextView = FindViewById<TextView>(Resource.Id.TopSpeedTextView);
                this.totalClimbsTextView = FindViewById<TextView>(Resource.Id.TotalClimbsTextView);

                this.infoTextView = FindViewById<TextView>(Resource.Id.InfoTextView);
                this.adapter = new ArrayAdapter<string>(this, Resource.Layout.ListViewItem, new List<string>());
                ListAdapter = this.adapter;
                this.trackButton = FindViewById<Button>(Resource.Id.TrackButton);

                this.enableButton.Click += EnableButtonClicked;
                this.trackButton.Click += trackSelectionClicked;

                //SHORT_LIFECYCLE_OnPartialCreatePart();

                //loadTrack();
                updateTrackInfo();
                updateStatistics(totalClimbs: app.Prefs.TotalClimbs, ridingDistance: app.Prefs.RidingDistance, ridingTime: app.Prefs.RidingTime, topSpeed: app.Prefs.TopSpeed);

                this.logDebug(LogLevel.Verbose, $"Done OnCreate");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, $"OnCreate {ex}");
            }

        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnResume()
        {
            try
            {
                this.logDebug(LogLevel.Verbose, "Entering OnResume");

                base.OnResume();

                SHORT_LIFECYCLE_OnPartialCreatePart();

                lastGpsEvent_debug = GpsEvent.Stopped;

                logDebug(LogLevel.Verbose, "RESUMED");
                LocationManager lm = (LocationManager)GetSystemService(Context.LocationService);
                lm.AddGpsStatusListener(this);

                if (this.isServiceRunning())
                {
                    this.receiver.DistanceUpdate += Receiver_DistanceUpdate;
                    ServiceReceiver.SendSubscribe(this);
                }
                this.receiver.AlarmUpdate += Receiver_AlarmUpdate;
                this.receiver.DebugUpdate += Receiver_DebugUpdate;

                {
                    updateReadiness(out bool is_service_running);
                    if (is_service_running) // gps could be switched meanwhile
                    {
                        showAlarm("running", Android.Graphics.Color.GreenYellow);
                        ServiceReceiver.SendInfoRequest(this);
                    }
                }

                this.logDebug(LogLevel.Verbose, "Done OnResume");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, $"OnResume {ex}");
            }

        }

        private void SHORT_LIFECYCLE_OnPartialCreatePart() 
        {
            this.log_writer = new LogFile(this, "app.log", DateTime.UtcNow.AddDays(-2));

            this.receiver = MainReceiver.Create(this);
            this.receiver.RegisterReceiver();
        }

        private void SHORT_LIFECYCLE_OnStopPart()
        {
            this.receiver.UnregisterReceiver();
            this.receiver = null;

            log_writer?.Dispose();
            log_writer = null;
        }

        private void OnPausePart()
        {
            if (this.isServiceRunning())
            {
                this.receiver.DistanceUpdate -= Receiver_DistanceUpdate;
                ServiceReceiver.SendUnsubscribe(this);
            }

            this.receiver.AlarmUpdate -= Receiver_AlarmUpdate;
            this.receiver.DebugUpdate -= Receiver_DebugUpdate;

            logDebug(LogLevel.Verbose, "app paused");
            LocationManager lm = (LocationManager)GetSystemService(Context.LocationService);
            lm.RemoveGpsStatusListener(this);
        }


        protected override void OnPause()
        {
            try
            {
                this.logDebug(LogLevel.Verbose, "Entering OnPause");

                OnPausePart();

                SHORT_LIFECYCLE_OnStopPart(); 

                base.OnPause();

                this.logDebug(LogLevel.Verbose, "Done OnPause");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, $"OnPause {ex}");
            }

        }

        protected override void OnStop()
        {
            try
            {
                this.logDebug(LogLevel.Verbose, "Entering OnStop");

                //SHORT_LIFECYCLE_OnStopPart(); 

                base.OnStop();

                this.logDebug(LogLevel.Verbose, "Done OnStop");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, $"OnStop {ex}");
            }

        }


        private void Receiver_DistanceUpdate(object sender, DistanceEventArgs e)
        {
            try
            {
                bool off_track = Double.IsInfinity(e.FenceDistance) || e.FenceDistance > 0;
                this.infoTextView.SetTextColor(off_track ? Android.Graphics.Color.OrangeRed : Android.Graphics.Color.Green);
                if (Double.IsInfinity(e.FenceDistance))
                    this.infoTextView.Text = "far away";
                else
                    this.infoTextView.Text = Math.Abs(e.FenceDistance).ToString("0.0") + "m " + (off_track ? "off" : "on");

                updateStatistics(totalClimbs: e.TotalClimbs, ridingDistance: e.RidingDistance, ridingTime: e.RidingTime, topSpeed: e.TopSpeed);
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, $"Receiver_DistanceUpdate {ex}");
            }

        }

        private void updateStatistics(Length totalClimbs, Length ridingDistance, TimeSpan ridingTime, Speed topSpeed)
        {
            this.totalClimbsTextView.Text = totalClimbs.Meters.ToString("0");
            this.ridingDistanceTextView.Text = ridingDistance.Kilometers.ToString("0.0");
            if (ridingTime == TimeSpan.Zero)
                this.averageSpeedTextView.Text = "0.0";
            else
                this.averageSpeedTextView.Text = (ridingDistance / ridingTime).KilometersPerHour.ToString("0.0");
            this.topSpeedTextView.Text = topSpeed.KilometersPerHour.ToString("0.0");
        }

        private void Receiver_AlarmUpdate(object sender, MessageEventArgs e)
        {
            try
            {
                showAlarm(e.Message);
                logDebug(LogLevel.Verbose, "alarm: " + e.Message);
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, $"Receiver_AlarmUpdate {ex}");
            }

        }

        private void showAlarm(string message)
        {
            showAlarm(message, Android.Graphics.Color.Red);
        }

        private void showAlarm(string message, Android.Graphics.Color color)
        {
            this.infoTextView.SetTextColor(color);
            this.infoTextView.Text = message;
        }

        private void Receiver_DebugUpdate(object sender, MessageEventArgs e)
        {
            logUI(e.Message);
        }

        private void logUI(string message)
        {
            try
            {
                if (this.debugMode && this.adapter != null)
                {
                    // limit UI to small number of items on the list
                    if (this.adapter.Count == 100)
                        this.adapter.Remove(this.adapter.GetItem(this.adapter.Count - 1));
                    this.adapter.Insert(message, 0);
                }
            }
            catch (Exception ex)
            {
                Common.Log(LogLevel.Error, $"CRASH logUI {ex}");
            }
        }

        private void logDebug(LogLevel level, string message)
        {
            try
            {
                Common.Log(level, message);
                log_writer?.WriteLine(level, message);
                logUI(message);
            }
            catch (Exception ex)
            {
                Common.Log(LogLevel.Error, $"CRASH log {ex}");
            }
        }


        private void updateReadiness(out bool isServiceRunning)
        {
            try
            {
                IPreferences prefs = app.Prefs;
                this.alarmInfoTextView.Visibility = prefs.AlarmsValid() ? ViewStates.Gone : ViewStates.Visible;

                isServiceRunning = this.isServiceRunning();

                // bool track_enabled = loadTrack();// allowFileReload, prefs, is_running);

                LocationManager lm = (LocationManager)GetSystemService(Context.LocationService);
                bool gps_enabled = lm.IsProviderEnabled(LocationManager.GpsProvider);
                logDebug(LogLevel.Verbose, $"gps provider enabled {gps_enabled}");
                this.gpsInfoTextView.Visibility = gps_enabled ? ViewStates.Gone : ViewStates.Visible;

                bool can_start = (app.TrackData != null && gps_enabled && prefs.AlarmsValid());
                this.enableButton.Enabled = isServiceRunning || can_start;
                this.enableButton.Text = Resources.GetString(isServiceRunning ? Resource.String.StopService : Resource.String.StartService);

                this.trackButton.Enabled = !isServiceRunning;

                if (!isServiceRunning)
                    showAlarm("inactive", Android.Graphics.Color.Blue);
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, $"updateReadiness {ex}");
                throw;
            }
        }

        /* private void loadTrack()
         {
             app.TrackData = null;

             string track_path = Preferences.LoadTrackFileName(this);
             bool track_enabled = track_path != null && System.IO.File.Exists(track_path);
             this.trackFileNameTextView.Text = track_path;

             if (!track_enabled)
             {
                 this.trackInfoTextView.Text = "Track is not available.";
             }
             else
             {
                 GpxData gpx_data = GpxLoader.ReadGpx(track_path,
                      Length.FromMeters(app.Prefs.OffTrackAlarmDistance),
                      ex => logDebug(LogLevel.Error, $"Error while loading GPX {ex.Message}"));

                 if (gpx_data.Segments.Any())
                     app.TrackData = gpx_data;
                 else
                 {
                     track_enabled = false;
                     this.trackInfoTextView.Text = "Empty track.";
                 }

                 logUI($"Found {gpx_data.Crossroads.Count()} crossroads");
             }

             this.trackInfoTextView.Visibility = track_enabled ? ViewStates.Gone : ViewStates.Visible;
         }*/

        private void trackSelectionClicked(object sender, System.EventArgs e)
        {
            try
            {
                this.logDebug(LogLevel.Verbose, "Entering trackSelectionClicked");

                using (var intent = new Intent())
                {
                    intent.SetType("application/gpx+xml");
                    intent.SetAction(Intent.ActionGetContent);
                    StartActivityForResult(
                        Intent.CreateChooser(intent, "Select gpx file"), SelectTrackCode);
                }

                this.logDebug(LogLevel.Verbose, "Done trackSelectionClicked");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, $"trackSelectionClicked {ex}");
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent intent)
        {
            try
            {
                this.logDebug(LogLevel.Verbose, "Entering OnActivityResult");

                base.OnActivityResult(requestCode, resultCode, intent);

                if (resultCode == Result.Ok && requestCode == SelectTrackCode)
                {
                    if (intent.Data != null && intent.Data.Path != null)
                    {
                        app.Prefs.SaveTrackFileName(this, intent.Data.Path);

                        app.LoadTrack(onError: ex => logDebug(LogLevel.Error, $"Error while loading GPX {ex.Message}"));
                        updateTrackInfo();
                        updateReadiness(out _);
                    }
                }

                this.logDebug(LogLevel.Verbose, "Done OnActivityResult");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, $"OnActivityResult {ex}");
            }
        }

        private void updateTrackInfo()
        {
            GpxData track_data = app.TrackData;
            if (track_data != null)
                logUI($"Found {track_data.Crossroads.Count()} crossroads");
            this.trackInfoTextView.Visibility = track_data != null ? ViewStates.Gone : ViewStates.Visible;
            this.trackFileNameTextView.Text = app.Prefs.TrackName;
            this.trackInfoTextView.Text = app.TrackInfo;
        }

        private void EnableButtonClicked(object sender, System.EventArgs e)
        {
            try
            {
                this.logDebug(LogLevel.Verbose, "ENABLE CLICKED: Checking if service is running");
                bool running = isServiceRunning();
                if (running)
                {
                    this.logDebug(LogLevel.Verbose, "stopping service");
                    this.receiver.DistanceUpdate -= Receiver_DistanceUpdate;
                    this.StopService(radarServiceIntent);
                }
                else
                {
                    this.logDebug(LogLevel.Verbose, "starting service service");

                    this.adapter.Clear();
                    this.receiver.DistanceUpdate += Receiver_DistanceUpdate;

                    showAlarm("running", Android.Graphics.Color.GreenYellow);

                    maxOutSystemVolume(Android.Media.VolumeNotificationFlags.PlaySound);

                    this.StartService(radarServiceIntent);
                }
                this.logDebug(LogLevel.Verbose, "updating UI");

                // we can block reloading file because service does not do loading on its own
                updateReadiness(out _);

                this.logDebug(LogLevel.Verbose, "Done EnableButtonClicked");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, $"We have error {ex.Message}");
                log_writer?.WriteLine(LogLevel.Error, $"{ex}");
            }
        }

        private void maxOutSystemVolume(Android.Media.VolumeNotificationFlags flags)
        {
            var mAudioManager = (Android.Media.AudioManager)GetSystemService(AudioService);
            mAudioManager.SetStreamVolume(
              Android.Media.Stream.Music, // Stream type
             mAudioManager.GetStreamMaxVolume(Android.Media.Stream.Music), // Index
              flags);
        }

        public void OnGpsStatusChanged([GeneratedEnum] GpsEvent e)
        {
            if (e == GpsEvent.Started || e == GpsEvent.Stopped)
            {
                logDebug(LogLevel.Info, $"MA gps changed to {e}");
                updateReadiness(out _);
            }
            else if (e != lastGpsEvent_debug) // prevents polluting log with SatelliteStatus value
            {
                logDebug(LogLevel.Verbose, $"MA minor status gps changed to {e}");
                this.lastGpsEvent_debug = e;
            }
        }

        private bool isServiceRunning()
        {
            // http://stackoverflow.com/a/42601635/210342

            string service_name = Java.Lang.Class.FromType(typeof(RadarService)).CanonicalName;

            var manager = (ActivityManager)this.GetSystemService(Context.ActivityService);
            var services_info = manager.GetRunningServices(int.MaxValue);
            foreach (ActivityManager.RunningServiceInfo info in services_info)
            {
                if (info.Service.ClassName == service_name)
                    return true;
            }

            return false;
        }
        /*        public override bool OnKeyDown(Keycode keyCode, KeyEvent eventx)
                {
                    if (keyCode == Keycode.Menu)
                    {
                        StartActivity(typeof(SettingsActivity));
                        //StartActivity(typeof(Settings2Activity));
                        return true;
                    }

                    return base.OnKeyDown(keyCode, eventx);
                }
                */
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            try
            {
                this.logDebug(LogLevel.Verbose, "Entering OnCreateOptionsMenu");

                this.MenuInflater.Inflate(Resource.Menu.MainMenu, menu);

                this.logDebug(LogLevel.Verbose, "Done OnCreateOptionsMenu");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, $"OnCreateOptionsMenu {ex}");
            }

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            try
            {
                this.logDebug(LogLevel.Verbose, "Entering OnOptionsItemSelected");
                // since we have only single item menu we blindly process further

                switch (item.ItemId)
                {
                    case Resource.Id.SettingsMenuItem:
                        maxOutSystemVolume(new Android.Media.VolumeNotificationFlags());
                        StartActivity(typeof(SettingsActivity));
                        break;

                    case Resource.Id.TurnAheadMenuItem:
                        StartActivity(typeof(TurnAheadActivity));
                        break;

                    case Resource.Id.ClearStatsMenuItem:
                        app.Prefs.SaveRideStatistics(this, Length.Zero, Length.Zero, TimeSpan.Zero, Speed.Zero);
                        updateStatistics(totalClimbs: app.Prefs.TotalClimbs, ridingDistance: app.Prefs.RidingDistance, ridingTime: app.Prefs.RidingTime, topSpeed: app.Prefs.TopSpeed);
                        break;
                }

                this.logDebug(LogLevel.Verbose, "Done OnOptionsItemSelected");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, $"OnOptionsItemSelected {ex}");
            }

            return true;
        }
    }
}

