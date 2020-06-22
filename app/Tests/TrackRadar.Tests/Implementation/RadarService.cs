using System;
using System.Collections.Generic;
using System.Linq;
using MathUnit;
using TrackRadar.Implementation;

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

        private readonly Dictionary<Alarm, int> alarmCount;
        private readonly List<(Alarm alarm, int pointIndex)> alarms;
        private readonly IPreferences prefs;
        private readonly ITimeStamper timeStamper;
        private int pointIndex;
        private long lastAlarmAt;

        public IReadOnlyDictionary<Alarm, int> AlarmCounters => this.alarmCount;
        public IReadOnlyList<(Alarm alarm, int index)> Alarms => alarms;


        public RadarService(IPreferences prefs, ITimeStamper timeStamper)
        {
            this.alarmCount = LinqExtension.GetEnums<Alarm>().ToDictionary(alarm => alarm, _ => 0);
            this.alarms = new List<(Alarm alarm, int pointIndex)>();
            this.prefs = prefs;
            this.timeStamper = timeStamper;
        }

        bool IRadarService.TryAlarm(Alarm alarm, out string reason)
        {
            this.alarms.Add((alarm, pointIndex));
            ++alarmCount[alarm];
           // Console.WriteLine($"ALARM {alarm}");
            this.lastAlarmAt = timeStamper.GetTimestamp();
            reason = null;
            return true;
        }

        void IRadarService.LogDebug(LogLevel level, string message)
        {
            //Console.WriteLine($"[{level}] {message}");
        }

        void IRadarService.WriteCrossroad(double latitudeDegrees, double longitudeDegrees)
        {
            ; // do nothing
        }

        void IRadarService.WriteOffTrack(double latitudeDegrees, double longitudeDegrees, string name = null)
        {
            ; // do nothing
        }

        internal void SetPointIndex(int index)
        {
            this.pointIndex = index;
        }

        public bool TryGetLatestTurnAheadAlarmAt(out long timeStamp)
        {
            timeStamp = this.lastAlarmAt;
            return true;
        }
    }
}
