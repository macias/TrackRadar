using System;
using Android.Content;

namespace TrackRadar
{
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public sealed class RadarReceiver : BroadcastReceiver
    {
        public event EventHandler UpdatePrefs;
        public event EventHandler InfoRequest;
        public event EventHandler Subscribe;
        public event EventHandler Unsubscribe;

        public RadarReceiver()
        {

        }

        public static RadarReceiver Create(Context context)
        {
            var receiver = new RadarReceiver();
            var filter = new IntentFilter();
            filter.AddAction(Message.Prefs);
            filter.AddAction(Message.RequestInfo);
            context.RegisterReceiver(receiver, filter);
            return receiver;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == Message.Prefs)
                UpdatePrefs?.Invoke(this, new EventArgs());
            else if (intent.Action == Message.RequestInfo)
                InfoRequest?.Invoke(this, new EventArgs());
            else if (intent.Action == Message.Subscribe)
                Subscribe?.Invoke(this, new EventArgs());
            else if (intent.Action == Message.Unsubscribe)
                Unsubscribe?.Invoke(this, new EventArgs());

            intent.Dispose();
        }

        internal static void SendUpdatePrefs(Context context)
        {
            var intent = new Intent();
            intent.SetAction(Message.Prefs);
            context.SendBroadcast(intent);
        }

        internal static void SendInfoRequest(Context context)
        {
            var intent = new Intent();
            intent.SetAction(Message.RequestInfo);
            context.SendBroadcast(intent);
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