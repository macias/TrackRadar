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

namespace TrackRadar
{
    // player.setDataSource(getAssets().openFD("raw/...").getFileDescriptor());
    [Activity(Label = "TrackRadar", MainLauncher = true, Icon = "@drawable/icon")]
    public sealed class MainActivity : ListActivity, GpsStatus.IListener
    {
        private const int SelectTrackCode = 1;

        private Button enableButton;
        private TextView trackFileNameTextView;
        private TextView gpsInfoTextView;
        private TextView trackInfoTextView;
        private TextView alarmInfoTextView;
        private TextView infoTextView;
        private ArrayAdapter<string> adapter;
        private Button trackButton;
        private bool debugMode;
        private LogFile log_writer;
        private GpsEvent lastGpsEvent_debug;

        private Intent radarIntent;
        private MainReceiver receiver;

        public MainActivity()
        {
        }

        protected override void OnCreate(Bundle bundle)
        {
            try
            {
                base.OnCreate(bundle);

                this.debugMode = Common.IsDebugMode(this);

                this.log_writer = new LogFile(this, "app.log", DateTime.UtcNow.AddDays(-2));

                log(LogLevel.Info, "app started");

                this.radarIntent = new Intent(this, typeof(RadarService));

                SetContentView(Resource.Layout.Main);

                this.enableButton = FindViewById<Button>(Resource.Id.EnableButton);
                this.trackFileNameTextView = FindViewById<TextView>(Resource.Id.TrackFileNameTextView);
                this.gpsInfoTextView = FindViewById<TextView>(Resource.Id.GpsInfoTextView);
                this.trackInfoTextView = FindViewById<TextView>(Resource.Id.TrackInfoTextView);
                this.alarmInfoTextView = FindViewById<TextView>(Resource.Id.AlarmInfoTextView);
                this.infoTextView = FindViewById<TextView>(Resource.Id.InfoTextView);
                this.adapter = new ArrayAdapter<string>(this, Resource.Layout.ListViewItem, new List<string>());
                ListAdapter = this.adapter;
                this.trackButton = FindViewById<Button>(Resource.Id.TrackButton);

                this.enableButton.Click += EnableButtonClicked;
                this.trackButton.Click += trackSelectionClicked;

                this.receiver = MainReceiver.Create(this);
                this.receiver.DistanceUpdate += Receiver_DistanceUpdate;
                this.receiver.AlarmUpdate += Receiver_AlarmUpdate;
                this.receiver.DebugUpdate += Receiver_DebugUpdate;

                this.log(LogLevel.Info, "Done OnCreate");
            }
            catch (Exception ex)
            {
                this.log(LogLevel.Error, "OnCreate " + ex.ToString());
            }

        }

        private void Receiver_DistanceUpdate(object sender, DistanceEventArgs e)
        {
            try
            {
                bool off_track = e.Distance > 0;
                this.infoTextView.SetTextColor(off_track ? Android.Graphics.Color.OrangeRed : Android.Graphics.Color.Green);
                this.infoTextView.Text = Math.Abs(e.Distance).ToString("0.0") + "m " + (off_track ? "off" : "on");
            }
            catch (Exception ex)
            {
                this.log(LogLevel.Error, "Receiver_DistanceUpdate " + ex.ToString());
            }

        }

