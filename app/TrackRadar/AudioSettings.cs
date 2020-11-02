using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Widget;

namespace TrackRadar
{
    public class AudioFileSettings
    {
        private TextView audioFileNameTextView;
        private Button playButton;
        private Button audioSelectButton;
        private MediaPlayer player;

        private readonly Activity activity;
        private readonly int intentSelectionCode;
        private readonly Func<bool> isPlaybackEnabled;
        private readonly AudioRichSettings masterSettings;
        private readonly int audioResourceId;

        public string AudioFileName { get; private set; }

        public AudioFileSettings(Activity activity, int audioResourceId, int selectionCode,
            Func<bool> isPlaybackEnabled,
            int audioFileNameTextViewId,
            int playButtonId, int audioFileNameButtonId,
            AudioRichSettings masterSettings)
        {
            this.intentSelectionCode = selectionCode;
            this.activity = activity;
            this.isPlaybackEnabled = isPlaybackEnabled;
            this.audioResourceId = audioResourceId;

            this.audioFileNameTextView = activity.FindViewById<TextView>(audioFileNameTextViewId);
            this.playButton = activity.FindViewById<Button>(playButtonId);
            this.audioSelectButton = activity.FindViewById<Button>(audioFileNameButtonId);

            this.playButton.Enabled = false;

            this.playButton.Click += PlayButton_Click;
            this.audioSelectButton.Click += AudioSelectButton_Click;
            this.masterSettings = masterSettings ?? (AudioRichSettings)this;
        }
        private void createPlayer(string filename)
        {
            // type of alarm does not matter here
            this.player = Common.CreateMediaPlayer(this.activity, filename, this.audioResourceId, out _);
            UpdatePlaybackVolume();
            this.playButton.Enabled = this.player != null;
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (player != null && !player.IsPlaying)
                player.Start();
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

        protected void UpdatePlaybackVolume()
        {
            Common.SetVolume(this.player, masterSettings.Volume);
            if (player != null && isPlaybackEnabled())
            {
                if (!player.IsPlaying)
                    player.Start();
            }
        }

        internal void Update(string audioFileName)
        {
            setAudioFileNameTextView(audioFileName);

            createPlayer(audioFileName);
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

    public sealed class AudioRichSettings : AudioFileSettings
    {
        private SeekBar volumeSeekBar;
        //private TextView audioFileNameTextView;
        private TextView volumeTextView;
        //private Button playButton;
        //private Button audioSelectButton;
        //private MediaPlayer player;

        //private readonly Activity activity;
        //private readonly int intentSelectionCode;
        //private readonly Func<bool> isPlaybackEnabled;
        //private readonly int audioResourceId;

        public int Volume => this.volumeSeekBar.Progress;
        //public string AudioFileName { get; private set; }

        public AudioRichSettings(Activity activity, int audioResourceId, int selectionCode,
            Func<bool> isPlaybackEnabled,
            int volumeSeekBarId, int volumeTextViewId, int audioFileNameTextViewId,
            int playButtonId, int audioFileNameButtonId)
            : base(activity, audioResourceId, selectionCode,
             isPlaybackEnabled,
             audioFileNameTextViewId,
             playButtonId, audioFileNameButtonId, null)
        {
            //this.intentSelectionCode = selectionCode;
            //this.activity = activity;
            //this.isPlaybackEnabled = isPlaybackEnabled;
            //this.audioResourceId = audioResourceId;

            this.volumeSeekBar = activity.FindViewById<SeekBar>(volumeSeekBarId);
            this.volumeTextView = activity.FindViewById<TextView>(volumeTextViewId);
            //this.audioFileNameTextView = activity.FindViewById<TextView>(audioFileNameTextViewId);
            //this.playButton = activity.FindViewById<Button>(playButtonId);
            //this.audioSelectButton = activity.FindViewById<Button>(audioFileNameButtonId);

            //this.playButton.Enabled = false;

            //this.playButton.Click += PlayButton_Click;
            //this.audioSelectButton.Click += AudioSelectButton_Click;

            this.volumeSeekBar.ProgressChanged += VolumeSeekBar_ProgressChanged;

        }
        /*private void createPlayer(string filename)
        {
            this.player = Common.CreateMediaPlayer(this.activity, filename,this.audioResourceId);
            updatePlaybackVolume();
            this.playButton.Enabled = this.player != null;
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (player != null && !player.IsPlaying)
                player.Start();
        }*/

        private void VolumeSeekBar_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            this.volumeTextView.Text = this.volumeSeekBar.Progress.ToString();
            UpdatePlaybackVolume();
        }
        /* private void setAudioFileNameTextView(string filename)
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

         }*/
        /*private void updatePlaybackVolume()
        {
            Common.SetVolume(this.player, volumeSeekBar.Progress);
            if (player != null && isPlaybackEnabled())
            {
                if (!player.IsPlaying)
                    player.Start();
            }
            */
        internal void Update(int audioVolume, string audioFileName)
        {
            this.volumeSeekBar.Progress = audioVolume;
            Update(audioFileName);
            //setAudioFileNameTextView(audioFileName);

            //createPlayer(audioFileName);
        }
        /*private void AudioSelectButton_Click(object sender, EventArgs e)
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
        }*/
    }
}