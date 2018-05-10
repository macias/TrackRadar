using System;
using System.Threading;
using Android.Locations;
using Android.OS;
using Gpx;

namespace TrackRadar.Mocks
{
    internal sealed class LocationManagerMock
    {
        private readonly IGeoPoint point;
        private readonly Timer timer;
        private readonly ThreadSafe<int> interval;
        private readonly ThreadSafe<ILocationListener> listener;
        private readonly ThreadSafe<string> provider;

        public LocationManagerMock(IGeoPoint point)
        {
            this.point = point;
            this.timer = new Timer(_ => ping());
            this.interval = new ThreadSafe<int>(Timeout.Infinite);
            this.listener = new ThreadSafe<ILocationListener>();
            this.provider = new ThreadSafe<string>();
        }
        internal void RequestLocationUpdates(string provider, long minTime, float minDistance, ILocationListener listener, Looper looper)
        {
            this.listener.Value = listener;
            this.provider.Value = provider;
            this.timer.Change(this.interval.Value = 1000, Timeout.Infinite);
        }

        internal void RemoveUpdates(ILocationListener listener)
        {
            this.timer.Change(this.interval.Value = Timeout.Infinite, Timeout.Infinite);
            this.listener.Value = null;
        }

        private void ping()
        {
            Location loc = createLocation();
            this.listener.Value.OnLocationChanged(loc);
            this.timer.Change(this.interval.Value, Timeout.Infinite);
        }

        private Location createLocation()
        {
            return new Location(provider.Value)
            {
                Latitude = this.point.Latitude + (DateTime.Now.Ticks % 83) * 1.0 / 1000,
                Longitude = this.point.Longitude + (DateTime.Now.Ticks % 83) * 1.0 / 1000
            };
        }

        internal Location GetLastKnownLocation(string gpsProvider)
        {
            return createLocation();
        }
    }
}