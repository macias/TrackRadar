using System;
using Android.Content;

namespace TrackRadar
{
    [BroadcastReceiver(Enabled = true, Exported = false)]
    //[IntentFilter(new[] { "prefs", "dist" })]
    public sealed class ServiceReceiver : BroadcastReceiver
    {
        public event EventHandler UpdatePrefs;
        public event EventHandler InfoRequest;

        public ServiceReceiver()
        {

        }

        public static ServiceReceiver Create(Context context)
        {
            var receiver = new ServiceReceiver();
            var filter = new IntentFilter();
            filter.AddAction(Message.Prefs);
            filter.AddAction(Message.Req);
            context.RegisterReceiver(receiver, filter);
            return receiver;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == Message.Prefs)
                UpdatePrefs?.Invoke(this, new EventArgs());
            else if (intent.Action == Message.Req)
                InfoRequest?.Invoke(this, new EventArgs());

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
            intent.SetAction(Message.Req);
            context.SendBroadcast(intent);
        }

    }
}