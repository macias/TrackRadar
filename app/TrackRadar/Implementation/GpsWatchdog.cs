using System;

namespace TrackRadar.Implementation
{
    internal sealed class GpsWatchdog : IDisposable
    {
        private readonly object threadLock = new object();
        private readonly ISignalCheckerService service;
        private readonly ITimeStamper timeStamper;
        private readonly long startAt;
        private bool isDisposed;
        private long lastGpsPresentAtTicks;
        private long lastNoGpsAlarmAtTicks;
        private readonly ITimer timer;
        private uint gpsSignalCounter; // with GPS signal every 1sec it takes 136 years to overflow
        private ulong DEBUG_CHECKS;
        private ulong DEBUG_NO_GPS;
        private ulong DEBUG_ALARMS;

        public bool HasGpsSignal => System.Threading.Interlocked.CompareExchange(ref this.lastNoGpsAlarmAtTicks, 0, 0) == timeStamper.GetBeforeTimeTimestamp();

        public GpsWatchdog(ISignalCheckerService service, ITimeStamper timeStamper)
        {
            this.service = service;
            this.timeStamper = timeStamper;

            // initially we have no signal and we assume user starting the service pays attention 
            // to initial message "no signal"
            this.startAt = this.timeStamper.GetTimestamp();
            this.lastNoGpsAlarmAtTicks = startAt - (long)(service.NoGpsAgainInterval.TotalSeconds * this.timeStamper.Frequency);
            this.lastGpsPresentAtTicks = startAt;

            this.service.Log(LogLevel.Verbose, $"Starting watchdog with no-gps-interval {service.NoGpsAgainInterval.TotalSeconds}");
            this.timer = service.CreateTimer(check);
            // we are setting it to timeout, not interval, because we could have such scenario
            // quick update, and then no signal -- with interval we would have to wait long time
            // with timeout the timer will be triggered sooner so we could correctly adjust the time of the alarm
            // in other words -- DO NOT use period to set the timer
            this.timer.Change(dueTime: TimeSpan.FromSeconds(1), period: System.Threading.Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            lock (this.threadLock)
            {
                this.service.Log(LogLevel.Info, $"Watchdog disposing {DEBUG_CHECKS}, {DEBUG_NO_GPS}, {DEBUG_ALARMS}");

                if (this.isDisposed)
                    return;
                this.isDisposed = true;
            }

            this.timer.Dispose();
        }

        private void check()
        {
            lock (this.threadLock)
            {
                if (this.isDisposed)
                {
                    return;
                }

                long now = this.timeStamper.GetTimestamp();
                if (timeStamper.GetSecondsSpan(now, this.lastGpsPresentAtTicks) > service.NoGpsFirstTimeout.TotalSeconds)
                {
                    ++this.DEBUG_NO_GPS;

                    if (gpsSignalCounter != 0)
                        this.service.Log(LogLevel.Verbose, $"Watchdog: we lost GPS signal {now}/{this.lastNoGpsAlarmAtTicks}");

                    gpsSignalCounter = 0;

                    if (timeStamper.GetSecondsSpan(now, this.lastNoGpsAlarmAtTicks) > service.NoGpsAgainInterval.TotalSeconds)
                    {
                        try
                        {
                            service.GpsOffAlarm(DEBUG_ALARMS.ToString());
                            ++this.DEBUG_ALARMS;
                        }
                        catch (Exception ex)
                        {
                            service.Log(LogLevel.Error, $"Timer action crash {ex}");
                        }

                        this.lastNoGpsAlarmAtTicks = now;
                    }
                }

                if (this.DEBUG_CHECKS % 900 == 0) // every 15 minutes
                    this.service.Log(LogLevel.Verbose, $"Gps watchdog stats {DEBUG_CHECKS}, {DEBUG_NO_GPS}, {DEBUG_ALARMS}");

                ++this.DEBUG_CHECKS;

                this.timer.Change(TimeSpan.FromSeconds(1), System.Threading.Timeout.InfiniteTimeSpan);
            }
        }

        public bool UpdateGpsIsOn()
        {
            lock (this.threadLock)
            {
                this.lastGpsPresentAtTicks = timeStamper.GetTimestamp();

                if (++this.gpsSignalCounter < 10)
                    return false;

                if (this.lastNoGpsAlarmAtTicks != this.timeStamper.GetBeforeTimeTimestamp())
                {
                    this.lastNoGpsAlarmAtTicks = this.timeStamper.GetBeforeTimeTimestamp();

                    service.Log(LogLevel.Info, $"GPS signal reacquired in {TimeSpan.FromSeconds(timeStamper.GetSecondsSpan(this.lastGpsPresentAtTicks, this.startAt))}");

                    return true;
                }

                return false;
            }
        }

    }
}