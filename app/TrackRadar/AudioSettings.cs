using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Widget;

namespace TrackRadar
{
    public sealed class AudioSettings
    {
        private SeekBar volumeSeekBar;
        private TextView audioFileNameTextView;
        private TextView volumeTextView;
        private Button playButton;
        private Button audioSelectButton;
        private MediaPlayer player;

        private readonly Activity activity;
        private readonly int intentSelectionCode;
        private readonly Func<bool> isPlaybackEnabled;
        private readonly int audioResourceId;

        public int Volume => this.volumeSeekBar.Progress;
        public string AudioFileName { get; private set; }

        public AudioSettings(Activity activity, int audioResourceId,  int selectionCode,
            Func<bool> isPlaybackEnabled,
            int volumeSeekBarId, int volumeTextViewId, int audioFileNameTextViewId,
            int playButtonId, int audioFileNameButtonId)
        {
            this.intentSelectionCode = selectionCode;
            this.activity = activity;
            this.isPlaybackEnabled = isPlaybackEnabled;
            this.audioResourceId = audioResourceId;

            this.volumeSeekBar = activity.FindViewById<SeekBar>(volumeSeekBarId);
            this.volumeTextView = activity.FindViewById<TextView>(volumeTextViewId);
            this.audioFileNameTextView = activity.FindViewById<TextView>(audioFileNameTextViewId);
            this.playButton = activity.FindViewById<Button>(playButtonId);
            this.audioSelectButton = activity.FindViewById<Button>(audioFileNameButtonId);

            this.playButton.Enabled = false;

            this.playButton.Click += PlayButton_Click;
            this.audioSelectButton.Click += AudioSelectButton_Click;

            this.volumeSeekBar.ProgressChanged += VolumeSeekBar_ProgressChanged;

        }
        private void createPlayer(string filename)
        {
            this.player = Common.CreateMediaPlayer(this.activity, filename,this.audioResourceId);
            updatePlaybackVolume();
            this.playButton.Enabled = this.player != null;
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (player != null && !player.IsPlaying)
                player.Start();
        }

        private void VolumeSeekBar_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            this.volumeTextView.Text = this.volumeSeekBar.Progress.ToString();
            updatePlaybackVolume();
        }
        private void setAudioFileNameTextView(string filename)
        {
            this.AudioFileName = filename;
            if (String.IsNullOrEmpty(filename))
            {
                this.audioFileNameTextView.SetTypeface(null, TypefaceStyle.Italic);
                this.audioFileNameTextView.Text = "default";
            }
            else
            {
                this.audioFileNameTextView.SetTypeface(null, TypefaceStyle.Normal);
                this.audioFileNameTextView.Text = filename;
            }
        }
        public void Destroy()
        {
            this.player = Common.DestroyMediaPlayer(this.player);
        }

        private void updatePlaybackVolume()
        {
            Common.SetVolume(this.player, volumeSeekBar.Progress);
            if (player != null && isPlaybackEnabled())
            {
                if (!player.IsPlaying)
                    player.Start();
            }
        }

        internal void Update(int distanceAudioVolume, string distanceAudioFileName)
        {
            this.volumeSeekBar.Progress = distanceAudioVolume;
            setAudioFileNameTextView(distanceAudioFileName);

            createPlayer(distanceAudioFileName);
        }
        private void AudioSelectButton_Click(object sender, EventArgs e)
        {
            var intent = new Intent();
            intent.SetType("audio/*");
            intent.SetAction(Intent.ActionGetContent);
            activity.StartActivityForResult(
                Intent.CreateChooser(intent, "Select audio file"), intentSelectionCode);
        }

        internal void AudioFileSelected(Android.Net.Uri data)
        {
            if (data?.Path == null)
                return;

            setAudioFileNameTextView(data.Path);
            createPlayer(data.Path);
        }
    }
}