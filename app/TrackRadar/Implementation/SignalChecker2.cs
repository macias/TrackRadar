using System;

namespace TrackRadar.Implementation
{
    internal sealed class SignalChecker2 : IDisposable
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

        public bool HasGpsSignal => System.Threading.Interlocked.CompareExchange(ref this.lastNoGpsAlarmAtTicks, 0, 0) == timeStamper.GetBeforeTimeTimestamp();

        public SignalChecker2(ISignalCheckerService service, ITimeStamper timeStamper)
        {
            this.service = service;
            this.timeStamper = timeStamper;

            // initially we have no signal and we assume user starting the service pays attention 
            // to initial message "no signal"
            this.startAt = this.timeStamper.GetTimestamp();
            this.lastNoGpsAlarmAtTicks = startAt - (long)(service.NoGpsAgainInterval.TotalSeconds * this.timeStamper.Frequency);
            this.lastGpsPresentAtTicks = startAt;

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
                if (timeStamper.GetSecondsSpan(now, this.lastGpsPresentAtTicks) > service.NoGpsFirstTimeout.TotalSeconds)
                {
                    service.RequestGps();

                    gpsSignalCounter = 0;

                    if (timeStamper.GetSecondsSpan(now, this.lastNoGpsAlarmAtTicks) > service.NoGpsAgainInterval.TotalSeconds)
                    {
                        try
                        {
                            service.GpsOffAlarm();
                        }
                        catch (Exception ex)
                        {
                            service.Log(LogLevel.Error, $"Timer action crash {ex}");
                        }

                        this.lastNoGpsAlarmAtTicks = now;
                    }
                }

                this.timer.Change(TimeSpan.FromSeconds(1), System.Threading.Timeout.InfiniteTimeSpan);
            }
        }

        public void UpdateGpsIsOn(bool canAlarm)
        {
            lock (this.threadLock)
            {
                this.lastGpsPresentAtTicks = timeStamper.GetTimestamp();

                if (++this.gpsSignalCounter < 10)
                    return;

                if (this.lastNoGpsAlarmAtTicks != this.timeStamper.GetBeforeTimeTimestamp())
                {
                    this.lastNoGpsAlarmAtTicks = this.timeStamper.GetBeforeTimeTimestamp();

                    if (canAlarm)
                        service.GpsOnAlarm();

                    service.Log(LogLevel.Info, $"GPS signal reacquired in {TimeSpan.FromSeconds(timeStamper.GetSecondsSpan(this.lastGpsPresentAtTicks, this.startAt))}");
                }
            }
        }

    }
}