        private void Receiver_AlarmUpdate(object sender, MessageEventArgs e)
        {
            try
            {
                showAlarm(e.Message);
                log(LogLevel.Info, "alarm: " + e.Message);
            }
            catch (Exception ex)
            {
                this.log(LogLevel.Error, "Receiver_AlarmUpdate " + ex.ToString());
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

        protected override void OnResume()
        {
            try
            {
                this.log(LogLevel.Info, "Entering OnResume");

                lastGpsEvent_debug = GpsEvent.Stopped;

                base.OnResume();

                log(LogLevel.Info, "RESUMED");
                LocationManager lm = (LocationManager)GetSystemService(Context.LocationService);
                lm.AddGpsStatusListener(this);

                if (updateReadiness()) // gps could be switched meanwhile
                {
                    showAlarm("running", Android.Graphics.Color.GreenYellow);
                    ServiceReceiver.SendInfoRequest(this);
                }

                this.log(LogLevel.Info, "Done OnResume");
            }
            catch (Exception ex)
            {
                this.log(LogLevel.Error, "OnResume " + ex.ToString());
            }

        }
        protected override void OnPause()
        {
            try
            {
                this.log(LogLevel.Info, "Entering OnPause");

                log(LogLevel.Info, "app paused");
                LocationManager lm = (LocationManager)GetSystemService(Context.LocationService);
                lm.RemoveGpsStatusListener(this);

                base.OnPause();

                this.log(LogLevel.Info, "Done OnPause");
            }
            catch (Exception ex)
            {
                this.log(LogLevel.Error, "OnPause " + ex.ToString());
            }

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
                    this.adapter.Insert(message, 0);
            }
            catch (Exception ex)
            {
                Common.Log($"CRASH logUI {ex}");
            }
        }

        private void log(LogLevel level, string message)
        {
            try
            {
                if (level> LogLevel.Info)
                Common.Log(message);
                log_writer?.WriteLine(level, message);
                logUI(message);
            }
            catch (Exception ex)
            {
                Common.Log($"CRASH log {ex}");
            }
        }

        protected override void OnDestroy()
        {
            try
            {
                log(LogLevel.Info, "app destroyed");

                this.receiver.AlarmUpdate -= Receiver_AlarmUpdate;
                this.receiver.DistanceUpdate -= Receiver_DistanceUpdate;
                this.receiver.DebugUpdate -= Receiver_DebugUpdate;
                UnregisterReceiver(this.receiver);

                log_writer?.Dispose();
                log_writer = null;

                base.OnDestroy();

                this.log(LogLevel.Info, "Done OnDestroy");
            }
            catch (Exception ex)
            {
                this.log(LogLevel.Error, "OnDestroy " + ex.ToString());
            }
        }

        private bool updateReadiness()
        {
            try
            {
                var prefs = Preferences.Load(this);
                this.alarmInfoTextView.Visibility = prefs.PrimaryAlarmEnabled ? ViewStates.Gone : ViewStates.Visible;
                string track = Preferences.LoadTrackFileName(this);
                bool track_enabled = track != null && System.IO.File.Exists(track);
                this.trackInfoTextView.Visibility = track_enabled ? ViewStates.Gone : ViewStates.Visible;
                this.trackFileNameTextView.Text = track;

                LocationManager lm = (LocationManager)GetSystemService(Context.LocationService);
                bool gps_enabled = lm.IsProviderEnabled(LocationManager.GpsProvider);
                log(LogLevel.Info, $"gps provider enabled {gps_enabled}");
                this.gpsInfoTextView.Visibility = gps_enabled ? ViewStates.Gone : ViewStates.Visible;

                bool is_running = isServiceRunning();
                bool can_start = (track_enabled && gps_enabled && prefs.PrimaryAlarmEnabled);
                this.enableButton.Enabled = is_running || can_start;
                this.enableButton.Text = Resources.GetString(is_running ? Resource.String.StopService : Resource.String.StartService);

                this.trackButton.Enabled = !is_running;

                if (!is_running)
                    showAlarm("inactive", Android.Graphics.Color.Blue);

                return is_running;
            }
            catch (Exception ex)
            {
                this.log(LogLevel.Error, "updateReadiness " + ex.ToString());
                throw;
            }
        }

        private void trackSelectionClicked(object sender, System.EventArgs e)
        {
            try
            {
                this.log(LogLevel.Info, "Entering trackSelectionClicked");

                var intent = new Intent();
                intent.SetType("application/gpx+xml");
                intent.SetAction(Intent.ActionGetContent);
                StartActivityForResult(
                    Intent.CreateChooser(intent, "Select gpx file"), SelectTrackCode);

                this.log(LogLevel.Info, "Done trackSelectionClicked");
            }
            catch (Exception ex)
            {
                this.log(LogLevel.Error, "trackSelectionClicked " + ex.ToString());
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent intent)
        {
            try
            {
                this.log(LogLevel.Info, "Entering OnActivityResult");

                base.OnActivityResult(requestCode, resultCode, intent);

                if (resultCode == Result.Ok && requestCode == SelectTrackCode)
                {
                    if (intent.Data != null && intent.Data.Path != null)
                    {
                        this.trackFileNameTextView.Text = intent.Data.Path;
                        Preferences.SaveTrackFileName(this, intent.Data.Path);

                        updateReadiness();
                    }
                }

                this.log(LogLevel.Info, "Done OnActivityResult");
            }
            catch (Exception ex)
            {
                this.log(LogLevel.Error, "OnActivityResult " + ex.ToString());
            }
        }

        private void EnableButtonClicked(object sender, System.EventArgs e)
        {
            try
            {
                this.log(LogLevel.Info, "ENABLE CLICKED: Checking if service is running");
                bool running = isServiceRunning();
                if (running)
                {
                    this.log(LogLevel.Info, "stopping service");
                    this.StopService(radarIntent);
                }
                else
                {
                    this.log(LogLevel.Info, "starting service service");
                    this.adapter.Clear();

                    showAlarm("running", Android.Graphics.Color.GreenYellow);

                    maxOutSystemVolume(Android.Media.VolumeNotificationFlags.PlaySound);

                    this.StartService(radarIntent);
                }
                this.log(LogLevel.Info, "updating UI");
                updateReadiness();

                this.log(LogLevel.Info, "Done EnableButtonClicked");
            }
            catch (Exception ex)
            {
                this.log(LogLevel.Error, $"We have error {ex.Message}");
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
                log(LogLevel.Info, "MA gps changed to " + e.ToString());
                updateReadiness();
            }
            else if (e != lastGpsEvent_debug) // prevents polluting log with SatelliteStatus value
            {
                log(LogLevel.Info, $"MA minor status gps changed to {e}");
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
                this.log(LogLevel.Info, "Entering OnCreateOptionsMenu");

                this.MenuInflater.Inflate(Resource.Menu.MainMenu, menu);

                this.log(LogLevel.Info, "Done OnCreateOptionsMenu");
            }
            catch (Exception ex)
            {
                this.log(LogLevel.Error, "OnCreateOptionsMenu " + ex.ToString());
            }

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            try
            {
                this.log(LogLevel.Info, "Entering OnOptionsItemSelected");
                // since we have only single item menu we blindly process further

                maxOutSystemVolume(new Android.Media.VolumeNotificationFlags());

                StartActivity(typeof(SettingsActivity));

                this.log(LogLevel.Info, "Done OnOptionsItemSelected");
            }
            catch (Exception ex)
            {
                this.log(LogLevel.Error, "OnOptionsItemSelected " + ex.ToString());
            }

            return true;
        }
    }
}

