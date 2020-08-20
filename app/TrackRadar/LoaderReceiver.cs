using System;
using System.Runtime.InteropServices;
using Android.Content;
using MathUnit;

namespace TrackRadar
{
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public sealed class LoaderReceiver : BroadcastReceiver
    {
        [ComVisible(true)]
        public delegate void EventFileHandler(object sender, EventFileArgs e);

        public event EventFileHandler LoadRequest;
        public event EventHandler Subscribe;
        public event EventHandler Unsubscribe;
        public event EventHandler InfoRequest;

        public LoaderReceiver()
        {

        }

        public static LoaderReceiver Create(Context context)
        {
            var receiver = new LoaderReceiver();
            var filter = new IntentFilter();
            filter.AddAction(Message.LoadRequest);
            filter.AddAction(Message.RequestInfo);
            context.RegisterReceiver(receiver, filter);
            return receiver;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == Message.LoadRequest)
            {
                ProcessLoadRequest(intent);
            }
            else if (intent.Action == Message.RequestInfo)
                InfoRequest?.Invoke(this, new EventArgs());
            else if (intent.Action == Message.Subscribe)
                Subscribe?.Invoke(this, new EventArgs());
            else if (intent.Action == Message.Unsubscribe)
                Unsubscribe?.Invoke(this, new EventArgs());

            intent.Dispose();
        }

        public void ProcessLoadRequest(Intent intent)
        {
            string path = intent.GetStringExtra(Message.PathKey);
            double off_dist = intent.GetDoubleExtra(Message.DistanceKey, Preferences.DefaultOffTrackAlarmDistance.Meters);
            int tag_request = intent.GetIntExtra(Message.TagKey, -1);
            LoadRequest?.Invoke(this, new EventFileArgs(tag_request, path, Length.FromMeters(off_dist)));
        }

        internal static void SendInfoRequest(Context context)
        {
            var intent = new Intent();
            intent.SetAction(Message.RequestInfo);
            context.SendBroadcast(intent);
        }

        internal static void SendLoadRequest(Context context, int loadRequest, string path, Length offTrackDistance)
        {
            var intent = new Intent();
            intent.SetAction(Message.LoadRequest);
            SetLoadRequestData(intent, loadRequest, path, offTrackDistance);
            context.SendBroadcast(intent);
        }

        public static void SetLoadRequestData(Intent intent, int loadRequest, string path, Length offTrackDistance)
        {
            intent.PutExtra(Message.PathKey, path);
            intent.PutExtra(Message.DistanceKey, offTrackDistance.Meters);
            intent.PutExtra(Message.TagKey, loadRequest);
        }

        internal static void SendSubscribe(Context context)
        {
            var intent = new Intent();
            intent.SetAction(Message.Subscribe);
            context.SendBroadcast(intent);
        }
        internal static void SendUnsubscribe(Context context)
        {
            var intent = new Intent();
            intent.SetAction(Message.Unsubscribe);
            context.SendBroadcast(intent);
        }

    }
}