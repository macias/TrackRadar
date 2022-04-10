using Geo;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using TrackRadar.Implementation;
using static TrackRadar.Implementation.GpxLoader;

namespace TrackRadar.Tests
{
    public sealed class RideParams
    {
#if DEBUG
        public MetaLogger DEBUG_Logger { get; set; } = MetaLogger.None;
#endif
        public Preferences Prefs { get; }
        public TimeSpan? PlayDuration { get; set; }
        public string PlanFilename { get; set; }
        public string TraceFilename { get; set; }
        public Speed? Speed { get; set; }
        public bool Reverse { get; set; }
        public IPlanData PlanData { get; set; }
        public Length InitMinAccuracy { get; set; }
        public bool ReportOnlyDuration { get; set; }
        public bool UseTraceTimestamps { get; set; }
        public IEnumerable<GpsPoint?> Trace { get; set; }
        public bool ExtendPlanEnds { get; set; }
        public bool ReadAltitude { get; internal set; }

        public RideParams(Preferences prefs)
        {
            this.Prefs = prefs;
            InitMinAccuracy = RadarCore.InitialMinAccuracy;
            this.ReportOnlyDuration = true;
        }

        public RideParams SetTrace(IEnumerable<GpsPoint> trackPoints)
        {
            Trace = trackPoints.Select(pt => new GpsPoint?(pt));
            return this; 
        }
    }
}
