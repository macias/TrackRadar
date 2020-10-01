using System;
using System.Collections.Generic;
using Geo;
using MathUnit;

namespace TrackRadar
{
    public partial class GpxLoader
    {   
        internal sealed class TrackNode : ISegment
        {
            public enum Direction
            {
                Forward,
                Backward,
            }
            public GeoPoint Point { get; }
            private readonly TrackNode previous;
            public TrackNode Next { get; private set; }
            public IEnumerable<TrackNode> Siblings
            {
                get
                {
                    if (previous != null)
                        yield return previous;
                    if (Next != null)
                        yield return Next;
                }
            }
            public IEnumerable<(TrackNode,Length)> MeasuredSiblings
            {
                get
                {
                    if (previous != null)
                        yield return (previous,previous.GetLength());
                    if (Next != null)
                        yield return (Next,this.GetLength());
                }
            }

            public bool IsFirst => this.previous == null;
            public bool IsLast => this.Next == null;

            // for nodest closest to the turning point unique tag run is given
            // then each tag run value is used for segments from one turning point to another
            // each node next to turning point starts new tag run
            private int? sectionId;
            public int SectionId => this.sectionId.Value;

            int ISegment.SectionId => this.SectionId;
            GeoPoint ISegment.A => this.Point;
            GeoPoint ISegment.B => this.Next.Point;

            public TrackNode(GeoPoint point, TrackNode previous, TrackNode next)
            {
                this.Point = point;
                this.previous = previous;
                this.Next = next;
            }

            public void SetSectionId(int id)
            {
                this.sectionId = id;
            }
            internal TrackNode Add(GeoPoint point)
            {
                var result = new TrackNode(point, this, this.Next);
                this.Next = result;
                return result;
            }

            Ordering ISegment.CompareImportance(ISegment other)
            {
                if (other.GetType() == this.GetType())
                    return Ordering.Equal;
                else
                    throw new ArgumentException();
            }

            internal TrackNode Go(Direction dir)
            {
                return dir == Direction.Forward ? this.Next : this.previous;
            }

            public Length GetLength()
            {
                return GeoCalculator.GetDistance(this.Point, this.Next.Point);
            }

            Length ISegment.GetLength()
            {
                return this.GetLength();
            }
        }
    }
}
