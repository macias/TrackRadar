using System;

namespace TrackRadar.Implementation
{
    internal sealed class AlarmSequencer : IAlarmSequencer
    {
        private const ulong disengageMask = (1UL << (int)Alarm.Disengage);
        private const ulong engagedMask = (1UL << (int)Alarm.Engaged);

        private readonly ILogger logger;
        private readonly IAlarmMaster master;
        private readonly IDisposable disposable;
        private Alarm? pendingAlarm;
        private ulong notificationMask;
        private ulong failedMask;
        private Alarm? alarmPlayed;

        public TimeSpan MaxTurnDuration => this.master.MaxTurnDuration;

        public AlarmSequencer(ILogger logger, IAlarmMaster master)
        {
            this.logger = logger;
            this.master = master;
            this.disposable = Disposable.Create(closeContext);
        }

        public void NotifyAlarm(Alarm alarm)
        {
            // notifying about alarm is a way to say that we are at state for given alarm, but cannot play it
            // (because for example interval between alarms prevents it), so any other alarm should be played
            // also this influences already pending alarms
            this.notificationMask |= (1UL << alarmNotificationCategory(alarm));
        }

        private int alarmNotificationCategory(Alarm alarm)
        {
            // for notification mask we slash some alarms into one general, it already happened in test
            // the primary turn point caused crossroad notification and this blocked alarm on secondary turn point
            switch (alarm)
            {
                case Alarm.Crossroad: return (int)Alarm.Crossroad;
                case Alarm.GoAhead: return (int)Alarm.Crossroad;
                case Alarm.LeftCross: return (int)Alarm.Crossroad;
                case Alarm.LeftEasy: return (int)Alarm.Crossroad;
                case Alarm.LeftSharp: return (int)Alarm.Crossroad;
                case Alarm.RightCross: return (int)Alarm.Crossroad;
                case Alarm.RightEasy: return (int)Alarm.Crossroad;
                case Alarm.RightSharp: return (int)Alarm.Crossroad;

                case Alarm.DoubleTurn: return (int)Alarm.Crossroad;

                default: return (int)alarm;
            }
        }

        public bool TryAlarm(Alarm alarm, out string reason)
        {
            if (this.alarmPlayed.HasValue)
            {
                if (this.alarmPlayed == alarm)
                {
                    reason = null;
                    return true;
                }
                else
                {
                    reason = $"Single alarm per cycle, {this.alarmPlayed} played";
                    this.failedMask |= (1UL << (int)alarm);
                    return false;
                }
            }
            else if (notificationMask != 0)
            {
                if ((notificationMask & (1UL << alarmNotificationCategory(alarm))) == notificationMask) // notification only about this alarm
                {
                    this.notificationMask = 0;
                    return TryAlarm(alarm, out reason);
                }
                else
                {
                    reason = $"We are notified about alarm {this.notificationMask}";
                    this.failedMask |= (1UL << (int)alarm);
                    return false;
                }
            }
            else if (master.TryAlarm(alarm, out reason))
            {
                this.alarmPlayed = alarm;
                return true;
            }
            else
            {
                this.failedMask |= (1UL << (int)alarm);
                return false;
            }
        }

        public bool TryGetLatestTurnAheadAlarmAt(out long timeStamp)
        {
            return master.TryGetLatestTurnAheadAlarmAt(out timeStamp);
        }

        public void PostMessage(string reason)
        {
            master.PostMessage(reason);
        }

        public IDisposable OpenAlarmContext(bool gpsAcquired, bool hasGpsSignal)
        {
            if (gpsAcquired)
            {
                if (!this.pendingAlarm.HasValue)
                    this.pendingAlarm = Alarm.GpsAcquired;
            }
            else if (!hasGpsSignal)
            {
                if (this.pendingAlarm == Alarm.GpsAcquired)
                    this.pendingAlarm = null;
            }

            return this.disposable;
        }

        private void closeContext()
        {
            if (!alarmPlayed.HasValue
                // we might have other failures but we have to save for futre only dis/engage
                && (this.failedMask & (disengageMask | engagedMask)) != 0
                // dis/engage override gps acquired (without gps we wouldn't trigger those alarms)
                && (!pendingAlarm.HasValue || pendingAlarm == Alarm.GpsAcquired
                    || pendingAlarm == Alarm.Disengage || pendingAlarm == Alarm.Engaged))
            {
                this.pendingAlarm = (this.failedMask & disengageMask) != 0 ? Alarm.Disengage : Alarm.Engaged;
            }
            else if (pendingAlarm.HasValue)
            {
                if (this.alarmPlayed.HasValue) // every played alarm renders useless both Dis/engage and GpsAcquired
                    pendingAlarm = null;
                else if (this.notificationMask == 0 && this.failedMask == 0) // nothing was even tried
                {
                    if (master.TryAlarm(pendingAlarm.Value, out string reason))
                    {
                        pendingAlarm = null;
                    }
                    else
                        logger.LogDebug(LogLevel.Info, $"Cannot play {pendingAlarm}, reason: {reason}");
                }
            }

            this.notificationMask = 0;
            this.failedMask = 0;
            this.alarmPlayed = null;
        }

    }
}