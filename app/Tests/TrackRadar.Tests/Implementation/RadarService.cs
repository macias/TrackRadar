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
        public Length TurnAheadAlarmDistance => prefs.TurnAheadAlarmDistance;
        public TimeSpan TurnAheadAlarmInterval => prefs.TurnAheadAlarmInterval;

        private readonly Dictionary<Alarm, int> alarmCount;
        private readonly List<(Alarm alarm, int index)> alarms;
        private readonly IPreferences prefs;
        private int index;

        public IReadOnlyDictionary<Alarm, int> AlarmCount => this.alarmCount;
        public IEnumerable<(Alarm alarm, int index)> Alarms => alarms;


        public RadarService(IPreferences prefs)
        {
            this.alarmCount = LinqExtension.GetEnums<Alarm>().ToDictionary(alarm => alarm, _ => 0);
            this.alarms = new List<(Alarm alarm, int index)>();
            this.prefs = prefs;
        }

        bool IRadarService.TryAlarm(Alarm alarm, out string reason)
        {
            this.alarms.Add((alarm, index));
            ++alarmCount[alarm];
            Console.WriteLine($"ALARM {alarm}");
            reason = null;
            return true;
        }

        void IRadarService.LogDebug(LogLevel level, string message)
        {
            Console.WriteLine($"[{level}] {message}");
        }

        void IRadarService.WriteCrossroad(double latitudeDegrees, double longitudeDegrees)
        {
            ; // do nothing
        }

        void IRadarService.WriteOffTrack(double latitudeDegrees, double longitudeDegrees, string name = null)
        {
            ; // do nothing
        }

        internal void SetIndex(int index)
        {
            this.index = index;
        }
    }
}
