using MathUnit;
using System;

namespace Geo
{
    public readonly struct GeoPoint : IEquatable<GeoPoint>
    {
        public static GeoPoint FromDegrees(double latitude, double longitude)
        {
            return new GeoPoint(latitude: Angle.FromDegrees(latitude), longitude: Angle.FromDegrees(longitude));
        }

        public Angle Latitude { get; } // Y, -90 to +90
        public Angle Longitude { get; } // X, -180 to +180

        public GeoPoint(Angle latitude, Angle longitude)
        {
            this.Latitude = latitude;
            this.Longitude = longitude;
        }

        public override string ToString()
        {
            return Latitude.ToString() + "," + Longitude.ToString();
        }

        public string ToString(string format)
        {
            return Latitude.ToString(format) + "," + Longitude.ToString(format);
        }

        public override bool Equals(object obj)
        {
            if (obj is GeoPoint gp)
                return Equals(gp);
            else
                return false;
        }

        public bool Equals(GeoPoint obj)
        {
            return this.Latitude==obj.Latitude && this.Longitude==obj.Longitude;
        }

        public override int GetHashCode()
        {
            return Latitude.GetHashCode() ^ Longitude.GetHashCode();
        }

        public static bool operator ==(GeoPoint a, GeoPoint b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(GeoPoint a, GeoPoint b)
        {
            return !(a == b);
        }

    }
}