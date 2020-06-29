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
        private readonly AlarmMaster alarmMaster;
        private int pointIndex;

        public IReadOnlyDictionary<Alarm, int> AlarmCounters => this.alarmCount;
        public IReadOnlyList<(Alarm alarm, int index)> Alarms => alarms;


        public RadarService(IPreferences prefs, ITimeStamper timeStamper, AlarmMaster alarmMaster)
        {
            this.alarmCount = LinqExtension.GetEnums<Alarm>().ToDictionary(alarm => alarm, _ => 0);
            this.alarms = new List<(Alarm alarm, int pointIndex)>();
            this.prefs = prefs;
            this.timeStamper = timeStamper;
            this.alarmMaster = alarmMaster;
        }

        bool IRadarService.TryAlarm(Alarm alarm, out string reason)
        {
            if (!this.alarmMaster.TryPlay(alarm, out reason))
                return false;

            
                this.alarms.Add((alarm, pointIndex));
                ++alarmCount[alarm];
                // Console.WriteLine($"ALARM {alarm}");
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

        void IRadarService.WriteOffTrack(double latitudeDegrees, double longitudeDegrees, string name)
        {
            ; // do nothing
        }

        internal void SetPointIndex(int index)
        {
            this.pointIndex = index;
        }

        public bool TryGetLatestTurnAheadAlarmAt(out long timeStamp)
        {
            return this.alarmMaster.TryGetLatestTurnAheadAlarmAt(out timeStamp);
        }
    }
}
