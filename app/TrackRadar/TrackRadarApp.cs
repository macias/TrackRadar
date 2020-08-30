using Android.App;
using Android.Content.Res;
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
        private int trackTag;
        public int TrackTag
        {
            get { return Interlocked.CompareExchange(ref trackTag, 0, 0); }
            set { Interlocked.Exchange(ref trackTag, value); }
        }
        private IPreferences prefs;
        public IPreferences Prefs
        {
            get { return Interlocked.CompareExchange(ref prefs, null, null); }
            set { Interlocked.Exchange(ref prefs, value); }
        }

        public int MainActivity_loadTrackRequestTag { get; internal set; }

        public TrackRadarApp(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            this.Prefs = TrackRadar.Preferences.LoadAll(this);

            Common.Log(LogLevel.Info, "APP constructor called");

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Common.Log(LogLevel.Verbose, $"app dispose {disposing}");
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            Common.Log(LogLevel.Verbose, "APP OnConfigurationChanged");

            base.OnConfigurationChanged(newConfig);
        }

        public override void OnLowMemory()
        {
            Common.Log(LogLevel.Verbose, "app OnLowMemory");

            base.OnLowMemory();
        }

        public override void OnTerminate()
        {
            Common.Log(LogLevel.Verbose, "app OnTerminate");
            base.OnTerminate();
        }
    }

}
