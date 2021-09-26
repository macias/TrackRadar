using Geo;
using MathUnit;
using System;
using TrackRadar.Implementation;

namespace TrackRadar.Tests.Implementation
{
    internal sealed class RideParams
    {
#if DEBUG
        public MetaLogger DEBUG_Logger { get; set; } = MetaLogger.None;
#endif
        public Preferences Prefs { get; }
        public TimeSpan PlayDuration { get; set; }
        public string PlanFilename { get; set; }
        public string TraceFilename { get; set; }
        public Speed? Speed { get; set; }
        public bool Reverse { get; set; }
        public IPlanData PlanData { get; set; }
        public Length InitMinAccuracy { get; set; } = RadarCore.InitialMinAccuracy;

        public RideParams(Preferences prefs)
        {
            this.Prefs = prefs;
        }
    }
}
