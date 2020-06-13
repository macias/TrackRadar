using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using MathUnit;

namespace TrackRadar
{
    [Activity(Label = "SettingsActivity")]
    public class SettingsActivity : Activity
    {
        CheckBox vibrateCheckBox;
        CheckBox showTurnAheadCheckBox;
        CheckBox requestGpsCheckBox;
        EditText offTrackDistanceEditText;
        EditText offTrackIntervalEditText;
        EditText noGpsIntervalEditText;
        EditText noGpsTimeoutEditText;
        private EditText turnAheadIntervalEditText;
        private EditText turnAheadScreenTimeoutEditText;
        EditText restSpeedThresholdEditText;
        EditText ridingSpeedThresholdEditText;
        private EditText turnAheadDistanceEditText;

        AudioSettings offTrackDistanceSettings;
        AudioSettings gpsLostSettings;
        AudioSettings gpsOnSettings;
        AudioSettings turnAheadSettings;

        Vibrator vibrator;

        private const int SelectDistanceAudioCode = 1;
        private const int SelectGpsLostAudioCode = 2;
        private const int SelectGpsOnAudioCode = 3;
        private const int SelectCrossroadsAudioCode = 4;

        bool playbackInitialized;

        private TrackRadarApp app => (TrackRadarApp)Application;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Settings);
            this.Title = nameof(TrackRadar) + " Settings";

            this.playbackInitialized = false;

            this.vibrator = (Vibrator)GetSystemService(Context.VibratorService);

            this.offTrackDistanceSettings = new AudioSettings(this, Preferences.OffTrackDefaultAudioId,
                SelectDistanceAudioCode,
                () => this.playbackInitialized,
                Resource.Id.OffTrackDistanceVolumeSeekBar,
                Resource.Id.OffTrackDistanceVolumeTextView,
                Resource.Id.OffTrackDistanceAudioFileNameTextView,
                Resource.Id.OffTrackDistancePlayButton,
                Resource.Id.OffTrackDistanceAudioFileNameButton);
            this.gpsOnSettings = new AudioSettings(this, Preferences.GpsOnDefaultAudioId,
                SelectGpsOnAudioCode,
                () => this.playbackInitialized,
                Resource.Id.GpsOnVolumeSeekBar,
                Resource.Id.GpsOnVolumeTextView,
                Resource.Id.GpsOnAudioFileNameTextView,
                Resource.Id.GpsOnPlayButton,
                Resource.Id.GpsOnAudioFileNameButton);
            this.turnAheadSettings = new AudioSettings(this, Preferences.CrossroadsDefaultAudioId,
                SelectCrossroadsAudioCode,
                () => this.playbackInitialized,
                Resource.Id.CrossroadsVolumeSeekBar,
                Resource.Id.CrossroadsVolumeTextView,
                Resource.Id.CrossroadsAudioFileNameTextView,
                Resource.Id.CrossroadsPlayButton,
                Resource.Id.CrossroadsAudioFileNameButton);
            this.gpsLostSettings = new AudioSettings(this, Preferences.GpsLostDefaultAudioId,
                SelectGpsLostAudioCode,
                () => this.playbackInitialized,
                Resource.Id.GpsLostVolumeSeekBar,
                Resource.Id.GpsLostVolumeTextView,
                Resource.Id.GpsLostAudioFileNameTextView,
                Resource.Id.GpsLostPlayButton,
                Resource.Id.GpsLostAudioFileNameButton);

            this.vibrateCheckBox = FindViewById<CheckBox>(Resource.Id.VibrateCheckBox);
            this.showTurnAheadCheckBox = FindViewById<CheckBox>(Resource.Id.ShowTurnAheadCheckBox);
            this.requestGpsCheckBox = FindViewById<CheckBox>(Resource.Id.RequestGpsCheckBox);
            this.offTrackDistanceEditText = FindViewById<EditText>(Resource.Id.OffTrackDistanceEditText);
            this.offTrackIntervalEditText = FindViewById<EditText>(Resource.Id.OffTrackIntervalEditText);
            this.noGpsIntervalEditText = FindViewById<EditText>(Resource.Id.NoGpsIntervalEditText);
            this.noGpsTimeoutEditText = FindViewById<EditText>(Resource.Id.NoGpsTimeoutEditText);

            this.restSpeedThresholdEditText = FindViewById<EditText>(Resource.Id.RestThresholdEditText);
            this.ridingSpeedThresholdEditText = FindViewById<EditText>(Resource.Id.RidingThresholdEditText);

            this.turnAheadDistanceEditText = FindViewById<EditText>(Resource.Id.TurnAheadDistanceEditText);
            this.turnAheadIntervalEditText = FindViewById<EditText>(Resource.Id.TurnAheadIntervalEditText);
            this.turnAheadScreenTimeoutEditText = FindViewById<EditText>(Resource.Id.TurnAheadScreenTimeoutEditText);

            this.vibrateCheckBox.CheckedChange += VibrateCheckBox_CheckedChange;

