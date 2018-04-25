using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace TrackRadar
{
    [Activity(Label = "SettingsActivity")]
    public class SettingsActivity : Activity
    {
        CheckBox vibrateCheckBox;
        EditText distanceEditText;
        EditText offTrackIntervalEditText;
        EditText noGpsIntervalEditText;
        EditText noGpsTimeoutEditText;

        AudioSettings distanceSettings;
        AudioSettings gpsLostSettings;
        AudioSettings gpsOnSettings;

        Vibrator vibrator;

        private const int SelectDistanceAudioCode = 1;
        private const int SelectGpsLostAudioCode = 2;
        private const int SelectGpsOnAudioCode = 3;

        bool playbackInitialized;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Settings);
            this.Title = nameof(TrackRadar) + " Settings";

            this.playbackInitialized = false;

            this.vibrator = (Vibrator)GetSystemService(Context.VibratorService);

            this.distanceSettings = new AudioSettings(this, Preferences.OffTrackDefaultAudioId,
                SelectDistanceAudioCode,
                () => this.playbackInitialized,
                Resource.Id.DistanceVolumeSeekBar,
                Resource.Id.DistanceVolumeTextView,
                Resource.Id.DistanceAudioFileNameTextView,
                Resource.Id.DistancePlayButton,
                Resource.Id.DistanceAudioFileNameButton);
            this.gpsOnSettings = new AudioSettings(this, Preferences.GpsOnDefaultAudioId,
                SelectGpsOnAudioCode,
                () => this.playbackInitialized,
                Resource.Id.GpsOnVolumeSeekBar,
                Resource.Id.GpsOnVolumeTextView,
                Resource.Id.GpsOnAudioFileNameTextView,
                Resource.Id.GpsOnPlayButton,
                Resource.Id.GpsOnAudioFileNameButton);
            this.gpsLostSettings = new AudioSettings(this, Preferences.GpsLostDefaultAudioId,
                SelectGpsLostAudioCode,
                () => this.playbackInitialized,
                Resource.Id.GpsLostVolumeSeekBar,
                Resource.Id.GpsLostVolumeTextView,
                Resource.Id.GpsLostAudioFileNameTextView,
                Resource.Id.GpsLostPlayButton,
                Resource.Id.GpsLostAudioFileNameButton);

            this.vibrateCheckBox = FindViewById<CheckBox>(Resource.Id.VibrateCheckBox);
            this.distanceEditText = FindViewById<EditText>(Resource.Id.DistanceEditText);
            this.offTrackIntervalEditText = FindViewById<EditText>(Resource.Id.OffTrackIntervalEditText);
            this.noGpsIntervalEditText = FindViewById<EditText>(Resource.Id.NoGpsIntervalEditText);
            this.noGpsTimeoutEditText = FindViewById<EditText>(Resource.Id.NoGpsTimeoutEditText);

            this.vibrateCheckBox.CheckedChange += VibrateCheckBox_CheckedChange;

            loadPreferences(Preferences.Load(this));

        }

        private void loadPreferences(Preferences prefs)
        {
            this.playbackInitialized = false;

            this.vibrateCheckBox.Checked = prefs.UseVibration;
            this.distanceEditText.Text = prefs.OffTrackAlarmDistance.ToString();
            this.noGpsIntervalEditText.Text = ((int)prefs.NoGpsAlarmAgainInterval.TotalMinutes).ToString();
            this.noGpsTimeoutEditText.Text = ((int)prefs.NoGpsAlarmFirstTimeout.TotalSeconds).ToString();
            this.offTrackIntervalEditText.Text = ((int)prefs.OffTrackAlarmInterval.TotalSeconds).ToString();
            this.distanceSettings.Update(prefs.DistanceAudioVolume, prefs.DistanceAudioFileName);
            this.gpsLostSettings.Update(prefs.GpsLostAudioVolume, prefs.GpsLostAudioFileName);
            this.gpsOnSettings.Update(prefs.GpsOnAudioVolume, prefs.GpsOnAudioFileName);

            this.playbackInitialized = true;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent intent)
        {
            base.OnActivityResult(requestCode, resultCode, intent);

            if (resultCode != Result.Ok)
                return;

            if (requestCode == SelectDistanceAudioCode)
                this.distanceSettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectGpsLostAudioCode)
                this.gpsLostSettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectGpsOnAudioCode)
                this.gpsOnSettings.AudioFileSelected(intent.Data);
        }


        private void VibrateCheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (this.vibrateCheckBox.Checked && this.playbackInitialized)
                Common.VibrateAlarm(this.vibrator);
        }

        public override void OnBackPressed()
        {
            Preferences.Save(this, new Preferences()
            {
                UseVibration = vibrateCheckBox.Checked,
                OffTrackAlarmDistance = int.Parse(distanceEditText.Text),
                OffTrackAlarmInterval = TimeSpan.FromSeconds(int.Parse(offTrackIntervalEditText.Text)),
                NoGpsAlarmAgainInterval = TimeSpan.FromMinutes(int.Parse(this.noGpsIntervalEditText.Text)),
                NoGpsAlarmFirstTimeout = TimeSpan.FromSeconds(int.Parse(this.noGpsTimeoutEditText.Text)),
                DistanceAudioVolume = this.distanceSettings.Volume,
                DistanceAudioFileName = this.distanceSettings.AudioFileName,
                GpsLostAudioVolume = this.gpsLostSettings.Volume,
                GpsLostAudioFileName = this.gpsLostSettings.AudioFileName,
                GpsOnAudioVolume = this.gpsOnSettings.Volume,
                GpsOnAudioFileName = this.gpsOnSettings.AudioFileName,
            });

            this.gpsLostSettings.Destroy();
            this.distanceSettings.Destroy();
            this.gpsOnSettings.Destroy();

            ServiceReceiver.SendUpdatePrefs(this);

            base.OnBackPressed();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            this.MenuInflater.Inflate(Resource.Menu.SettingsMenu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.UndoItem)
                loadPreferences(Preferences.Load(this));
            else if (item.ItemId == Resource.Id.DefaultItem)
                loadPreferences(new Preferences());
            return true;
        }
    }
}