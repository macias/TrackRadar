using System;

namespace TrackRadar.Implementation
{
    internal sealed class GpsWatchdog : IDisposable
    {
        private enum State
        {
            Created,
            Started,
            Disposed,
        }

        private const double slippageMargin = 0.5;

        // https://en.wikipedia.org/wiki/Leaky_bucket

        private readonly object threadLock = new object();
        private readonly ISignalCheckerService service;
        private readonly ITimeStamper timeStamper;
        private readonly TimeSpan gpsAcquisitionTimeout;
        private readonly TimeSpan gpsLossTimeout;
        private readonly TimeSpan noGpsAgainInterval;
        private long startAt;
        private State state;
        private long lastSignalAtTicks;
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

        public GpsWatchdog(ISignalCheckerService service, ITimeStamper timeStamper, TimeSpan gpsAcquisitionTimeout,
            TimeSpan gpsLossTimeout, TimeSpan noGpsAgainInterval)
        {
            this.service = service;
            this.timeStamper = timeStamper;

            this.gpsAcquisitionTimeout = gpsAcquisitionTimeout;
            this.gpsLossTimeout = gpsLossTimeout.Min(gpsAcquisitionTimeout);
            this.noGpsAgainInterval = noGpsAgainInterval;

            this.state = State.Created;

            this.service.Log(LogLevel.Verbose, $"Starting watchdog with no-gps-interval {noGpsAgainInterval.TotalSeconds}");
            this.timer = service.CreateTimer(check);
        }

        public void Dispose()
        {
            lock (this.threadLock)
            {
                if (this.state == State.Disposed)
                    return;
                this.state = State.Disposed;
            }

            this.timer.Dispose();
        }

        public void Start()
        {
            lock (this.threadLock)
            {
                if (this.state != State.Created)
                    return;

                this.startAt = this.timeStamper.GetTimestamp();
                // initially we have no signal but we don't alarm user about it right away -- we assume user 
                // when starting the service pays attention that we are at acquisition stage
                this.lastNoSignalAlarmAtTicks = timeStamper.GetBeforeTimeTimestamp();// startAt - (long)(service.NoGpsAgainInterval.TotalSeconds * this.timeStamper.Frequency);
                this.lastSignalAtTicks = startAt;

                // DO NOT use period to set the timer, even with fixed rate
                this.nextCheckAfter = GpsInfo.WEAK_updateRate;
                this.timer.Change(dueTime: nextCheckAfter, period: System.Threading.Timeout.InfiniteTimeSpan);

                this.state = State.Started;
            }
        }

        private string stats(long now,double counter)
        {
            return $"stats: {DEBUG_CHECKS}, {DEBUG_NO_GPS}, {DEBUG_ALARMS}; now {now}/{this.timeStamper.Frequency}; seen {this.lastSignalAtTicks}, alarm {this.lastNoSignalAlarmAtTicks}, stable {hasStableSignal}, counter {(counter.ToString("0.#"))}";
        }

        private void check()
        {
            lock (this.threadLock)
            {
                if (this.state != State.Started)
                    return;

                long now = this.timeStamper.GetTimestamp();
                bool warmed_up = timeStamper.GetSecondsSpan(now, startAt) > this.gpsLossTimeout.TotalSeconds;

                if (detectedUpdatesLoss(receivedSignal: false, out double signal_counter))
                {
                    ++this.DEBUG_NO_GPS;

                    service.AcquireGps();

                    this.nextCheckAfter = GpsInfo.WEAK_updateRate;

                    if (warmed_up && signal_counter <= (this.gpsAcquisitionTimeout - this.gpsLossTimeout).TotalSeconds)
                    {
                        this.service.Log(LogLevel.Verbose, $"Watchdog: we lost GPS signal. Stats {stats(now,signal_counter)}");

                        // if we had stable signal and now it dropped below threshold kill it entirely so we have to reacquire
                        // it completely, think about case: acquisition time timeout 20 second, loss timeout 4
                        // we have counter 15, so to be sure we have the signal and to avoid ping-pong effect
                        // we reset it to 0, so now we have to get it 20 times (not just 5) before stating we have it back for sure
                        if (this.hasStableSignal)
                            this.gpsSignalCounter = 0;

                        this.hasStableSignal = false;
                    }
                }

                double last_alarm_passed = timeStamper.GetSecondsSpan(now, this.lastNoSignalAlarmAtTicks);

                if (warmed_up && !hasStableSignal && last_alarm_passed > noGpsAgainInterval.TotalSeconds)
                {
                    try
                    {
                        if (service.GpsOffAlarm(stats(now, this.gpsSignalCounter)))
                            this.lastNoSignalAlarmAtTicks = now;
                        ++this.DEBUG_ALARMS;
                    }
                    catch (Exception ex)
                    {
                        service.Log(LogLevel.Error, $"Timer action crash {ex}");
                    }

                }


                if (this.DEBUG_CHECKS % 300 == 0) // every 5 minutes
                    this.service.Log(LogLevel.Verbose, $"Gps watchdog stats {stats(now, this.gpsSignalCounter)}");

                ++this.DEBUG_CHECKS;

                this.timer.Change(nextCheckAfter, System.Threading.Timeout.InfiniteTimeSpan);
            }
        }

        private bool detectedUpdatesLoss(bool receivedSignal, out double signalCounter)
        {
            long now = this.timeStamper.GetTimestamp();
            // it like fence counting, if last was 0, and now is 2, 2-0 means there is one piece missing (not two)
            int current_counted = (receivedSignal ? 1 : 0);
            double lost_updates = timeStamper.GetSecondsSpan(now, this.lastSignalAtTicks) / GpsInfo.WEAK_updateRate.TotalSeconds
                - current_counted;

            // give us some slack to avoid minor processing slippage
            bool is_real_loss = lost_updates > slippageMargin;

            // make sense only for real loss in updates
            // btw. we cannot update instance counter directly, because this method could be called in absence
            // of gps signal and in such case we cannot change counter and time when we get signal (because we didn't)
            signalCounter = Math.Max(0, this.gpsSignalCounter - lost_updates + current_counted);

            return is_real_loss;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if signal was RE-acquired</returns>
        public bool UpdateGpsIsOn()
        {
            lock (this.threadLock)
            {
                if (this.state != State.Started)
                    return false;

                if (detectedUpdatesLoss(receivedSignal: true, out double signal_counter))
                    this.gpsSignalCounter = signal_counter;
                else
                    this.gpsSignalCounter = Math.Min(this.gpsAcquisitionTimeout.TotalSeconds, this.gpsSignalCounter + 1);

                this.lastSignalAtTicks = timeStamper.GetTimestamp();

                bool prev_stable = this.hasStableSignal;

                if (this.gpsSignalCounter >= this.gpsAcquisitionTimeout.TotalSeconds - slippageMargin)
                {
                    this.nextCheckAfter = this.gpsLossTimeout;
                    this.hasStableSignal = true;
                }

                if (!this.hasStableSignal)
                {
                    service.Log(LogLevel.Verbose, $"Fresh GPS signal received {this.lastSignalAtTicks}, {gpsAcquisitionTimeout.TotalSeconds}, freq. {this.timeStamper.Frequency}");
                    return false;
                }
                // if we lost signal some time ago, but now it is back
                else if (!prev_stable)
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