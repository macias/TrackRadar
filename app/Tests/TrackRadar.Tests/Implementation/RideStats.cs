using Geo;
using MathUnit;
using System.Collections.Generic;
using TrackRadar.Implementation;

namespace TrackRadar.Tests.Implementation
{
    public readonly struct RideStats
    {
        public IPlanData Plan { get; }
        public IReadOnlyList<GeoPoint> TrackPoints { get; }
        public IReadOnlyList<Speed> Speeds { get; }
        public double MaxUpdate { get; }
        public double AvgUpdate { get; }
        public int TrackCount { get; }
        public IReadOnlyDictionary<Alarm, int> AlarmCounters { get; }
        public IReadOnlyList<(Alarm alarm, int index)> Alarms { get; }
        public IReadOnlyList<(string message, int index)> Messages { get; }

        public RideStats(IPlanData plan, IReadOnlyList<GeoPoint> trackPoints, IReadOnlyList<Speed> speeds, double maxUpdate, double avgUpdate, int trackCount,
            IReadOnlyDictionary<Alarm, int> alarmCounters,
            IReadOnlyList<(Alarm alarm, int index)> alarms,
            IReadOnlyList<(string message, int index)> messages)
        {
            Plan = plan;
            TrackPoints = trackPoints;
            Speeds = speeds;
            this.MaxUpdate = maxUpdate;
            this.AvgUpdate = avgUpdate;
            TrackCount = trackCount;
            AlarmCounters = alarmCounters;
            Alarms = alarms;
            Messages = messages;
        }

        /*public void Deconstruct(out double maxUpdate, out double avgUpdate)
        {
            maxUpdate = this.MaxUpdate;
            avgUpdate = this.AvgUpdate;
        }
        */
    }
}