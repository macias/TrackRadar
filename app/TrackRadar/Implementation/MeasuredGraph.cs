using Geo.Comparers;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Implementation
{
  /*  internal sealed class MeasuredGraph 
    {
        // the list is sorted from closest point to the farest
        private readonly IReadOnlyDictionary<Geo.GeoPoint, IEnumerable<(GeoPoint point,Length distance)>> data;

        public MeasuredGraph(IEnumerable<INodePoint> points)
        {
            var dict = new Dictionary<Geo.GeoPoint, IEnumerable<( GeoPoint point,Length distance)>>();

            foreach (INodePoint pt in points)
            {                
                if (dict.ContainsKey(pt.Point))
                    continue;

                dict.Add(pt.Point, pt.Neighbours.OrderBy(it => it.distance).Select(it => (it.point.Point,it.distance)).ToList());
            }

            this.data = dict;
        }

    }
    */
}