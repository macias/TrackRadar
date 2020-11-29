using System;
using System.Collections.Generic;
using System.Linq;
using MathUnit;
using TrackRadar.Implementation;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class CountingAlarmMaster : IAlarmMaster
    {
        private readonly IAlarmMaster master;
        private readonly Dictionary<Alarm, int> alarmCount;
        private readonly List<(Alarm alarm, int pointIndex)> alarms;
        private readonly List<(string message, int pointIndex)> messages;
        private int pointIndex;

        public IReadOnlyDictionary<Alarm, int> AlarmCounters => this.alarmCount;
        public IReadOnlyList<(Alarm alarm, int index)> Alarms => alarms;
        public IReadOnlyList<(string message, int index)> Messages => messages;

        public TimeSpan MaxTurnDuration => master.MaxTurnDuration;

        public CountingAlarmMaster(IAlarmMaster master)
        {
            this.master = master;
            this.alarmCount = LinqExtension.GetEnums<Alarm>().ToDictionary(alarm => alarm, _ => 0);
            this.alarms = new List<(Alarm alarm, int pointIndex)>();
            this.messages = new List<(string message, int pointIndex)>();
        }
        public bool TryAlarm(Alarm alarm, out string reason)
        {
            if (!this.master.TryAlarm(alarm, out reason))
                return false;

            this.alarms.Add((alarm, pointIndex));
            ++alarmCount[alarm];
            // Console.WriteLine($"ALARM {alarm}");
            return true;
        }
        public void PostMessage(string message)
        {
            this.messages.Add((message, pointIndex));
        }
        internal void SetPointIndex(int index)
        {
            this.pointIndex = index;
        }

        public bool TryGetLatestTurnAheadAlarmAt(out long timeStamp)
        {
            return master.TryGetLatestTurnAheadAlarmAt(out timeStamp);
        }
    }
}