            loadPreferences(app.Prefs);

        }

        private void loadPreferences(IPreferences prefs)
        {
            this.playbackInitialized = false;

            this.vibrateCheckBox.Checked = prefs.UseVibration;
            this.requestGpsCheckBox.Checked = prefs.RequestGps;
            this.showTurnAheadCheckBox.Checked = prefs.ShowTurnAhead;
            this.offTrackDistanceEditText.Text = ((int)prefs.OffTrackAlarmDistance.Meters).ToString();
            this.offTrackIntervalEditText.Text = ((int)prefs.OffTrackAlarmInterval.TotalSeconds).ToString();
            this.noGpsIntervalEditText.Text = ((int)prefs.NoGpsAlarmAgainInterval.TotalMinutes).ToString();
            this.noGpsTimeoutEditText.Text = ((int)prefs.NoGpsAlarmFirstTimeout.TotalSeconds).ToString();
            this.offTrackDistanceSettings.Update(prefs.DistanceAudioVolume, prefs.DistanceAudioFileName);
            this.gpsLostSettings.Update(prefs.GpsLostAudioVolume, prefs.GpsLostAudioFileName);
            this.gpsOnSettings.Update(prefs.GpsOnAudioVolume, prefs.GpsOnAudioFileName);
            this.turnAheadSettings.Update(prefs.TurnAheadAudioVolume, prefs.TurnAheadAudioFileName);

            this.restSpeedThresholdEditText.Text = ((int)prefs.RestSpeedThreshold.KilometersPerHour).ToString();
            this.ridingSpeedThresholdEditText.Text = ((int)prefs.RidingSpeedThreshold.KilometersPerHour).ToString();

            this.turnAheadDistanceEditText.Text = ((int)prefs.TurnAheadAlarmDistance.Meters).ToString();
            this.turnAheadIntervalEditText.Text = ((int)prefs.TurnAheadAlarmInterval.TotalSeconds).ToString();
            this.turnAheadScreenTimeoutEditText.Text = ((int)prefs.TurnAheadScreenTimeout.TotalSeconds).ToString();

            this.playbackInitialized = true;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent intent)
        {
            base.OnActivityResult(requestCode, resultCode, intent);

            if (resultCode != Result.Ok)
                return;

            if (requestCode == SelectDistanceAudioCode)
                this.offTrackDistanceSettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectGpsLostAudioCode)
                this.gpsLostSettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectGpsOnAudioCode)
                this.gpsOnSettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectCrossroadsAudioCode)
                this.turnAheadSettings.AudioFileSelected(intent.Data);
        }


        private void VibrateCheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (this.vibrateCheckBox.Checked && this.playbackInitialized)
                Common.VibrateAlarm(this.vibrator);
        }

        public override void OnBackPressed()
        {
            app.Prefs = Preferences.SaveBehaviors(this, new Preferences()
            {
                ShowTurnAhead = showTurnAheadCheckBox.Checked,
                UseVibration = vibrateCheckBox.Checked,
                RequestGps = requestGpsCheckBox.Checked,
                OffTrackAlarmDistance = Length.FromMeters(int.Parse(offTrackDistanceEditText.Text)),
                OffTrackAlarmInterval = TimeSpan.FromSeconds(int.Parse(offTrackIntervalEditText.Text)),
                NoGpsAlarmAgainInterval = TimeSpan.FromMinutes(int.Parse(this.noGpsIntervalEditText.Text)),
                NoGpsAlarmFirstTimeout = TimeSpan.FromSeconds(int.Parse(this.noGpsTimeoutEditText.Text)),

                DistanceAudioVolume = this.offTrackDistanceSettings.Volume,
                DistanceAudioFileName = this.offTrackDistanceSettings.AudioFileName,
                GpsLostAudioVolume = this.gpsLostSettings.Volume,
                GpsLostAudioFileName = this.gpsLostSettings.AudioFileName,
                GpsOnAudioVolume = this.gpsOnSettings.Volume,
                GpsOnAudioFileName = this.gpsOnSettings.AudioFileName,

                TurnAheadAudioVolume = this.turnAheadSettings.Volume,
                TurnAheadAudioFileName = this.turnAheadSettings.AudioFileName,

                RestSpeedThreshold = Speed.FromKilometersPerHour(int.Parse(restSpeedThresholdEditText.Text)),
                RidingSpeedThreshold = Speed.FromKilometersPerHour(int.Parse(ridingSpeedThresholdEditText.Text)),

                TurnAheadAlarmDistance = Length.FromMeters(int.Parse(turnAheadDistanceEditText.Text)),
                TurnAheadAlarmInterval = TimeSpan.FromSeconds(int.Parse(turnAheadIntervalEditText.Text)),
                TurnAheadScreenTimeout = TimeSpan.FromSeconds(int.Parse(turnAheadScreenTimeoutEditText.Text)),
            });

            this.gpsLostSettings.Destroy();
            this.offTrackDistanceSettings.Destroy();
            this.gpsOnSettings.Destroy();
            this.turnAheadSettings.Destroy();

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
                loadPreferences(app.Prefs);
            else if (item.ItemId == Resource.Id.DefaultItem)
                loadPreferences(new Preferences());
            return true;
        }
    }
}