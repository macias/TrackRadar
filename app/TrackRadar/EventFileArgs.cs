using MathUnit;
using System;

namespace TrackRadar
{
    public sealed class EventFileArgs 
    {
        public int TagRequest { get; }
        public string Path { get; }
        public Length OffTrackDistance { get; }

        public EventFileArgs(int tagRequest, string path,Length offTrackDistance)
        {
            TagRequest = tagRequest;
            Path = path;
            OffTrackDistance = offTrackDistance;
        }
    }
}