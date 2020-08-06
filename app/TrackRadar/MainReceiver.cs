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
            filter.AddAction(Message.Debug);
            filter.AddAction(Message.Distance);
            filter.AddAction(Message.Alarm);
            filter.AddAction(Message.LoadingProgress);
            var receiver = new MainReceiver() { context = context, filter = filter };
            return receiver;
        }

        public event EventHandler<DistanceEventArgs> DistanceUpdate;
        public event EventHandler<MessageEventArgs> DebugUpdate;
        public event EventHandler<MessageEventArgs> AlarmUpdate;
        public event EventHandler<ProgressEventArgs> ProgressUpdate;

        private Context context;
        private IntentFilter filter;

        public MainReceiver()
        {
        }

        // normally it would be Dispose and calling base.Dispose at the end, but I get some mysterious error
        // System.NotSupportedException: Unable to activate instance of type TrackRadar.MainReceiver from native handle 0x405466e8 (key_handle 0x405466e8). ---> System.MissingMethodException: No constructor found for TrackRadar.MainReceiver::.ctor(System.IntPtr, Android.Runtime.JniHandleOwnership) ---> Java.Interop.JavaLocationException: Exception of type 'Java.Interop.JavaLocationException' was thrown.
        // which has something to do with Android/Java-Xamarin object mapping
        public void Stop()
        {
            context.UnregisterReceiver(this);
        }

        public void Start()
        {
            context.RegisterReceiver(this, filter);
        }

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == Message.Distance)
                DistanceUpdate?.Invoke(this, new DistanceEventArgs(
                    intent.GetDoubleExtra(Message.DistanceKey, 0),
                    Length.FromMeters(intent.GetDoubleExtra(Message.TotalClimbsMetersKey, 0)),
                    Length.FromMeters(intent.GetDoubleExtra(Message.RidingDistanceMetersKey, 0)),
                    TimeSpan.FromSeconds(intent.GetDoubleExtra(Message.RidingTimeSecondsKey, 0)),
                    Speed.FromKilometersPerHour(intent.GetDoubleExtra(Message.TopSpeedKmPerHourKey, 0))
                    ));
            else if (intent.Action == Message.Debug)
                DebugUpdate?.Invoke(this, new MessageEventArgs(intent.GetStringExtra(Message.DebugKey)));
            else if (intent.Action == Message.Alarm)
                AlarmUpdate?.Invoke(this, new MessageEventArgs(intent.GetStringExtra(Message.AlarmKey)));
            else if (intent.Action == Message.LoadingProgress)
                ProgressUpdate?.Invoke(this, new ProgressEventArgs(
                    intent.GetStringExtra(Message.MessageKey),
                    intent.GetDoubleExtra(Message.ProgressKey, 0)));

            intent.Dispose();
        }

        internal static void SendDebug(Context context, string message)
        {
            var intent = new Intent();
            intent.SetAction(Message.Debug);
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

        internal static void SendLoadingProgress(Context context,  string message, double progress)
        {
            var intent = new Intent();
            intent.SetAction(Message.LoadingProgress);
            intent.PutExtra(Message.MessageKey, message);
            intent.PutExtra(Message.ProgressKey, progress);
            context.SendBroadcast(intent);
        }

        internal static void SendDistance(Context context, double distance, Length totalClimbs,
            Length ridingDistance, TimeSpan ridingTime, Speed topSpeed)
        {
            var intent = new Intent();
            intent.SetAction(Message.Distance);
            intent.PutExtra(Message.DistanceKey, distance);
            intent.PutExtra(Message.TotalClimbsMetersKey, totalClimbs.Meters);
            intent.PutExtra(Message.RidingDistanceMetersKey, ridingDistance.Meters);
            intent.PutExtra(Message.RidingTimeSecondsKey, ridingTime.TotalSeconds);
            intent.PutExtra(Message.TopSpeedKmPerHourKey, topSpeed.KilometersPerHour);
            context.SendBroadcast(intent);
        }

    }
}