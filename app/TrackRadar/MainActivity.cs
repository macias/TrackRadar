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

                this.log_writer = new LogFile(this, "app.log", DateTime.UtcNow.AddDays(-2));

                logDebug(LogLevel.Info, "app started (+testing log)");

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

                this.logDebug(LogLevel.Verbose, "Done OnCreate");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, "OnCreate " + ex.ToString());
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
                this.logDebug(LogLevel.Error, "Receiver_DistanceUpdate " + ex.ToString());
            }

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
                this.logDebug(LogLevel.Error, "Receiver_AlarmUpdate " + ex.ToString());
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
                this.logDebug(LogLevel.Verbose, "Entering OnResume");

                lastGpsEvent_debug = GpsEvent.Stopped;

                base.OnResume();

                logDebug(LogLevel.Verbose, "RESUMED");
                LocationManager lm = (LocationManager)GetSystemService(Context.LocationService);
                lm.AddGpsStatusListener(this);

                if (this.isServiceRunning())
                    ServiceReceiver.SendSubscribe(this);

                this.receiver.DistanceUpdate += Receiver_DistanceUpdate;
                this.receiver.AlarmUpdate += Receiver_AlarmUpdate;
                this.receiver.DebugUpdate += Receiver_DebugUpdate;

                if (updateReadiness()) // gps could be switched meanwhile
                {
                    showAlarm("running", Android.Graphics.Color.GreenYellow);
                    ServiceReceiver.SendInfoRequest(this);
                }

                this.logDebug(LogLevel.Verbose, "Done OnResume");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, "OnResume " + ex.ToString());
            }

        }
        protected override void OnPause()
        {
            try
            {
                this.logDebug(LogLevel.Verbose, "Entering OnPause");

                if (this.isServiceRunning())
                    ServiceReceiver.SendUnsubscribe(this);

                this.receiver.AlarmUpdate -= Receiver_AlarmUpdate;
                this.receiver.DistanceUpdate -= Receiver_DistanceUpdate;
                this.receiver.DebugUpdate -= Receiver_DebugUpdate;

                logDebug(LogLevel.Verbose, "app paused");
                LocationManager lm = (LocationManager)GetSystemService(Context.LocationService);
                lm.RemoveGpsStatusListener(this);

                base.OnPause();

                this.logDebug(LogLevel.Verbose, "Done OnPause");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, "OnPause " + ex.ToString());
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
                {
                    // limit UI to small number of items on the list
                    if (this.adapter.Count == 100)
                        this.adapter.Remove(this.adapter.GetItem(this.adapter.Count - 1));
                    this.adapter.Insert(message, 0);
                }
            }
            catch (Exception ex)
            {
                Common.Log($"CRASH logUI {ex}");
            }
        }

        private void logDebug(LogLevel level, string message)
        {
            try
            {
                if (level > LogLevel.Verbose)
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
                logDebug(LogLevel.Verbose, "app destroyed");

                UnregisterReceiver(this.receiver);

                log_writer?.Dispose();
                log_writer = null;

                base.OnDestroy();

                this.logDebug(LogLevel.Verbose, "Done OnDestroy");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, "OnDestroy " + ex.ToString());
            }
        }

        private bool updateReadiness()
        {
            try
            {
                var prefs = Preferences.Load(this);
                this.alarmInfoTextView.Visibility = prefs.PrimaryAlarmEnabled ? ViewStates.Gone : ViewStates.Visible;
                string track_path = Preferences.LoadTrackFileName(this);
                bool track_enabled = track_path != null && System.IO.File.Exists(track_path);
                this.trackFileNameTextView.Text = track_path;
                if (!track_enabled)
                    this.trackInfoTextView.Text = "Track is not available.";
                else
                {
                    var gpx_data = GpxLoader.ReadGpx(track_path,
                        Length.FromMeters(prefs.OffTrackAlarmDistance),
                        ex => logDebug(LogLevel.Error, $"Error while loading GPX {ex.Message}"));

                    if (!gpx_data.Map.Segments.Any())
                    {
                        track_enabled = false;
                        this.trackInfoTextView.Text = "Empty track.";
                    }

                    logUI($"Found {gpx_data.Crossroads.Count} crossroads");
                }

                this.trackInfoTextView.Visibility = track_enabled ? ViewStates.Gone : ViewStates.Visible;

                LocationManager lm = (LocationManager)GetSystemService(Context.LocationService);
                bool gps_enabled = lm.IsProviderEnabled(LocationManager.GpsProvider);
                logDebug(LogLevel.Verbose, $"gps provider enabled {gps_enabled}");
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
                this.logDebug(LogLevel.Error, "updateReadiness " + ex.ToString());
                throw;
            }
        }

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
                this.logDebug(LogLevel.Error, "trackSelectionClicked " + ex.ToString());
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
                        this.trackFileNameTextView.Text = intent.Data.Path;
                        Preferences.SaveTrackFileName(this, intent.Data.Path);

                        updateReadiness();
                    }
                }

                this.logDebug(LogLevel.Verbose, "Done OnActivityResult");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, "OnActivityResult " + ex.ToString());
            }
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
                    this.StopService(radarIntent);
                }
                else
                {
                    this.logDebug(LogLevel.Verbose, "starting service service");
                    this.adapter.Clear();

                    showAlarm("running", Android.Graphics.Color.GreenYellow);

                    maxOutSystemVolume(Android.Media.VolumeNotificationFlags.PlaySound);

                    this.StartService(radarIntent);
                }
                this.logDebug(LogLevel.Verbose, "updating UI");
                updateReadiness();

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
                logDebug(LogLevel.Verbose, "MA gps changed to " + e.ToString());
                updateReadiness();
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
                this.logDebug(LogLevel.Error, "OnCreateOptionsMenu " + ex.ToString());
            }

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            try
            {
                this.logDebug(LogLevel.Verbose, "Entering OnOptionsItemSelected");
                // since we have only single item menu we blindly process further

                maxOutSystemVolume(new Android.Media.VolumeNotificationFlags());

                StartActivity(typeof(SettingsActivity));

                this.logDebug(LogLevel.Verbose, "Done OnOptionsItemSelected");
            }
            catch (Exception ex)
            {
                this.logDebug(LogLevel.Error, "OnOptionsItemSelected " + ex.ToString());
            }

            return true;
        }
    }
}

