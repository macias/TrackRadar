using System;
using System.Diagnostics;
using System.Threading;

namespace TrackRadar
{
    public sealed class SignalTimer : IDisposable
    {
        private long lastGpsPresentAtTicks;
        private long lastNoGpsAlarmAtTicks;
        private readonly Timer timer;
        private readonly Action gpsOffAlarm;
        private readonly Action gpsOnAlarm;
        private readonly Func<TimeSpan> noGpsAgainInterval;
        private readonly Func<TimeSpan> noGpsFirstTimeout;
        // consecutive counts of reports when there is no signal
        // 0 -- means we have signal
        private int alarmCounter;
        private readonly Action<LogLevel, string> logger;
        private TimeSpan defaultCheckInterval => TimeSpan.FromTicks(Math.Min(this.noGpsAgainInterval().Ticks, noGpsFirstTimeout().Ticks));

        public bool HasGpsSignal => Interlocked.CompareExchange(ref this.alarmCounter, 0, 0) == 0;

        public SignalTimer(Action<LogLevel, string> logger, Func<TimeSpan> noGpsTimeoutFactory, Func<TimeSpan> noGpsIntervalFactory,
            Action gpsOnAlarm, Action gpsOffAlarm)
        {
            this.logger = logger;
            this.noGpsAgainInterval = noGpsIntervalFactory;
            this.noGpsFirstTimeout = noGpsTimeoutFactory;
            this.gpsOnAlarm = gpsOnAlarm;
            this.gpsOffAlarm = gpsOffAlarm;

            Interlocked.Exchange(ref this.lastNoGpsAlarmAtTicks, 0);
            Interlocked.Exchange(ref this.lastGpsPresentAtTicks, 0);
            // initially we have no signal and we assume user starting the service pays attention 
            // to initial message "no signal"
            Interlocked.Exchange(ref this.alarmCounter, 1);

            this.timer = new Timer(_ => check());
            // we are setting it to timeout, not interval, because we could have such scenario
            // quick update, and then no signal -- with interval we would have to wait long time
            // with timeout the timer will be triggered sooner so we could correctly adjust the time of the alarm
            // in other words -- DO NOT use period to set the timer
            this.timer.Change(dueTime: defaultCheckInterval, period: Timeout.InfiniteTimeSpan);
        }

        public void Update(bool canAlarm)
        {
            Interlocked.Exchange(ref this.lastGpsPresentAtTicks, Stopwatch.GetTimestamp());
            if (Interlocked.Exchange(ref this.alarmCounter, 0) != 0)
            {
                logger( LogLevel.Info,"GPS signal acquired");
                if (canAlarm)
                    gpsOnAlarm();
            }
        }

        private void check()
        {
            try
            {
                TimeSpan due_time;
                if (HasGpsSignal)
                {
                    logger( LogLevel.Info, $"Signal timer for has gps {this.lastGpsPresentAtTicks}");
                    due_time = tryRaiseAlarm(ref this.lastGpsPresentAtTicks, this.noGpsFirstTimeout());
                }
                else
                {
                    logger( LogLevel.Info,$"Signal timer for no gps {this.lastNoGpsAlarmAtTicks}");
                    due_time = tryRaiseAlarm(ref this.lastNoGpsAlarmAtTicks, this.noGpsAgainInterval());
                }

                this.timer.Change(due_time, Timeout.InfiniteTimeSpan);
            }
            catch (Exception ex)
            {
                logger(LogLevel.Error, $"Timer action crash {ex}");
                this.timer.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
            }
        }

        private TimeSpan tryRaiseAlarm(ref long lastEventAtTicks, TimeSpan alarmAfter)
        {
            long now = Stopwatch.GetTimestamp();
            TimeSpan repeat = defaultCheckInterval;

            TimeSpan passed = TimeSpan.FromSeconds((now - Interlocked.CompareExchange(ref lastEventAtTicks, 0, 0)) * 1.0
                / Stopwatch.Frequency);
            TimeSpan delay = alarmAfter - passed;
            this.logger( LogLevel.Info, $"GPS signal check alarm-after {alarmAfter.Minutes} pass {passed.TotalSeconds} delay " + delay.TotalSeconds.ToString());

            if (delay <= TimeSpan.Zero)
            {
                Interlocked.Exchange(ref this.lastNoGpsAlarmAtTicks, now);
                Interlocked.Increment(ref this.alarmCounter);

                gpsOffAlarm();
                delay = repeat;
            }
            else if (delay > repeat)
                delay = repeat;

            return delay;
        }

        public void Dispose()
        {
            using (var handle = new AutoResetEvent(false))
            {
                if (this.timer.Dispose(handle))
                    handle.WaitOne();
            };
        }
    }
}