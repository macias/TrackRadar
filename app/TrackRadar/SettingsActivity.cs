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
        CheckBox gpsFilterCheckBox;
        CheckBox gpsDumpCheckBox;
        EditText offTrackDistanceEditText;
        EditText offTrackIntervalEditText;
        EditText noGpsIntervalEditText;
        EditText noGpsTimeoutEditText;
        private EditText turnAheadIntervalEditText;
        private EditText turnAheadScreenTimeoutEditText;
        EditText restSpeedThresholdEditText;
        EditText ridingSpeedThresholdEditText;
        private EditText turnAheadDistanceEditText;

        AudioRichSettings offTrackDistanceSettings;
        AudioRichSettings gpsLostSettings;
        AudioRichSettings gpsOnSettings;
        AudioRichSettings turnAheadSettings;
        AudioRichSettings disengageSettings;

        AudioFileSettings goAheadSettings;
        AudioFileSettings leftEasySettings;
        AudioFileSettings leftCrossSettings;
        AudioFileSettings leftSharpSettings;
        AudioFileSettings rightEasySettings;
        AudioFileSettings rightCrossSettings;
        AudioFileSettings rightSharpSettings;

        IAlarmVibrator vibrator;

        private const int SelectDistanceAudioCode = 1;
        private const int SelectGpsLostAudioCode = 2;
        private const int SelectGpsOnAudioCode = 3;
        private const int SelectCrossroadsAudioCode = 4;

        private const int SelectGoAheadAudioCode = 5;
        private const int SelectLeftEasyAudioCode = 6;
        private const int SelectLeftCrossAudioCode = 7;
        private const int SelectLeftSharpAudioCode = 8;
        private const int SelectRightEasyAudioCode = 9;
        private const int SelectRightCrossAudioCode = 10;
        private const int SelectRightSharpAudioCode = 11;

        private const int SelectDisengageAudioCode = 12;

        bool playbackInitialized;

        private TrackRadarApp app => (TrackRadarApp)Application;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Settings);
            this.Title = nameof(TrackRadar) + " Settings";

            this.playbackInitialized = false;

            this.vibrator = new AlarmVibrator((Vibrator)GetSystemService(Context.VibratorService));

            this.offTrackDistanceSettings = new AudioRichSettings(this, Preferences.OffTrackDefaultAudioId,
                SelectDistanceAudioCode,
                () => this.playbackInitialized,
                Resource.Id.OffTrackDistanceVolumeSeekBar,
                Resource.Id.OffTrackDistanceVolumeTextView,
                Resource.Id.OffTrackDistanceAudioFileNameTextView,
                Resource.Id.OffTrackDistancePlayButton,
                Resource.Id.OffTrackDistanceAudioFileNameButton);
            this.gpsOnSettings = new AudioRichSettings(this, Preferences.GpsOnDefaultAudioId,
                SelectGpsOnAudioCode,
                () => this.playbackInitialized,
                Resource.Id.GpsOnVolumeSeekBar,
                Resource.Id.GpsOnVolumeTextView,
                Resource.Id.GpsOnAudioFileNameTextView,
                Resource.Id.GpsOnPlayButton,
                Resource.Id.GpsOnAudioFileNameButton);
            this.disengageSettings = new AudioRichSettings(this, Preferences.DisengageDefaultAudioId,
                SelectDisengageAudioCode,
                () => this.playbackInitialized,
                Resource.Id.DisengageVolumeSeekBar,
                Resource.Id.DisengageVolumeTextView,
                Resource.Id.DisengageAudioFileNameTextView,
                Resource.Id.DisengagePlayButton,
                Resource.Id.DisengageAudioFileNameButton);
            this.turnAheadSettings = new AudioRichSettings(this, Preferences.CrossroadsDefaultAudioId,
                SelectCrossroadsAudioCode,
                () => this.playbackInitialized,
                Resource.Id.CrossroadsVolumeSeekBar,
                Resource.Id.CrossroadsVolumeTextView,
                Resource.Id.CrossroadsAudioFileNameTextView,
                Resource.Id.CrossroadsPlayButton,
                Resource.Id.CrossroadsAudioFileNameButton);

            this.goAheadSettings = new AudioFileSettings(this, Preferences.GoAheadDefaultAudioId, SelectGoAheadAudioCode, () => this.playbackInitialized, Resource.Id.GoAheadAudioFileNameTextView, Resource.Id.GoAheadPlayButton, Resource.Id.GoAheadAudioFileNameButton, turnAheadSettings);
            this.leftEasySettings = new AudioFileSettings(this, Preferences.LeftEasyDefaultAudioId, SelectLeftEasyAudioCode, () => this.playbackInitialized, Resource.Id.LeftEasyAudioFileNameTextView, Resource.Id.LeftEasyPlayButton, Resource.Id.LeftEasyAudioFileNameButton, turnAheadSettings);
            this.leftCrossSettings = new AudioFileSettings(this, Preferences.LeftCrossDefaultAudioId, SelectLeftCrossAudioCode, () => this.playbackInitialized, Resource.Id.LeftCrossAudioFileNameTextView, Resource.Id.LeftCrossPlayButton, Resource.Id.LeftCrossAudioFileNameButton, turnAheadSettings);
            this.leftSharpSettings = new AudioFileSettings(this, Preferences.LeftSharpDefaultAudioId, SelectLeftSharpAudioCode, () => this.playbackInitialized, Resource.Id.LeftSharpAudioFileNameTextView, Resource.Id.LeftSharpPlayButton, Resource.Id.LeftSharpAudioFileNameButton, turnAheadSettings);
            this.rightEasySettings = new AudioFileSettings(this, Preferences.RightEasyDefaultAudioId, SelectRightEasyAudioCode, () => this.playbackInitialized, Resource.Id.RightEasyAudioFileNameTextView, Resource.Id.RightEasyPlayButton, Resource.Id.RightEasyAudioFileNameButton, turnAheadSettings);
            this.rightCrossSettings = new AudioFileSettings(this, Preferences.RightCrossDefaultAudioId, SelectRightCrossAudioCode, () => this.playbackInitialized, Resource.Id.RightCrossAudioFileNameTextView, Resource.Id.RightCrossPlayButton, Resource.Id.RightCrossAudioFileNameButton, turnAheadSettings);
            this.rightSharpSettings = new AudioFileSettings(this, Preferences.RightSharpDefaultAudioId, SelectRightSharpAudioCode, () => this.playbackInitialized, Resource.Id.RightSharpAudioFileNameTextView, Resource.Id.RightSharpPlayButton, Resource.Id.RightSharpAudioFileNameButton, turnAheadSettings);

            this.gpsLostSettings = new AudioRichSettings(this, Preferences.GpsLostDefaultAudioId,
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
            this.gpsFilterCheckBox = FindViewById<CheckBox>(Resource.Id.GpsFilterCheckBox);
            this.gpsDumpCheckBox = FindViewById<CheckBox>(Resource.Id.GpsDumpCheckBox);
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
            this.requestGpsCheckBox.Checked = prefs.DebugKillingService;
            this.gpsFilterCheckBox.Checked = prefs.GpsFilter;
            this.gpsDumpCheckBox.Checked = prefs.GpsDump;
            this.showTurnAheadCheckBox.Checked = prefs.ShowTurnAhead;
            this.offTrackDistanceEditText.Text = ((int)prefs.OffTrackAlarmDistance.Meters).ToString();
            this.offTrackIntervalEditText.Text = ((int)prefs.OffTrackAlarmInterval.TotalSeconds).ToString();
            this.noGpsIntervalEditText.Text = ((int)prefs.NoGpsAlarmAgainInterval.TotalMinutes).ToString();
            this.noGpsTimeoutEditText.Text = ((int)prefs.NoGpsAlarmFirstTimeout.TotalSeconds).ToString();
            this.offTrackDistanceSettings.Update(prefs.OffTrackAudioVolume, prefs.DistanceAudioFileName);
            this.gpsLostSettings.Update(prefs.GpsLostAudioVolume, prefs.GpsLostAudioFileName);
            this.gpsOnSettings.Update(prefs.AcknowledgementAudioVolume, prefs.GpsOnAudioFileName);
            this.disengageSettings.Update(prefs.DisengageAudioVolume, prefs.DisengageAudioFileName);
            this.turnAheadSettings.Update(prefs.TurnAheadAudioVolume, prefs.TurnAheadAudioFileName);

            this.goAheadSettings.Update(prefs.GoAheadAudioFileName);
            this.leftEasySettings.Update(prefs.LeftEasyAudioFileName);
            this.leftCrossSettings.Update(prefs.LeftCrossAudioFileName);
            this.leftSharpSettings.Update(prefs.LeftSharpAudioFileName);
            this.rightEasySettings.Update(prefs.RightEasyAudioFileName);
            this.rightCrossSettings.Update(prefs.RightCrossAudioFileName);
            this.rightSharpSettings.Update(prefs.RightSharpAudioFileName);

            this.restSpeedThresholdEditText.Text = ((int)prefs.RestSpeedThreshold.KilometersPerHour).ToString();
            this.ridingSpeedThresholdEditText.Text = ((int)prefs.RidingSpeedThreshold.KilometersPerHour).ToString();

            this.turnAheadDistanceEditText.Text = ((int)prefs.TurnAheadAlarmDistance.TotalSeconds).ToString();
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
            else if (requestCode == SelectDisengageAudioCode)
                this.disengageSettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectCrossroadsAudioCode)
                this.turnAheadSettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectGoAheadAudioCode)
                this.goAheadSettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectLeftEasyAudioCode)
                this.leftEasySettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectLeftCrossAudioCode)
                this.leftCrossSettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectLeftSharpAudioCode)
                this.leftSharpSettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectRightEasyAudioCode)
                this.rightEasySettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectRightCrossAudioCode)
                this.rightCrossSettings.AudioFileSelected(intent.Data);
            else if (requestCode == SelectRightSharpAudioCode)
                this.rightSharpSettings.AudioFileSelected(intent.Data);
        }


        private void VibrateCheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (this.vibrateCheckBox.Checked && this.playbackInitialized)
                Common.VibrateAlarm(this.vibrator);
        }

        public override void OnBackPressed()
        {
            Preferences p = app.Prefs.Clone();

            p.ShowTurnAhead = showTurnAheadCheckBox.Checked;
            p.UseVibration = vibrateCheckBox.Checked;
            p.DebugKillingService = requestGpsCheckBox.Checked;
            p.GpsDump = gpsDumpCheckBox.Checked;
            p.GpsFilter = gpsFilterCheckBox.Checked;
            p.OffTrackAlarmDistance = Length.FromMeters(int.Parse(offTrackDistanceEditText.Text));
            p.OffTrackAlarmInterval = TimeSpan.FromSeconds(int.Parse(offTrackIntervalEditText.Text));
            p.NoGpsAlarmAgainInterval = TimeSpan.FromMinutes(int.Parse(this.noGpsIntervalEditText.Text));
            p.NoGpsAlarmFirstTimeout = TimeSpan.FromSeconds(int.Parse(this.noGpsTimeoutEditText.Text));

            p.OffTrackAudioVolume = this.offTrackDistanceSettings.Volume;
            p.DistanceAudioFileName = this.offTrackDistanceSettings.AudioFileName;
            p.GpsLostAudioVolume = this.gpsLostSettings.Volume;
            p.GpsLostAudioFileName = this.gpsLostSettings.AudioFileName;
            p.AcknowledgementAudioVolume = this.gpsOnSettings.Volume;
            p.GpsOnAudioFileName = this.gpsOnSettings.AudioFileName;

            p.DisengageAudioVolume = this.disengageSettings.Volume;
            p.DisengageAudioFileName = this.disengageSettings.AudioFileName;

            p.TurnAheadAudioVolume = this.turnAheadSettings.Volume;
            p.TurnAheadAudioFileName = this.turnAheadSettings.AudioFileName;

            p.GoAheadAudioFileName = this.goAheadSettings.AudioFileName;
            p.LeftEasyAudioFileName = this.leftEasySettings.AudioFileName;
            p.LeftCrossAudioFileName = this.leftCrossSettings.AudioFileName;
            p.LeftSharpAudioFileName = this.leftSharpSettings.AudioFileName;
            p.RightEasyAudioFileName = this.rightEasySettings.AudioFileName;
            p.RightCrossAudioFileName = this.rightCrossSettings.AudioFileName;
            p.RightSharpAudioFileName = this.rightSharpSettings.AudioFileName;

            p.RestSpeedThreshold = Speed.FromKilometersPerHour(int.Parse(restSpeedThresholdEditText.Text));
            p.RidingSpeedThreshold = Speed.FromKilometersPerHour(int.Parse(ridingSpeedThresholdEditText.Text));

            p.TurnAheadAlarmDistance = TimeSpan.FromSeconds(int.Parse(turnAheadDistanceEditText.Text));
            p.TurnAheadAlarmInterval = TimeSpan.FromSeconds(int.Parse(turnAheadIntervalEditText.Text));
            p.TurnAheadScreenTimeout = TimeSpan.FromSeconds(int.Parse(turnAheadScreenTimeoutEditText.Text));

            app.Prefs = Preferences.SaveBehaviors(this, p);

            this.gpsLostSettings.Destroy();
            this.offTrackDistanceSettings.Destroy();
            this.gpsOnSettings.Destroy();
            this.disengageSettings.Destroy();
            this.turnAheadSettings.Destroy();
            this.goAheadSettings.Destroy();
            this.leftEasySettings.Destroy();
            this.leftCrossSettings.Destroy();
            this.leftSharpSettings.Destroy();
            this.rightEasySettings.Destroy();
            this.rightCrossSettings.Destroy();
            this.rightSharpSettings.Destroy();

            RadarReceiver.SendUpdatePrefs(this);

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
                loadPreferences(Preferences.Default);
            return true;
        }
    }
}