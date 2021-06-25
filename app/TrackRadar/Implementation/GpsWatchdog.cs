using System;

namespace TrackRadar.Implementation
{
    internal sealed class GpsWatchdog : IDisposable
    {
        // https://en.wikipedia.org/wiki/Leaky_bucket
        private const double signalBucketLowLevelFraction = 0.5;

        // setter is for unit tests
        //internal static int StableSignalAcquisitionCountLimit { get; set; } = 10;

        private readonly object threadLock = new object();
        private readonly ISignalCheckerService service;
        private readonly ITimeStamper timeStamper;
        private readonly long startAt;
        private bool isDisposed;
        private long lastSignalAtTicks;
        //private long lastStableSignalAtTicks;
        private long lastNoSignalAlarmAtTicks;
        private readonly ITimer timer;
        private double gpsSignalCounter;
        private ulong DEBUG_CHECKS;
        private ulong DEBUG_NO_GPS;
        private ulong DEBUG_ALARMS;

        // when we have signal we check it rarely, but if we don't we check it every second
        private TimeSpan nextCheckAfter;
        private bool hasStableSignal;

        public bool HasGpsSignal { get { lock (this.threadLock) return this.hasStableSignal; } }

        public GpsWatchdog(ISignalCheckerService service, ITimeStamper timeStamper)
        {
            this.service = service;
            this.timeStamper = timeStamper;

            this.startAt = this.timeStamper.GetTimestamp();
            // initially we have no signal but we don't alarm user about it right away -- we assume user 
            // when starting the service pays attention that we are at acquisition stage
            this.lastNoSignalAlarmAtTicks = timeStamper.GetBeforeTimeTimestamp();// startAt - (long)(service.NoGpsAgainInterval.TotalSeconds * this.timeStamper.Frequency);
            this.lastSignalAtTicks = startAt;
            //this.lastStableSignalAtTicks = startAt;

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
                bool initiated = timeStamper.GetSecondsSpan(now, startAt) > service.GpsAcquisitionTimeout.TotalSeconds;

                if (detectedUpdatesLoss(receivedSignal: false, out double signal_counter))
                {
                    ++this.DEBUG_NO_GPS;

                    service.AcquireGps();

                    this.nextCheckAfter = GpsInfo.WEAK_updateRate;

                    if (initiated && signal_counter < service.GpsAcquisitionTimeout.TotalSeconds * signalBucketLowLevelFraction)
                    {
                        this.hasStableSignal = false;
                        this.service.Log(LogLevel.Verbose, $"Watchdog: we lost GPS signal. Stats {stats(now)}");
                    }
                }


                //double stable_signal_passed = timeStamper.GetSecondsSpan(now, this.lastStableSignalAtTicks);
                double last_alarm_passed = timeStamper.GetSecondsSpan(now, this.lastNoSignalAlarmAtTicks);

                if (initiated && !hasStableSignal && last_alarm_passed > service.NoGpsAgainInterval.TotalSeconds)
                {
                    try
                    {
                        if (service.GpsOffAlarm(stats(now)))
                            this.lastNoSignalAlarmAtTicks = now;
                        ++this.DEBUG_ALARMS;
                    }
                    catch (Exception ex)
                    {
                        service.Log(LogLevel.Error, $"Timer action crash {ex}");
                    }

                }


                if (this.DEBUG_CHECKS % 300 == 0) // every 5 minutes
                    this.service.Log(LogLevel.Verbose, $"Gps watchdog stats {stats(now)}");

                ++this.DEBUG_CHECKS;

                this.timer.Change(nextCheckAfter, System.Threading.Timeout.InfiniteTimeSpan);
            }
        }

        private bool detectedUpdatesLoss(bool receivedSignal, out double signalCounter)
        {
            long now = this.timeStamper.GetTimestamp();
            // it like fence counting, if last was 0, and now is 2, 2-0 means there is one piece missing (not two)
            double lost_updates = timeStamper.GetSecondsSpan(now, this.lastSignalAtTicks) / GpsInfo.WEAK_updateRate.TotalSeconds
                - (receivedSignal ? 1 : 0);

            // give us some slack to avoid minor processing slippage
            bool is_real_loss = lost_updates > 0.5;

            // make sense only for real loss in updates
            // btw. we cannot update instance counter directly, because this method could be called in absence
            // of gps signal and in such case we cannot change counter and time when we get signal (because we didn't)
            signalCounter = Math.Max(0, this.gpsSignalCounter - lost_updates);

            return is_real_loss;
        }

        private string stats(long now)
        {
            return $"stats: {DEBUG_CHECKS}, {DEBUG_NO_GPS}, {DEBUG_ALARMS}; now {now}/{this.timeStamper.Frequency}; gps {this.lastSignalAtTicks}, timeout {service.GpsAcquisitionTimeout.TotalSeconds}s; no-gps {this.lastNoSignalAlarmAtTicks}, interval {service.NoGpsAgainInterval.TotalSeconds}s";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if signal was RE-acquired</returns>
        public bool UpdateGpsIsOn()
        {
            lock (this.threadLock)
            {
                if (detectedUpdatesLoss(receivedSignal: true, out double signal_counter))
                    this.gpsSignalCounter = signal_counter;
                else
                    this.gpsSignalCounter = Math.Min(service.GpsAcquisitionTimeout.TotalSeconds, this.gpsSignalCounter + 1);

                this.lastSignalAtTicks = timeStamper.GetTimestamp();

                bool prev_stable = this.hasStableSignal;

                if (this.gpsSignalCounter >= service.GpsAcquisitionTimeout.TotalSeconds - 1)
                {
                    this.nextCheckAfter = service.GpsAcquisitionTimeout;
                    this.hasStableSignal = true;
                }

                // this.lastStableSignalAtTicks = this.lastSignalAtTicks;

                if (!this.hasStableSignal)
                {
                    service.Log(LogLevel.Verbose, $"Fresh GPS signal received {this.lastSignalAtTicks}, {service.GpsAcquisitionTimeout.TotalSeconds}, freq. {this.timeStamper.Frequency}");
                    return false;
                }
                // if we lost signal some time ago, but now it is back
                else if (!prev_stable)
                //if (this.lastNoSignalAlarmAtTicks != this.timeStamper.GetBeforeTimeTimestamp())
                {
                    this.lastNoSignalAlarmAtTicks = this.timeStamper.GetBeforeTimeTimestamp();

                    service.Log(LogLevel.Info, $"GPS signal reacquired in {TimeSpan.FromSeconds(timeStamper.GetSecondsSpan(this.lastSignalAtTicks, this.startAt))}");

                    return true;
                }

                return false;
            }
        }

    }
}