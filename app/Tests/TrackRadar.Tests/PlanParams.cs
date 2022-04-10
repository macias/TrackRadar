using Geo;
using MathUnit;
using System;
using System.Collections.Generic;
using TrackRadar.Implementation;
using static TrackRadar.Implementation.GpxLoader;

namespace TrackRadar.Tests
{
    public sealed class PlanParams
    {
        public List<IReadOnlyList<GeoPoint>> Tracks { get; set; }
        public IEnumerable<GeoPoint> Waypoints { get; set; }
        public IEnumerable<GeoPoint> Endpoints { get; set; }
        public Length OffTrackDistance { get; set; }
        public Length SegmentLengthLimit { get; set; }
        public Action<Stage, long, long> OnProgress { get; set; }

        public PlanParams(IPreferences prefs)
        {
            this.Tracks = new List<IReadOnlyList<GeoPoint>>();
            this.Waypoints = new List<GeoPoint>();
            this.Endpoints = new List<GeoPoint>();
            SegmentLengthLimit = GeoMapFactory.SegmentLengthLimit;
            this.OffTrackDistance = prefs.OffTrackAlarmDistance;
        }

        public PlanParams AddTrack(params GeoPoint[] points)
        {
            this.Tracks.Add(points);
            return this;
        }
    }
}
