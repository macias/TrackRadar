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
                offTrackPlayer: new TestAlarmPlayer(Alarm.OffTrack),
                gpsLostPlayer: new TestAlarmPlayer(Alarm.GpsLost),
                gpsOnPlayer: new TestAlarmPlayer(Alarm.PositiveAcknowledgement),
                disengage: new TestAlarmPlayer(Alarm.Disengage),
                crossroadsPlayer: new TestAlarmPlayer(Alarm.Crossroad),
                goAhead: new TestAlarmPlayer(Alarm.GoAhead),
                leftEasy: new TestAlarmPlayer(Alarm.LeftEasy),
                leftCross: new TestAlarmPlayer(Alarm.LeftCross),
                leftSharp: new TestAlarmPlayer(Alarm.LeftSharp),
                rightEasy: new TestAlarmPlayer(Alarm.RightEasy),
                rightCross: new TestAlarmPlayer(Alarm.RightCross),
                rightSharp: new TestAlarmPlayer(Alarm.RightSharp));
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