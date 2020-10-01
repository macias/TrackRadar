using Geo;
using Gpx;
using MathUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TrackRadar.Implementation;

namespace TrackRadar.Tests.Implementation
{
    public static class Toolbox
    {
        public static void Ride(Preferences prefs, IPlanData planData, List<GeoPoint> trackPoints,
            out IReadOnlyDictionary<Alarm, int> alarmCounters,
            out IReadOnlyList<(Alarm alarm, int index)> alarms,
            out IReadOnlyList<(string message, int index)> messages)
        {
            var clock = new SecondStamper();
            using (var raw_alarm_master = new AlarmMaster(clock))
            {
                raw_alarm_master.PopulateAlarms();

                var counting_alarm_master = new CountingAlarmMaster(raw_alarm_master);

                var service = new TrackRadar.Tests.Implementation.RadarService(prefs, clock);
                AlarmSequencer sequencer = new AlarmSequencer(service, counting_alarm_master);
                var core = new TrackRadar.Implementation.RadarCore(service, sequencer, clock, planData, Length.Zero, Length.Zero, TimeSpan.Zero, Speed.Zero);

                int point_index = 0;
                foreach (var pt in trackPoints)
                {
                    using (sequencer.OpenAlarmContext(gpsAcquired: false, hasGpsSignal: true))
                    {
                        counting_alarm_master.SetPointIndex(point_index);
                        core.UpdateLocation(pt, null, accuracy: null);
                        clock.Advance();
                        ++point_index;
                    }
                }

                alarmCounters = counting_alarm_master.AlarmCounters;
                alarms = counting_alarm_master.Alarms;
                messages = counting_alarm_master.Messages;
            }
        }

        internal static Preferences LowThresholdSpeedPreferences()
        {
            return new Preferences() { RestSpeedThreshold = Speed.Zero, RidingSpeedThreshold = Speed.FromMetersPerSecond(1) };
        }


        internal static void SaveSegments(string filename, IEnumerable<ISegment> segments)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                int idx = 0;
                foreach (ISegment seg in segments)
                {
                    writer.WriteTrack($"{idx}:{seg.SectionId}", seg.Points().ToArray());
                    ++idx;
                }
            }
        }
        internal static void SaveGpx(string filename, IEnumerable<GeoPoint> points)
        {
            using (var writer = new GpxDirtyWriter(filename))
            {
                int idx = 0;
                foreach (GeoPoint pt in points)
                {
                    writer.WritePoint(pt, $"{idx}");
                    ++idx;
                }
            }
        }

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

        public static void PopulateTrackDensely(List<GeoPoint> trackPoints)
        {
            for (int i = 0; i < trackPoints.Count - 1; ++i)
            {
                while (GeoCalculator.GetDistance(trackPoints[i], trackPoints[i + 1]).Meters > 3)
                    trackPoints.Insert(i + 1, GeoCalculator.GetMidPoint(trackPoints[i], trackPoints[i + 1]));
            }
        }

        internal static IEnumerable<GpxTrackPoint> ReadTrackPoints(string ride_filename)
        {
            var track_points = new List<GpxTrackPoint>();
            using (Gpx.GpxIOFactory.CreateReader(ride_filename, out IGpxReader reader, out _))
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

        public static IPlanData CreateTrackData(IEnumerable<GeoPoint> track, IEnumerable<GeoPoint> waypoints, Length offTrackDistance)
        {
            return GpxLoader.ProcessTrackData(new[] { track }, waypoints, offTrackDistance: offTrackDistance,
                segmentLengthLimit: GeoMapFactory.SegmentLengthLimit, null, CancellationToken.None);
        }

        public static IGeoMap CreateTrackMap(IEnumerable<GeoPoint> track)
        {
            var prefs = new Preferences();
            IPlanData track_data = CreateTrackData(track, new GeoPoint[] { }, prefs.OffTrackAlarmDistance);
            return RadarCore.CreateTrackMap(track_data.Segments);
        }

        internal static IEnumerable<GpxWayPoint> ReadWaypoints(string ride_filename)
        {
            var way_points = new List<GpxWayPoint>();
            using (Gpx.GpxIOFactory.CreateReader(ride_filename, out IGpxReader reader, out _))
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