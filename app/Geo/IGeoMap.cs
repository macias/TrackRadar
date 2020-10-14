using MathUnit;
using System.Collections.Generic;

namespace Geo
{
    public interface IGeoMap
    {
        IEnumerable<Geo.ISegment> Segments { get; }

        bool FindClosest(in GeoPoint point, Length? limit, out Geo.ISegment nearby, out Length? distance,out ArcSegmentIntersection crosspointInfo);
        bool FindCloseEnough(in GeoPoint point, Length limit, out ISegment nearby, out Length? distance, out ArcSegmentIntersection crosspointInfo);
        bool IsWithinLimit(in GeoPoint point, Length limit, out Length? distance);
        IEnumerable<IMeasuredPinnedSegment> FindAll( GeoPoint point, Length limit);
        IEnumerable<Geo.ISegment> GetFromRegion(Angle westmost, Angle eastmost, Angle northmost, Angle southmost);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node">has to be existing node in the map</param>
        /// <returns></returns>
        //IEnumerable<GeoPoint> GetAdjacent(in GeoPoint node);

       // GeoPoint GetReference(Angle latitude,Angle longitude);
        IEnumerable<Geo.ISegment> GetNearby(in GeoPoint point, Length limit);
    }
}