using Geo;
using Gpx;
using System.Collections.Generic;
using System.Linq;
using TrackRadar.Implementation;

namespace TrackRadar.Tests.Implementation
{
    public static class Toolbox
    {
        internal static void PopulateAlarms(this AlarmMaster alarmMaster)
        {
            alarmMaster.Reset(new TestAlarmVibrator(),
                offTrackPlayer: new TestAlarmPlayer(AlarmSound.OffTrack),
                gpsLostPlayer: new TestAlarmPlayer(AlarmSound.GpsLost),
                gpsOnPlayer: new TestAlarmPlayer(AlarmSound.BackOnTrack),
                disengage: new TestAlarmPlayer(AlarmSound.Disengage),
                crossroadsPlayer: new TestAlarmPlayer(AlarmSound.Crossroad),
                goAhead: new TestAlarmPlayer(AlarmSound.GoAhead),
                leftEasy: new TestAlarmPlayer(AlarmSound.LeftEasy),
                leftCross: new TestAlarmPlayer(AlarmSound.LeftCross),
                leftSharp: new TestAlarmPlayer(AlarmSound.LeftSharp),
                rightEasy: new TestAlarmPlayer(AlarmSound.RightEasy),
                rightCross: new TestAlarmPlayer(AlarmSound.RightCross),
                rightSharp: new TestAlarmPlayer(AlarmSound.RightSharp));
        }
        internal static IEnumerable<GpxTrackPoint> ReadTrackPoints(string ride_filename)
        {
            var track_points = new List<GpxTrackPoint>();
            using (Gpx.GpxIOFactory.CreateReader(ride_filename, out IGpxReader reader))
            {
                while (reader.Read(out GpxObjectType type))
                {
                    if (type == GpxObjectType.Track)
                    {
                        track_points.AddRange(reader.Track.Segments.SelectMany(it => it.TrackPoints));
                    }
                }

            }

            return track_points;
        }
        internal static IEnumerable<GpxWayPoint> ReadWayPoints(string ride_filename)
        {
            var way_points = new List<GpxWayPoint>();
            using (Gpx.GpxIOFactory.CreateReader(ride_filename, out IGpxReader reader))
            {
                while (reader.Read(out GpxObjectType type))
                {
                    if (type == GpxObjectType.WayPoint)
                    {
                        way_points.Add(reader.WayPoint);
                    }
                }

            }

            return way_points;
        }
    }
}