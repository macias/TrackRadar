using System;
using System.Diagnostics;
using MathUnit;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class RadarService : IRadarService
    {
        public TimeSpan OffTrackAlarmInterval => prefs.OffTrackAlarmInterval;
        public Length OffTrackAlarmDistance => prefs.OffTrackAlarmDistance;
        public Speed RestSpeedThreshold => prefs.RestSpeedThreshold;
        public Speed RidingSpeedThreshold => prefs.RidingSpeedThreshold;
        public TimeSpan TurnAheadAlarmDistance => prefs.TurnAheadAlarmDistance;
        public TimeSpan TurnAheadAlarmInterval => prefs.TurnAheadAlarmInterval;
        public TimeSpan DoubleTurnAlarmDistance => prefs.DoubleTurnAlarmDistance;

        private readonly IPreferences prefs;
        private readonly ITimeStamper timeStamper;

        public RadarService(IPreferences prefs, ITimeStamper timeStamper)
        {
            this.prefs = prefs;
            this.timeStamper = timeStamper;
        }

        void ILogger.LogDebug(LogLevel level, string message)
        {
            //Console.WriteLine($"[{level}] {message}");
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
