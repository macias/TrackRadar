using System;
using System.Diagnostics;
using System.Threading;

namespace TrackRadar.Implementation
{
    internal sealed class SignalChecker : IDisposable
    {
        private readonly object threadLock = new object();

        private bool isDisposed;
        private long lastGpsPresentAtTicks;
        private long lastNoGpsAlarmAtTicks;
        private readonly Timer timer;
        private readonly Action gpsOffAlarm;
        private readonly Action gpsOnAlarm;
        private readonly Func<TimeSpan> noGpsAgainInterval;
        private readonly Func<TimeSpan> noGpsFirstTimeout;
        // consecutive counts of reports when there is no signal
        // 0 -- means we have signal
        private int noGpsSignalCounter;
        private readonly Action<LogLevel, string> logger;
        private TimeSpan defaultCheckInterval => this.noGpsAgainInterval().Min(noGpsFirstTimeout());

        public bool HasGpsSignal => Interlocked.CompareExchange(ref this.noGpsSignalCounter, 0, 0) == 0;

        public SignalChecker(Action<LogLevel, string> logger, Func<TimeSpan> noGpsTimeoutFactory, Func<TimeSpan> noGpsIntervalFactory,
            Action gpsOnAlarm, Action gpsOffAlarm)
        {
            this.logger = logger;
            this.noGpsAgainInterval = noGpsIntervalFactory;
            this.noGpsFirstTimeout = noGpsTimeoutFactory;
            this.gpsOnAlarm = gpsOnAlarm;
            this.gpsOffAlarm = gpsOffAlarm;

            this.lastNoGpsAlarmAtTicks = 0;
            this.lastGpsPresentAtTicks = 0;
            // initially we have no signal and we assume user starting the service pays attention 
            // to initial message "no signal"
            this.noGpsSignalCounter = 1;

            this.timer = new Timer(_ => check());
            // we are setting it to timeout, not interval, because we could have such scenario
            // quick update, and then no signal -- with interval we would have to wait long time
            // with timeout the timer will be triggered sooner so we could correctly adjust the time of the alarm
            // in other words -- DO NOT use period to set the timer
            this.timer.Change(dueTime: defaultCheckInterval, period: Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            lock (this.threadLock)
            {
                if (this.isDisposed)
                    return;
                this.isDisposed = true;
            }

            using (var handle = new AutoResetEvent(false))
            {

                if (this.timer.Dispose(handle))
                    handle.WaitOne();
            }
        }

        private void check()
        {
            TimeSpan due_time;
            try
            {
                if (HasGpsSignal)
                {
                    // here in theory we have signal, but if the signal was acquired some time ago 
                    // it means we don't have signal after all

                    long last_gps_at = Interlocked.CompareExchange(ref lastGpsPresentAtTicks, 0, 0);
                    // logger(LogLevel.Verbose, $"{nameof(SignalTimer)} Last gps update at {last_gps_at}");
                    due_time = tryRaiseAlarm(last_gps_at, this.noGpsFirstTimeout());
                }
                else
                {
                    logger(LogLevel.Verbose, $"{nameof(SignalChecker)} Last no-gps alarm at {this.lastNoGpsAlarmAtTicks}");
                    due_time = tryRaiseAlarm(lastNoGpsAlarmAtTicks, this.noGpsAgainInterval());
                }

            }
            catch (Exception ex)
            {
                logger(LogLevel.Error, $"Timer action crash {ex}");
                due_time = TimeSpan.FromSeconds(10);
            }

            lock (this.threadLock)
            {
                if (!this.isDisposed)
                    this.timer.Change(due_time, Timeout.InfiniteTimeSpan);
            }
        }

        private TimeSpan tryRaiseAlarm(long lastEventAtTicks, TimeSpan alarmAfter)
        {
            long now = Stopwatch.GetTimestamp();
            TimeSpan interval = defaultCheckInterval;

            TimeSpan passed = TimeSpan.FromSeconds((now - lastEventAtTicks) * 1.0 / Stopwatch.Frequency);
            TimeSpan delay = alarmAfter - passed;
            // this.logger(LogLevel.Verbose, $"GPS signal check alarm-after {alarmAfter.Minutes} passed {passed.TotalSeconds} delay " + delay.TotalSeconds.ToString());

            if (delay <= TimeSpan.Zero) // we passed the alarm timeout
            {
                this.lastNoGpsAlarmAtTicks = now;
                Interlocked.Increment(ref this.noGpsSignalCounter);

                gpsOffAlarm();
                return interval;
            }
            else
                // 1 second is arbitrary to avoid going overboard with trimming the time for the next check
                return TimeSpan.FromSeconds(1).Max(interval.Min(delay)); // trim the value so we won't check for alarm too late
        }

        public void UpdateGpsIsOn(bool canAlarm)
        {
            Interlocked.Exchange(ref this.lastGpsPresentAtTicks, Stopwatch.GetTimestamp());
            if (Interlocked.Exchange(ref this.noGpsSignalCounter, 0) != 0)
            {
                logger(LogLevel.Verbose, "GPS signal acquired");
                if (canAlarm)
                    gpsOnAlarm();
                else
                    logger(LogLevel.Verbose, "GPS-ON, Skipping alarming");
            }
        }

    }
}