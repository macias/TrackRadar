using Android.App;
using Android.Views;
using Gpx;
using MathUnit;
using System;
using System.Linq;
using System.Threading;

namespace TrackRadar
{
    [Application]
    public sealed class TrackRadarApp : Application
    {
        private GpxData trackData;
        public GpxData TrackData
        {
            get { return Interlocked.CompareExchange(ref trackData, null, null); }
            set { Interlocked.Exchange(ref trackData, value); }
        }
        private string trackInfo;
        public string TrackInfo
        {
            get { return Interlocked.CompareExchange(ref trackInfo, null, null); }
            set { Interlocked.Exchange(ref trackInfo, value); }
        }
        private IPreferences prefs;
        public IPreferences Prefs
        {
            get { return Interlocked.CompareExchange(ref prefs, null, null); }
            set { Interlocked.Exchange(ref prefs, value); }
        }

        public TrackRadarApp(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            this.Prefs = TrackRadar.Preferences.LoadAll(this);

            LoadTrack(onError: null);
        }

        internal void LoadTrack(Action<Exception> onError)
        {
            TrackData = null;

            string track_path = Prefs.TrackName;
            this.TrackInfo = track_path;

            if (track_path == null || !System.IO.File.Exists(track_path))
            {
                this.TrackInfo = "Track is not available.";
            }
            else
            {
                GpxData gpx_data = GpxLoader.ReadGpx(track_path,
                    offTrackDistance: Prefs.OffTrackAlarmDistance,
                    onError);

                if (gpx_data.Segments.Any())
                    TrackData = gpx_data;
                else
                    this.TrackInfo = "Empty track.";
            }
        }

    }
}