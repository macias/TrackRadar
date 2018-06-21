using System;
using Android.Content;

namespace TrackRadar
{
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public sealed class MainReceiver : BroadcastReceiver
    {
        public event EventHandler<DistanceEventArgs> DistanceUpdate;
        public event EventHandler<MessageEventArgs> DebugUpdate;
        public event EventHandler<MessageEventArgs> AlarmUpdate;

        public MainReceiver()
        {

        }

        public static MainReceiver Create(Context context)
        {
            var receiver = new MainReceiver();
            var filter = new IntentFilter();
            filter.AddAction(Message.Dbg);
            filter.AddAction(Message.Dist);
            filter.AddAction(Message.Alarm);
            context.RegisterReceiver(receiver, filter);
            return receiver;
        }
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == Message.Dist)
                DistanceUpdate?.Invoke(this, new DistanceEventArgs(intent.GetDoubleExtra(Message.ValueKey, -1)));
            else if (intent.Action == Message.Dbg)
                DebugUpdate?.Invoke(this, new MessageEventArgs(intent.GetStringExtra(Message.ValueKey)));
            else if (intent.Action == Message.Alarm)
                AlarmUpdate?.Invoke(this, new MessageEventArgs(intent.GetStringExtra(Message.ValueKey)));

            intent.Dispose();
        }

        internal static void SendDebug(Context context, string message)
        {
            var intent = new Intent();
            intent.SetAction(Message.Dbg);
            intent.PutExtra(Message.ValueKey, message);
            context.SendBroadcast(intent);
        }

        internal static void SendAlarm(Context context, string message)
        {
            var intent = new Intent();
            intent.SetAction(Message.Alarm);
            intent.PutExtra(Message.ValueKey, message);
            context.SendBroadcast(intent);
        }

        internal static void SendDistance(Context context, double distance)
        {
            var intent = new Intent();
            intent.SetAction(Message.Dist);
            intent.PutExtra(Message.ValueKey, distance);
            context.SendBroadcast(intent);
        }

    }
}