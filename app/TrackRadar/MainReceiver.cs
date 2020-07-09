using System;
using Android.Content;
using MathUnit;

namespace TrackRadar
{
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public sealed class MainReceiver : BroadcastReceiver
    {
        public static MainReceiver Create(Context context)
        {
            var filter = new IntentFilter();
            filter.AddAction(Message.Dbg);
            filter.AddAction(Message.Dist);
            filter.AddAction(Message.Alarm);
            var receiver = new MainReceiver() { context = context, filter = filter };
            return receiver;
        }

        public event EventHandler<DistanceEventArgs> DistanceUpdate;
        public event EventHandler<MessageEventArgs> DebugUpdate;
        public event EventHandler<MessageEventArgs> AlarmUpdate;

        private Context context;
        private IntentFilter filter;

        public MainReceiver()
        {
        }

        public new void Dispose()
        {
            context.UnregisterReceiver(this);
            base.Dispose();
        }

        public void RegisterReceiver()
        {
            context.RegisterReceiver(this, filter);
        }

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == Message.Dist)
                DistanceUpdate?.Invoke(this, new DistanceEventArgs(
                    intent.GetDoubleExtra(Message.DistanceKey, 0),
                    Length.FromMeters(intent.GetDoubleExtra(Message.TotalClimbsMetersKey, 0)),
                    Length.FromMeters(intent.GetDoubleExtra(Message.RidingDistanceMetersKey, 0)),
                    TimeSpan.FromSeconds(intent.GetDoubleExtra(Message.RidingTimeSecondsKey, 0)),
                    Speed.FromKilometersPerHour(intent.GetDoubleExtra(Message.TopSpeedKmPerHourKey, 0))
                    ));
            else if (intent.Action == Message.Dbg)
                DebugUpdate?.Invoke(this, new MessageEventArgs(intent.GetStringExtra(Message.DebugKey)));
            else if (intent.Action == Message.Alarm)
                AlarmUpdate?.Invoke(this, new MessageEventArgs(intent.GetStringExtra(Message.AlarmKey)));

            intent.Dispose();
        }

        internal static void SendDebug(Context context, string message)
        {
            var intent = new Intent();
            intent.SetAction(Message.Dbg);
            intent.PutExtra(Message.DebugKey, message);
            context.SendBroadcast(intent);
        }

        internal static void SendAlarm(Context context, string message)
        {
            var intent = new Intent();
            intent.SetAction(Message.Alarm);
            intent.PutExtra(Message.AlarmKey, message);
            context.SendBroadcast(intent);
        }

        internal static void SendDistance(Context context, double distance,Length totalClimbs,
            Length ridingDistance, TimeSpan ridingTime, Speed topSpeed)
        {
            var intent = new Intent();
            intent.SetAction(Message.Dist);
            intent.PutExtra(Message.DistanceKey, distance);
            intent.PutExtra(Message.TotalClimbsMetersKey, totalClimbs.Meters);
            intent.PutExtra(Message.RidingDistanceMetersKey, ridingDistance.Meters);
            intent.PutExtra(Message.RidingTimeSecondsKey, ridingTime.TotalSeconds);
            intent.PutExtra(Message.TopSpeedKmPerHourKey, topSpeed.KilometersPerHour);
            context.SendBroadcast(intent);
        }

    }
}