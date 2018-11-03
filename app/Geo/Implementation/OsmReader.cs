
using Gpx;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace Geo.Implementation
{
    // https://wiki.openstreetmap.org/wiki/OSM_XML

    internal sealed class OsmReader : IReader
    {
        private readonly XmlReader reader;
        private readonly Dictionary<long, GeoPoint> nodes;
        private readonly Dictionary<string, WayKind> wayKinds;

        public OsmReader(Stream stream)
        {
            this.reader = XmlReader.Create(stream, new XmlReaderSettings() { Async = true });
            this.nodes = new Dictionary<long, GeoPoint>();
            this.wayKinds = WayKind.Values.Where(it => it != WayKind.Other).ToDictionary(it => it.Name, it => it);
        }

        public void Dispose()
        {
            reader.Dispose();
        }

        public async Task<Way> ReadWayAsync()
        {
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name=="node")
                    {
                        string id_value = reader.GetAttribute("id");
                        string lat_value = reader.GetAttribute("lat");
                        string lon_value = reader.GetAttribute("lon");

                        long id = long.Parse(id_value, CultureInfo.InvariantCulture);
                        double lon = double.Parse(lon_value, CultureInfo.InvariantCulture);
                        double lat = double.Parse(lat_value, CultureInfo.InvariantCulture);

                        this.nodes.Add(id, new GeoPoint() { Latitude = Angle.FromDegrees(lat), Longitude = Angle.FromDegrees(lon) });
                    }
                    else if (reader.Name == "way")
                    {
                        Way way = await readWayXAsync().ConfigureAwait(false);
                        if (way != null)
                            return way;
                    }
                }
            }

            return null;
        }

        public async Task<Way> readWayXAsync()
        {
            WayKind kind = null;
            var points = new List<IGeoPoint>();

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "tag")
                    {
                        string key_value = reader.GetAttribute("k");
                        if (key_value == "highway")
                        {
                            string val_value = reader.GetAttribute("v");
                            if (this.wayKinds.TryGetValue(val_value, out WayKind k))
                                kind = k;
                            else
                                kind = WayKind.Other;
                        }
                    }
                    else if (reader.Name=="nd")
                    {
                        string ref_value = reader.GetAttribute("ref");
                        long node_id = long.Parse(ref_value, CultureInfo.InvariantCulture);
                        points.Add(this.nodes[node_id]);
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.Name == "way")
                    {
                        break;
                    }
                }
            }

            if (kind == null || points.Count<2)
                return null;
            else
                return new Way(kind, points);
        }
    }
}