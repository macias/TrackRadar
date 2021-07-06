using System;
using System.Diagnostics;
using MathUnit;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class MockRadarService : IRadarService
    {
        TimeSpan IRadarService.OffTrackAlarmInterval => prefs.OffTrackAlarmInterval;
        Length IRadarService.OffTrackAlarmDistance => prefs.OffTrackAlarmDistance;
        Speed IRadarService.RestSpeedThreshold => prefs.RestSpeedThreshold;
        Speed IRadarService.RidingSpeedThreshold => prefs.RidingSpeedThreshold;
        TimeSpan IRadarService.TurnAheadAlarmDistance => prefs.TurnAheadAlarmDistance;
        TimeSpan IRadarService.TurnAheadAlarmInterval => prefs.TurnAheadAlarmInterval;
        TimeSpan IRadarService.DoubleTurnAlarmDistance => prefs.DoubleTurnAlarmDistance;
        Length IRadarService.DriftWarningDistance => prefs.DriftWarningDistance;
        int IRadarService.DriftMovingAwayCountLimit => prefs.DriftMovingAwayCountLimit;
        int IRadarService.DriftComingCloserCountLimit => prefs.DriftComingCloserCountLimit;
        int IRadarService.OffTrackAlarmCountLimit => this.prefs.OffTrackAlarmCountLimit;

        private readonly IPreferences prefs;
        private readonly ITimeStamper timeStamper;

        public MockRadarService(IPreferences prefs, ITimeStamper timeStamper)
        {
            this.prefs = prefs;
            this.timeStamper = timeStamper;
        }

        void ILogger.LogDebug(LogLevel level, string message)
        {
            Console.WriteLine($"[{level}] {message}");
        }

        void IRadarService.WriteCrossroad(double latitudeDegrees, double longitudeDegrees)
        {
            ; // do nothing
        }

        void IRadarService.WriteOffTrack(double latitudeDegrees, double longitudeDegrees, string name)
        {
            ; // do nothing
        }

        void IRadarService.WriteDebug(double latitudeDegrees, double longitudeDegrees, string name, string comment)
        {
            Debug.WriteLine($"{latitudeDegrees}, {longitudeDegrees} {name} {comment}");
        }
    }
}
