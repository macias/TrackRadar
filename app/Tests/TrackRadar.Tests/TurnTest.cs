using Geo;
using Gpx;
using MathUnit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TrackRadar.Implementation;
using TrackRadar.Tests.Implementation;

namespace TrackRadar.Tests
{
    [TestClass]
    public class TurnTest
    {
        private const double precision = 0.00000001;

        [TestMethod]
        public void TurnKindsTest()
        {
            {
                GeoPoint a = GeoPoint.FromDegrees(37.41428, -94.70178);
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.84314, -94.70957);

                Assert.AreEqual(TurnKind.GoAhead, TurnLookout.getTurnKind(a, b, b, c));
                Assert.AreEqual(TurnKind.GoAhead, TurnLookout.getTurnKind(c, b, b, a));
            }

            {
                GeoPoint a = GeoPoint.FromDegrees(37.41428, -94.70178);
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.49093, -94.4816);

                Assert.AreEqual(TurnKind.RightSharp, TurnLookout.getTurnKind(a, b, b, c));
                Assert.AreEqual(TurnKind.LeftSharp, TurnLookout.getTurnKind(c, b, b, a));
            }

            {
                GeoPoint a = GeoPoint.FromDegrees(37.41428, -94.70178);
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.70202, -94.47611);

                Assert.AreEqual(TurnKind.RightCross, TurnLookout.getTurnKind(a, b, b, c));
                Assert.AreEqual(TurnKind.LeftCross, TurnLookout.getTurnKind(c, b, b, a));
            }

            {
                GeoPoint a = GeoPoint.FromDegrees(37.41428, -94.70178);
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.84639, -94.58322);

                Assert.AreEqual(TurnKind.RightEasy, TurnLookout.getTurnKind(a, b, b, c));
                Assert.AreEqual(TurnKind.LeftEasy, TurnLookout.getTurnKind(c, b, b, a));
            }
        }

        [TestMethod]
        public void FindingTurnsTest()
        {
            Length turn_ahead_distance = Length.FromMeters(5);

            {
                GeoPoint a = GeoPoint.FromDegrees(37.41428, -94.70178);
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.84314, -94.70957);

                Assert.IsTrue(TurnCalculator.TryComputeTurn(b, MapHelper.CreateDefaultGrid(new[] { new Segment(a, b), new Segment(b, c) }),
                    turn_ahead_distance, out Turn turn));
                Assert.AreEqual(359.475341177312, turn.BearingA.Degrees, precision);
                Assert.AreEqual(178.802134619193, turn.BearingB.Degrees, precision);
            }

            {
                GeoPoint a = GeoPoint.FromDegrees(37.41428, -94.70178);
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.49093, -94.4816);

                Assert.IsTrue(TurnCalculator.TryComputeTurn(b, MapHelper.CreateDefaultGrid(new[] { new Segment(a, b), new Segment(b, c) }),
                    turn_ahead_distance, out Turn turn));
                Assert.AreEqual(359.475341177312, turn.BearingA.Degrees, precision);
                Assert.AreEqual(312.425971052848, turn.BearingB.Degrees, precision);
            }

            {
                GeoPoint a = GeoPoint.FromDegrees(37.41428, -94.70178);
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.70202, -94.47611);

                Assert.IsTrue(TurnCalculator.TryComputeTurn(b, MapHelper.CreateDefaultGrid(new[] { new Segment(a, b), new Segment(b, c) }),
                    turn_ahead_distance, out Turn turn));
                Assert.AreEqual(359.475341177312, turn.BearingA.Degrees, precision);
                Assert.AreEqual(254.698737269103, turn.BearingB.Degrees, precision);
            }

            {
                GeoPoint a = GeoPoint.FromDegrees(37.41428, -94.70178);
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.84639, -94.58322);

                Assert.IsTrue(TurnCalculator.TryComputeTurn(b, MapHelper.CreateDefaultGrid(new[] { new Segment(a, b), new Segment(b, c) }),
                    turn_ahead_distance, out Turn turn));
                Assert.AreEqual(359.475341177312, turn.BearingA.Degrees, precision);
                Assert.AreEqual(206.32012655082, turn.BearingB.Degrees, precision);
            }
        }

        [TestMethod]
        public void RideWithTurnsTest()
        {
            const string result_filename = @"Data/turning-excercise.result.gpx";
            IReadOnlyList<GpxWayPoint> turn_points = Toolbox.ReadWayPoints(result_filename).ToList();

            List<GeoPoint> track_points = new List<GeoPoint>();
            RideWithTurns(track_points, out Implementation.RadarService service);

            Assert.AreEqual(turn_points.Count, service.Alarms.Count());
            for (int i = 0; i < service.Alarms.Count(); ++i)
            {
                Assert.AreEqual(track_points[service.Alarms[i].index].Latitude.Degrees, turn_points[i].Latitude.Degrees, precision);
                Assert.AreEqual(track_points[service.Alarms[i].index].Longitude.Degrees, turn_points[i].Longitude.Degrees, precision);
                Assert.AreEqual(service.Alarms[i].alarm, Enum.Parse<Alarm>(turn_points[i].Name));
            }
        }

        internal double RideWithTurns(List<GeoPoint> trackPoints, out Implementation.RadarService service)
        {
            const string plan_filename = @"Data/turning-excercise.gpx";

            var prefs = new Preferences();

            trackPoints.AddRange(Toolbox.ReadTrackPoints(plan_filename).Select(it => GpxHelper.FromGpx(it)));

            var gpx_data = new GpxData(Enumerable.Range(0, trackPoints.Count - 1)
                .Select(i => new Segment(trackPoints[i], trackPoints[i + 1])),
                // set each in-track point as turning one
                trackPoints.Skip(1).SkipLast(1));

            // populate densely the "ride" points (better than having 1MB file on disk)
            for (int i = 0; i < trackPoints.Count - 1; ++i)
            {
                while (GeoCalculator.GetDistance(trackPoints[i], trackPoints[i + 1]).Meters > 3)
                    trackPoints.Insert(i + 1, GeoCalculator.GetMidPoint(trackPoints[i], trackPoints[i + 1]));
            }

            var clock = new SecondStamper();
            using (var alarm_master = new AlarmMaster(clock))
            {
                alarm_master.PopulateAlarms();
                service = new Implementation.RadarService(prefs, clock, alarm_master);
                var lookout = new TurnLookout(service, clock, gpx_data, MapHelper.CreateDefaultGrid(gpx_data.Segments));
                Speed ride_speed = Speed.FromKilometersPerHour(10);

                long start = Stopwatch.GetTimestamp();

                int point_index = 0;
                foreach (var pt in trackPoints)
                {
                    clock.Advance();
                    service.SetPointIndex(point_index);
                    lookout.AlarmTurnAhead(pt, ride_speed, clock.GetTimestamp(), out _);
                    ++point_index;
                }

                double run_time = (Stopwatch.GetTimestamp() - start - 0.0) / Stopwatch.Frequency;

                /*            using (var writer = new GpxDirtyWriter("turns.gpx"))
                            {
                                foreach ((Alarm alarm, int idx) in service.Alarms)
                                {
                                    writer.WritePoint(track_points[idx], alarm.ToString());
                                }
                            }
                            */

                return run_time;
            }
        }
    }
}
