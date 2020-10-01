using Geo;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using TrackRadar.Implementation;

namespace TrackRadar
{
    public partial class GpxLoader
    {
        internal sealed class Crossroad
        {
            public static Crossroad AverageCrossroads(Crossroad a, Crossroad b, CrossroadKind kind, TracksIndex sourceIndex)
            {
                var result = new Crossroad(GeoCalculator.GetMidPoint(a.Point, b.Point), kind);
                result.SourceIndex = sourceIndex;

                foreach (TrackNode neighbour in new[] { a, b }
                                            .SelectMany(it => it.Neighbours)
                                            .Select(it => it.node)
                                            .Distinct()
                                            .ToArray())
                {
                    // because both points are computed on the fly crossroads, we have to compute fresh distance
                    // because we cannot just add distances (manhattan-like), because we are are NOT on the track
                    result.Connected(neighbour, null);
                }

                foreach (TrackNode node in new[] { a, b }
                                        .SelectMany(it => it.projections)
                                        .Select(it => it.Key)
                                        .Distinct()
                                        .ToArray())
                {
                    // since we are changing crossroad position the projection on given segment will be somewhere else
                    result.Projected(node, null, null);
                }

                a.Clear();
                b.Clear();

                return result;
            }

#if DEBUG
            private static int debugId;
            public int DebugId { get; } = debugId++;
#endif

            // private readonly TrackNode node;
            private readonly Dictionary<TrackNode, Length?> neighbours;
            private readonly Dictionary<TrackNode, (GeoPoint? projection, Length? nodeProjDistance)> projections;

            public IEnumerable<(TrackNode node, GeoPoint projection, Length nodeProjDistance)> Projections => this.projections
                .Select(it => (it.Key, it.Value.projection.Value, it.Value.nodeProjDistance.Value));

            public IEnumerable<(TrackNode node, Length? distance)> Neighbours => this.neighbours.Select(it => (it.Key, it.Value));

            public GeoPoint Point { get; }
            internal CrossroadKind Kind { get; }
            public TracksIndex SourceIndex { get; private set; }

            public Crossroad(GeoPoint point, CrossroadKind kind)
            {
                this.Point = point;
                this.Kind = kind;
                this.neighbours = new Dictionary<TrackNode, Length?>();
                this.projections = new Dictionary<TrackNode, (GeoPoint?, Length?)>();
            }

            public override string ToString()
            {
                return $"{SourceIndex} {Kind} {Point}";
            }

            internal Crossroad Connected(TrackNode other, Length? distance)
            {
                this.neighbours.Add(other, distance);
                return this;
            }

            internal void Clear()
            {
                this.neighbours.Clear();
                this.RemoveProjections();
            }

            internal void SetSourceIndex(int i, int k)
            {
                this.SourceIndex = new TracksIndex(i, k);
            }

            internal Crossroad Projected(TrackNode node, GeoPoint projection)
            {
                this.projections.Add(node, (projection, null));
                return this;
            }

            internal Crossroad Projected(TrackNode node, GeoPoint? projection, Length? distance)
            {
                this.projections.Add(node, (projection, distance));
                return this;
            }

            internal void UpdateNeighbours()
            {
                foreach (KeyValuePair<TrackNode,Length?> entry in this.neighbours.ToArray())
                {
                    if (entry.Value.HasValue)
                        continue;

                    this.neighbours[entry.Key] = GeoCalculator.GetDistance(entry.Key.Point, this.Point);
                }
            }

            internal void UpdateProjections()
            {
                foreach (KeyValuePair<TrackNode, (GeoPoint? projection, Length? nodeProjDistance)> __proj in projections.ToArray())
                {
                    var proj = __proj;
                    // we don't know what the projections point is
                    if (!proj.Value.projection.HasValue)
                    {
                        GeoCalculator.GetDistanceToArcSegment(this.Point, proj.Key.Point, proj.Key.Next.Point, out GeoPoint pt);
                        proj = new KeyValuePair<TrackNode, (GeoPoint? projection, Length? nodeProjDistance)>(proj.Key, (pt, null));
                    }

                    // todo: check how we add projections and maybe add this check when adding avoiding computation
                    // todo: add some precision slack here maybe
                    if (proj.Value.projection == proj.Key.Point)
                    {
                        if (!this.neighbours.ContainsKey(proj.Key))
                            this.Connected(proj.Key, null);
                        this.projections.Remove(proj.Key);
                        continue;
                    }

                    if (proj.Value.projection == proj.Key.Next.Point)
                    {
                        if (!this.neighbours.ContainsKey(proj.Key.Next))
                            this.Connected(proj.Key.Next, null);
                        this.projections.Remove(proj.Key);
                        continue;
                    }

                    // we know the projection point, but we don't know how far from head node it is
                    if (!proj.Value.nodeProjDistance.HasValue)
                    {
                        Length dist = GeoCalculator.GetDistance(proj.Value.projection.Value, proj.Key.Point);
                        projections[proj.Key] = (proj.Value.projection.Value, dist);
                    }
                }
            }

            internal void RemoveProjections()
            {
                this.projections.Clear();
            }
        }
    }
}
