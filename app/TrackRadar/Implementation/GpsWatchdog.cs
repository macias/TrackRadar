using System;

namespace TrackRadar.Implementation
{
    internal sealed class GpsWatchdog : IDisposable
    {
        private const int stableGpsAcquirementCountLimit = 10;

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

        // when we have signal we check it rarely, but if we don't we check it every second
        private TimeSpan nextCheckAfter;

        public bool HasGpsSignal => System.Threading.Interlocked.CompareExchange(ref this.lastNoGpsAlarmAtTicks, 0, 0) == timeStamper.GetBeforeTimeTimestamp();

        public GpsWatchdog(ISignalCheckerService service, ITimeStamper timeStamper)
        {
            this.service = service;
            this.timeStamper = timeStamper;

            this.startAt = this.timeStamper.GetTimestamp();
            // initially we have no signal but we don't alarm user about it right away -- we assume user 
            // when starting the service pays attention that we are at acquisition stage
            this.lastNoGpsAlarmAtTicks = startAt - (long)(service.NoGpsAgainInterval.TotalSeconds * this.timeStamper.Frequency);
            this.lastGpsPresentAtTicks = startAt;

            this.service.Log(LogLevel.Verbose, $"Starting watchdog with no-gps-interval {service.NoGpsAgainInterval.TotalSeconds}");
            this.timer = service.CreateTimer(check);

            // DO NOT use period to set the timer, even with fixed rate
            this.nextCheckAfter = GpsInfo.WEAK_updateRate;
            this.timer.Change(dueTime: nextCheckAfter, period: System.Threading.Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            lock (this.threadLock)
            {
                this.service.Log(LogLevel.Info, $"Watchdog disposing {stats(this.timeStamper.GetTimestamp())}");

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
                    return;

                long now = this.timeStamper.GetTimestamp();
                // get double of the refresh rate to avoid minor slippage
                double margin_for_gps_update = Math.Min(service.NoGpsFirstTimeout.TotalSeconds, 2 * GpsInfo.WEAK_updateRate.TotalSeconds);
                if (timeStamper.GetSecondsSpan(now, this.lastGpsPresentAtTicks) > margin_for_gps_update)
                {
                    ++this.DEBUG_NO_GPS;

                    if (gpsSignalCounter != 0)
                        this.service.Log(LogLevel.Verbose, $"Watchdog: we lost GPS signal. Stats {stats(now)}");

                    gpsSignalCounter = 0;

                    service.AcquireGps();

                    this.nextCheckAfter = GpsInfo.WEAK_updateRate;

                    if (timeStamper.GetSecondsSpan(now, this.lastGpsPresentAtTicks) > service.NoGpsFirstTimeout.TotalSeconds
                        && timeStamper.GetSecondsSpan(now, this.lastNoGpsAlarmAtTicks) > service.NoGpsAgainInterval.TotalSeconds)
                    {
                        try
                        {

                            service.GpsOffAlarm(stats(now));
                            ++this.DEBUG_ALARMS;
                        }
                        catch (Exception ex)
                        {
                            service.Log(LogLevel.Error, $"Timer action crash {ex}");
                        }

                        this.lastNoGpsAlarmAtTicks = now;
                    }
                }

                if (this.DEBUG_CHECKS % 300 == 0) // every 5 minutes
                    this.service.Log(LogLevel.Verbose, $"Gps watchdog stats {stats(now)}");

                ++this.DEBUG_CHECKS;

                this.timer.Change(nextCheckAfter, System.Threading.Timeout.InfiniteTimeSpan);
            }
        }

        private string stats(long now)
        {
            return $"stats: {DEBUG_CHECKS}, {DEBUG_NO_GPS}, {DEBUG_ALARMS}; now {now}/{this.timeStamper.Frequency}; gps {this.lastGpsPresentAtTicks}, timeout {service.NoGpsFirstTimeout.TotalSeconds}s; no-gps {this.lastNoGpsAlarmAtTicks}, interval {service.NoGpsAgainInterval.TotalSeconds}s";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if signal was RE-acquired</returns>
        public bool UpdateGpsIsOn()
        {
            lock (this.threadLock)
            {
                this.lastGpsPresentAtTicks = timeStamper.GetTimestamp();

                if (++this.gpsSignalCounter < stableGpsAcquirementCountLimit)
                {
                    service.Log(LogLevel.Verbose, $"Fresh GPS signal received {this.lastGpsPresentAtTicks}, {service.NoGpsFirstTimeout.TotalSeconds}, freq. {this.timeStamper.Frequency}");
                    return false;
                }

                this.nextCheckAfter = service.NoGpsFirstTimeout;

                // if we lost signal some time ago, but now it is back
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