using System;
using System.Collections.Generic;
using Geo;
using MathUnit;

namespace TrackRadar.Implementation
{
    public partial class GpxLoader
    {
        internal sealed class TrackNode : ISegment
        {
#if DEBUG
            private static int __DEBUG_idCounter = 0;
            public int __DEBUG_id { get; } = ++__DEBUG_idCounter;
#endif
            public enum Direction
            {
                Forward,
                Backward,
            }

            public IEnumerable<TrackNode> Nodes
            {
                get
                {
                    TrackNode current = this;
                    while (current != null)
                    {
                        yield return current;
                        current = current.Next;
                    }
                }
            }

            public GeoPoint Point { get; }
            private TrackNode previous;
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
            public IEnumerable<(TrackNode, Length)> MeasuredSiblings
            {
                get
                {
                    if (previous != null)
                        yield return (previous, previous.GetLength());
                    if (Next != null)
                        yield return (Next, this.GetLength());
                }
            }

            public bool IsFirst => this.previous == null;
            public bool IsLast => this.Next == null;

            // for each turn node with 2 arms, each arm should have unique id
            // extended tracks should have the same ids so the arm does not end abruptly
            // * turn point, -/\ track
            //
            //          *---
            //         /
            //   \    / b
            //  c \  / a
            //      *
            // here we see 2 turn nodes with 8 tracks, but only 3 ids should be used
            // not more, because if for example a!=b ids, then while moving towards lower turn from b
            // we couldn't tell if "a" is the other arm or "c", thus we couldn't compute turn angle
            // when a=b ids (as they should) we could see c is the other arm of "b" (because c!=b)
            private int? sectionId;
            public int SectionId => this.sectionId.Value;
            public bool IsSectionSet => this.sectionId.HasValue;

            int ISegment.SectionId => this.SectionId;
            GeoPoint ISegment.A => this.Point;
            GeoPoint ISegment.B => this.Next.Point;

            public TrackNode(GeoPoint point, TrackNode previous, TrackNode next)
            {
                this.Point = point;
                this.previous = previous;
                this.Next = next;

#if DEBUG
                if (this.__DEBUG_id==5)
                {
                    ;
                }
#endif
            }

            public void SetSectionId(int id)
            {
#if DEBUG
                if (this.sectionId.HasValue)
                    throw new ArgumentException($"This node has section id already set.");
                if (this.IsLast)
                {
                    if (this.previous.sectionId.HasValue && this.previous.sectionId != id)
                        throw new ArgumentException($"Previous node has other section id");

                }
                else if (this.Next.IsLast && this.Next.sectionId.HasValue && this.Next.sectionId!=id)
                {
                    throw new ArgumentException($"Next node has other section id");
                }
#endif
                this.sectionId = id;
            }
            internal TrackNode Add(GeoPoint point)
            {
                var result = new TrackNode(point, this, this.Next);
                if (this.Next != null)
                    this.Next.previous = result;
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

#if DEBUG
            public override string ToString()
            {
                return $"{this.__DEBUG_id} {this.sectionId}";
            }
#endif
        }
    }
}
