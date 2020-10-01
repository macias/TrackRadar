using Geo.Comparers;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Geo.Implementation
{
/*    internal sealed class Graph : IGraph
    {
        private readonly Dictionary<Geo.GeoPoint, List<Geo.GeoPoint>> data;

        public Graph(IEnumerable<IEnumerable<Geo.GeoPoint>> tracks)
        {
            this.data = new Dictionary<Geo.GeoPoint, List<Geo.GeoPoint>>();

            foreach (IEnumerable<Geo.GeoPoint> track in tracks)
            {
                Geo.GeoPoint prev = track.First();

                foreach (GeoPoint p in track.Skip(1))
                {
                    add(prev, p);
                    add(p, prev);

                    prev = p;
                }
            }
        }

        private void add(GeoPoint source, GeoPoint target)
        {
            if (!this.data.TryGetValue(source, out List<GeoPoint> list))
            {
                list = new List<GeoPoint>(capacity: 2);
                this.data.Add(source, list);
            }

            list.Add(target);
        }

        public IEnumerable<GeoPoint> GetAdjacent(in GeoPoint node)
        {
            return this.data[node];
        }

        
    }*/
}