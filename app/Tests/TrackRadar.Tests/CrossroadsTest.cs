using Geo;
using MathUnit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TrackRadar.Implementation;
using TrackRadar.Tests.Implementation;

namespace TrackRadar.Tests
{
    [TestClass]
    public class CrossroadsTest
    {
        private const double precision = 0.00000001;

        [TestMethod]
        public void EightFigureTest()
        {
            // "oo" like figure, checking if program will make intersection out of it
            var prefs = Toolbox.LowThresholdSpeedPreferences();

            var track1 = new[] {
                GeoPoint.FromDegrees(0, 0),
                GeoPoint.FromDegrees(0, 1),
                GeoPoint.FromDegrees(1, 1),
                GeoPoint.FromDegrees(1, 0),
                GeoPoint.FromDegrees(0, 0),
            };
            var track2 = new[] {
                GeoPoint.FromDegrees(0, 0),
                GeoPoint.FromDegrees(0, -1),
                GeoPoint.FromDegrees(-1, -1),
                GeoPoint.FromDegrees(-1, 0),
                GeoPoint.FromDegrees(0, 0),
            };
            IPlanData plan_data = Toolbox.CreateBasicTrackData(
                null, prefs.OffTrackAlarmDistance, track1, track2);

#if DEBUG
            Assert.AreEqual(0, plan_data.DEBUG_ExtensionCount);
#endif
            Assert.AreEqual(1, plan_data.Crossroads.Count);
        }

        [TestMethod]
        public void CrossFigureTest()
        {
            // "+" like figure, checking if program will make intersection out of it
            var prefs = Toolbox.LowThresholdSpeedPreferences();

            var track1 = new[] {
                GeoPoint.FromDegrees(0, 0),
                GeoPoint.FromDegrees(0, 1),
            };
            var track2 = new[] {
                GeoPoint.FromDegrees(0, 0),
                GeoPoint.FromDegrees(0, -1),
            };
            var track3 = new[] {
                GeoPoint.FromDegrees(0, 0),
                GeoPoint.FromDegrees(1, 0),
            };
            var track4 = new[] {
                GeoPoint.FromDegrees(0, 0),
                GeoPoint.FromDegrees(-1, 0),
            };
            IPlanData plan_data = Toolbox.CreateBasicTrackData(
                null, prefs.OffTrackAlarmDistance, track1, track2, track3, track4);

#if DEBUG
            Assert.AreEqual(0, plan_data.DEBUG_ExtensionCount);
#endif
            // 1 regular + 4 implicit endpoints
            Assert.AreEqual(1+4, plan_data.Crossroads.Count);
        }

        [TestMethod]
        public void FlatExtendedLineTest()
        {
            var prefs = Toolbox.LowThresholdSpeedPreferences();

            GeoPoint turn = GeoPoint.FromDegrees(40, 5);
            var turning_points = new[] { turn };

            GeoPoint top_point = GeoPoint.FromDegrees(42, 5);
            GeoPoint bottom_point = GeoPoint.FromDegrees(38, 5);
            GeoPoint almost_turn_top = GeoPoint.FromDegrees(40.0001, 5);
            GeoPoint almost_turn_bottom = GeoPoint.FromDegrees(39.9999, 5);

            // making gaps on purpose to check if info about primary turn point will propagate over them
            GeoPoint[] track0 = new[] { almost_turn_top, GeoPoint.FromDegrees(40.9999, 5) };
            GeoPoint[] track1 = new[] { bottom_point, GeoPoint.FromDegrees(39, 5) };
            GeoPoint[] track2 = new[] { GeoPoint.FromDegrees(39, 5), almost_turn_bottom };
            GeoPoint[] track3 = new[] { GeoPoint.FromDegrees(41.0001, 5), top_point };
            IPlanData plan_data = Toolbox.CreateBasicTrackData(
                turning_points, prefs.OffTrackAlarmDistance,
                // this flat line, but we already divided it artificially to check if there will be only two sections
                // left and right
                track0,
                track1,
                track2,
                track3
                );

            TestHelper.IsGreaterEqual(prefs.OffTrackAlarmDistance / 2, GeoCalculator.GetDistance(track0[0], track2[1]));
            TestHelper.IsGreaterEqual(prefs.OffTrackAlarmDistance / 2, GeoCalculator.GetDistance(track3[0], track0[1]));
#if DEBUG
            Assert.AreEqual(2, plan_data.DEBUG_ExtensionCount); // not 3, because we have waypoint in the middle
            foreach (GeoPoint pt in plan_data.Segments.SelectMany(it => it.Points()))
            {
                Assert.IsTrue(plan_data.Graph.DEBUG_TryGetTurnInfo(pt, out var primary, out var alt));
                if (pt.Latitude.Degrees<39)
                    Assert.AreEqual(bottom_point, primary.TurnPoint);
                else if (pt.Latitude.Degrees < 41)
                    Assert.AreEqual(turn, primary.TurnPoint);
                else 
                    Assert.AreEqual(top_point, primary.TurnPoint);
                if (pt == almost_turn_top || pt==almost_turn_bottom || pt==bottom_point || pt==top_point)
                    Assert.IsNull(alt);
                else
                    // because we have implicit endpoints the alternate turns will be present for all points except turns themselves
                    Assert.IsNotNull(alt);
            }
#endif
            Assert.AreEqual(2, plan_data.Segments.Select(it => it.SectionId).Distinct().Count());
        }

        [TestMethod]
        public void AlternateTurnForFlatExtendedLineTest()
        {
            var prefs = Toolbox.LowThresholdSpeedPreferences();

            GeoPoint turn1 = GeoPoint.FromDegrees(0, 5);
            GeoPoint turn2 = GeoPoint.FromDegrees(0, 12);

            // flat line but with such lengths that middle track nodes are closer to the first turn
            // so the purpose of this test is to check whether program calculates the secondary turn
            // for middle track nodes corretly (it should simply be the second turn)
            GeoPoint middle1 = GeoPoint.FromDegrees(0, 6.0001);
            GeoPoint middle2 = GeoPoint.FromDegrees(0, 6.9999);
            IPlanData plan_data = Toolbox.ProcessTrackData(new[] {
                new[] { GeoPoint.FromDegrees(0, 5), GeoPoint.FromDegrees(0, 5.9999) },
                new[] { middle1, middle2 },
                new[] { GeoPoint.FromDegrees(0,7.0001), GeoPoint.FromDegrees(0, 12) }
                },
                waypoints: new[] { turn1, turn2 },
                offTrackDistance: prefs.OffTrackAlarmDistance,
                // do not split segments
                segmentLengthLimit: Length.Zero,
                null, CancellationToken.None);

#if DEBUG
            {
                Assert.IsTrue(plan_data.Graph.DEBUG_TryGetTurnInfo(middle1, out var primary, out var alt));
                Assert.AreEqual(turn1, primary.TurnPoint);
                Assert.IsTrue(alt.HasValue);
                Assert.AreEqual(turn2, alt.Value.TurnPoint);
            }
            {
                Assert.IsTrue(plan_data.Graph.DEBUG_TryGetTurnInfo(middle2, out var primary, out var alt));
                Assert.AreEqual(turn1, primary.TurnPoint);
                Assert.IsTrue(alt.HasValue);
                Assert.AreEqual(turn2, alt.Value.TurnPoint);
            }
#endif
        }

        [TestMethod]
        public void AlternateTurnForSinglePointTest()
        {
            var prefs = Toolbox.LowThresholdSpeedPreferences();

            GeoPoint turn1 = GeoPoint.FromDegrees(0, 5);
            GeoPoint turn2 = GeoPoint.FromDegrees(0, 12);

            GeoPoint middle = GeoPoint.FromDegrees(0, 5.1);
            IPlanData plan_data = Toolbox.ProcessTrackData(new[] {
                // extreme case, when middle nodes (here only 1) have all one turning point as primary one
                new[] { GeoPoint.FromDegrees(0, 5), middle, GeoPoint.FromDegrees(0, 12) }
                },
                waypoints: new[] { turn1, turn2 },
                offTrackDistance: prefs.OffTrackAlarmDistance,
                // do not split segments
                segmentLengthLimit: Length.Zero,
                null, CancellationToken.None);

#if DEBUG
            {
                Assert.IsTrue(plan_data.Graph.DEBUG_TryGetTurnInfo(middle, out var primary, out var alt));
                Assert.AreEqual(turn1, primary.TurnPoint);
                Assert.IsTrue(alt.HasValue);
                Assert.AreEqual(turn2, alt.Value.TurnPoint);
            }
#endif
        }

        [TestMethod]
        public void AlternateDistantTurnPointTest()
        {
            var prefs = Toolbox.LowThresholdSpeedPreferences();

            GeoPoint turn1 = GeoPoint.FromDegrees(0, 5);
            GeoPoint turn2 = GeoPoint.FromDegrees(0, 6);
            GeoPoint turn3 = GeoPoint.FromDegrees(0, 15);

            GeoPoint middle = GeoPoint.FromDegrees(0, 7);

            IPlanData plan_data = Toolbox.ProcessTrackData(new[] {
                new[] { turn1, turn2, middle, turn3 }
                },
                waypoints: new[] { turn1, turn2, turn3 },
                offTrackDistance: prefs.OffTrackAlarmDistance,
                // do not split segments
                segmentLengthLimit: Length.Zero,
                null, CancellationToken.None);

            TestHelper.IsGreaterThan(GeoCalculator.GetDistance(middle, turn3), GeoCalculator.GetDistance(middle, turn1));


            Assert.AreEqual(3, plan_data.Crossroads.Count);
#if DEBUG
            {
                Assert.IsTrue(plan_data.Graph.DEBUG_TryGetTurnInfo(middle, out var primary, out var alt));
                Assert.AreEqual(turn2, primary.TurnPoint);
                Assert.IsTrue(alt.HasValue);
                // despite turn1 is closer to track point, its alternate turn is turn3, which is correct
                // because our track point lies between turn2 and turn3, so moving AWAY from closest turn (i.e turn2)
                // will direct us towards turn3, not turn1
                Assert.AreEqual(turn3, alt.Value.TurnPoint);
            }
#endif
        }

        [TestMethod]
        public void SectionIdForFlatIncoming1LineTest()
        {
            var prefs = Toolbox.LowThresholdSpeedPreferences();

            // testing robustness of the assigning section ids -- this time both tracks are facing their "head"s
            IPlanData plan_data = Toolbox.CreateBasicTrackData(
                new[] { GeoPoint.FromDegrees(40.5, 5) }, prefs.OffTrackAlarmDistance,
                // this is just flat line, but two tracks (program should merge them logically)
                new[] { GeoPoint.FromDegrees(40.0001, 5), GeoPoint.FromDegrees(41, 5) },
                new[] { GeoPoint.FromDegrees(39.9999, 5), GeoPoint.FromDegrees(39, 5) }
                );

#if DEBUG
            Assert.AreEqual(1, plan_data.DEBUG_ExtensionCount);
#endif
            Assert.AreEqual(2, plan_data.Segments.Select(it => it.SectionId).Distinct().Count());
        }


        [TestMethod]
        public void SectionIdForFlatIncoming2LineTest()
        {
            var prefs = Toolbox.LowThresholdSpeedPreferences();

            // testing robustness of the assigning section ids -- this time both tracks are facing their "head"s
            IPlanData plan_data = Toolbox.CreateBasicTrackData(
                // waypoints are put at the ends
                new[] { GeoPoint.FromDegrees(41, 5), GeoPoint.FromDegrees(39, 5) },
                prefs.OffTrackAlarmDistance,
                // this is just flat line, but two tracks (program should merge them logically)
                new[] { GeoPoint.FromDegrees(40, 5), GeoPoint.FromDegrees(41, 5) },
                new[] { GeoPoint.FromDegrees(40, 5), GeoPoint.FromDegrees(39, 5) }
                );

#if DEBUG
            Assert.AreEqual(1, plan_data.DEBUG_ExtensionCount);
#endif
            Assert.AreEqual(1, plan_data.Segments.Select(it => it.SectionId).Distinct().Count());
        }

        [TestMethod]
        public void SectionIdForFlatOutgoingLineTest()
        {
            var prefs = Toolbox.LowThresholdSpeedPreferences();
            Speed ride_speed = prefs.RidingSpeedThreshold + Speed.FromKilometersPerHour(10);

            // testing robustness of the assigning section ids -- this time both tracks are facing their "tail"s
            IPlanData plan_data = Toolbox.CreateBasicTrackData(
                new[] { GeoPoint.FromDegrees(40.5, 5) }, prefs.OffTrackAlarmDistance,
                // this is just flat line, but two tracks (program should merge them logically)
                new[] { GeoPoint.FromDegrees(41, 5), GeoPoint.FromDegrees(40, 5) },
                new[] { GeoPoint.FromDegrees(39, 5), GeoPoint.FromDegrees(40, 5) }
                );

#if DEBUG
            Assert.AreEqual(1, plan_data.DEBUG_ExtensionCount);
#endif
            Assert.AreEqual(2, plan_data.Segments.Select(it => it.SectionId).Distinct().Count());
        }

        [TestMethod]
        public void FindingParallelIntersectionsTest()
        {
            Assert.IsFalse(GpxLoader.tryGetSegmentIntersection(GeoPoint.FromDegrees(39, 0), GeoPoint.FromDegrees(40, 0),
                GeoPoint.FromDegrees(40, 0), GeoPoint.FromDegrees(41, 0),
                out _, out _)); // wouldn't mind if this turned out of true (because point is shared)

            Assert.IsTrue(GpxLoader.tryGetSegmentIntersection(GeoPoint.FromDegrees(40, 5), GeoPoint.FromDegrees(41, 5),
                GeoPoint.FromDegrees(41, 5), GeoPoint.FromDegrees(42, 5),
                out _, out _));

            Assert.IsTrue(GpxLoader.tryGetSegmentIntersection(GeoPoint.FromDegrees(38, 5), GeoPoint.FromDegrees(39, 5),
                GeoPoint.FromDegrees(39, 5), GeoPoint.FromDegrees(40, 5),
                out _, out _));
        }

        [TestMethod]
        public void SectionIdForForkedLineTest()
        {
            var prefs = Toolbox.LowThresholdSpeedPreferences();
            Speed ride_speed = prefs.RidingSpeedThreshold + Speed.FromKilometersPerHour(10);

            // shape is like this: >--< (only rotated)
            IPlanData plan_data = Toolbox.CreateBasicTrackData(
                null, prefs.OffTrackAlarmDistance,
                // lower, left
                new[] { GeoPoint.FromDegrees(37, -2), GeoPoint.FromDegrees(38, -1) },
                new[] { GeoPoint.FromDegrees(38, -1), GeoPoint.FromDegrees(39, 0) },

                // lower, right
                new[] { GeoPoint.FromDegrees(37, 2), GeoPoint.FromDegrees(38, 1) },
                new[] { GeoPoint.FromDegrees(38, 1), GeoPoint.FromDegrees(39, 0) },

                // middle, vertical segments
                new[] { GeoPoint.FromDegrees(39, 0), GeoPoint.FromDegrees(40, 0) },
                new[] { GeoPoint.FromDegrees(40, 0), GeoPoint.FromDegrees(41, 0) },

                // upper, right
                new[] { GeoPoint.FromDegrees(41, 0), GeoPoint.FromDegrees(42, 1) },
                new[] { GeoPoint.FromDegrees(42, 1), GeoPoint.FromDegrees(43, 2) },

                // upper, left
                new[] { GeoPoint.FromDegrees(41, 0), GeoPoint.FromDegrees(42, -1) },
                new[] { GeoPoint.FromDegrees(42, -1), GeoPoint.FromDegrees(43, -2) }
            );


            // +4 because of the implicit endpoints
            Assert.AreEqual(2+4, plan_data.Crossroads.Count);
            Assert.AreEqual(5, plan_data.Segments.Select(it => it.SectionId).Distinct().Count());

#if DEBUG
            Assert.AreEqual(5, plan_data.DEBUG_ExtensionCount);
#endif
        }

        [TestMethod]
        public void ExtensionTest()
        {
            // two tracks, like
            // -
            // and
            // L 
            // it should be automatically merged (through extension-crossroads)
            const string filename = @"Data/extension.gpx";

            var prefs = Toolbox.CreatePreferences();
            prefs.OffTrackAlarmDistance = Length.FromMeters(100);
            IPlanData gpx_data = Toolbox.LoadPlan(prefs, filename);
            Assert.IsTrue(Toolbox.TryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(gpx_data.Segments, tracks));
            // +2 because of the implicit endpoints
            Assert.AreEqual(0+2, gpx_data.Crossroads.Count);
#if DEBUG
            Assert.AreEqual(1, gpx_data.DEBUG_ExtensionCount);
#endif       
        }

        [TestMethod]
        public void TripleStarTest()
        {
            // shape like this (two tracks)
            // >-
            const string filename = @"Data/triple-star.gpx";

            var prefs = Toolbox.CreatePreferences();
            prefs.OffTrackAlarmDistance = Length.FromMeters(100);
            IPlanData plan_data = Toolbox.LoadPlan(prefs, filename);
            Assert.IsTrue(Toolbox.TryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(plan_data.Segments, tracks));
            var crossroads = plan_data.GetCrossroadsList();

            //Toolbox.SaveGpxWaypoints("debug.gpx", crossroads);

            // +1 because of the implicit endpoints, not +3 because two extra possible endpoints are too close to computed crossroads
            // and they are simply not added
            Assert.AreEqual(1+1, crossroads.Count);

            Assert.AreEqual(48.5320546303804, crossroads[0].Latitude.Degrees, precision);
            Assert.AreEqual(-23.6676988529768, crossroads[0].Longitude.Degrees, precision);

            // endpoint
            Assert.AreEqual(48.606315, crossroads[1].Latitude.Degrees, precision);
            Assert.AreEqual(-23.707604, crossroads[1].Longitude.Degrees, precision);

#if DEBUG
            Assert.AreEqual(0, plan_data.DEBUG_ExtensionCount);
#endif
        }

        [TestMethod]
        public void CloudyForkTest()
        {
            // shape like this with some waypoints around
            // >-
            const string filename = @"Data/cloudy-fork.gpx";

            var prefs = Toolbox.CreatePreferences();
            prefs.OffTrackAlarmDistance = Length.FromMeters(100);
            IPlanData gpx_data = Toolbox.LoadPlan(prefs, filename);
            Assert.IsTrue(Toolbox.TryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(gpx_data.Segments, tracks));

            // just testing it does not crash
        }

        [TestMethod]
        public void TriangleIntersectingTest()
        {
            // shape like this
            // |>
            const string filename = @"Data/triangle-intersecting.gpx";
            var prefs = Toolbox.CreatePreferences();

            IPlanData gpx_data = Toolbox.LoadPlan(prefs, filename);
            Assert.IsTrue(Toolbox.TryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(gpx_data.Segments, tracks));

            // just testing it does not crash
            // originally it did, because intersections caused lengthening the tracks which is forbidden
        }

        [TestMethod]
        public void TrapezoidTest()
        {
            // one of the side is a bit longer which triggers intersection, not extension
            const string filename = @"Data/trapezoid.gpx";

            var prefs = Toolbox.CreatePreferences();
            prefs.OffTrackAlarmDistance = Length.FromMeters(100);
            IPlanData plan_data = Toolbox.LoadPlan(prefs, filename);
            Assert.IsTrue(Toolbox.TryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(plan_data.Segments, tracks));

            var crossroads = plan_data.GetCrossroadsList();

           // Toolbox.SaveGpxWaypoints("debug.gpx", crossroads);

            Assert.AreEqual(1 , crossroads.Count);

            Assert.AreEqual(29.2661170021499, crossroads[0].Latitude.Degrees, precision);
            Assert.AreEqual(6.2613336505024, crossroads[0].Longitude.Degrees, precision);

#if DEBUG
            Assert.AreEqual(3, plan_data.DEBUG_ExtensionCount);
#endif
        }

        [TestMethod]
        public void DoubleIntersectionTest()
        {
            // shape like:
            // /
            // |)
            // \
            // the left part is single track
            const string filename = @"Data/double-intersection.gpx";

            var prefs = Toolbox.CreatePreferences();
            prefs.OffTrackAlarmDistance = Length.FromMeters(100) ;
            IPlanData plan_data = Toolbox.LoadPlan(prefs, filename);
            Assert.IsTrue(Toolbox.TryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));


            //Toolbox.SaveGpx("segs.gpx", plan_data);
            Assert.IsTrue(containsPoints(plan_data.Segments, tracks));

            var crossroads = plan_data.GetCrossroadsList();

            // +2 because implicit endpoints
            Assert.AreEqual(2+2, crossroads.Count);

            Assert.AreEqual(38.8478391592013, crossroads[0].Latitude.Degrees, precision);
            Assert.AreEqual(-3.71607968045339, crossroads[0].Longitude.Degrees, precision);

            Assert.AreEqual(38.8425854026487, crossroads[1].Latitude.Degrees, precision);
            Assert.AreEqual(-3.71669889530884, crossroads[1].Longitude.Degrees, precision);

            Assert.AreEqual(4, plan_data.Segments.Select(it => it.SectionId).Distinct().Count());
        }

        [TestMethod]
        public void CrossroadsTotalTest()
        {
            const string filename = @"Data/crossroads-total.gpx";

            var prefs = Toolbox.CreatePreferences();
            prefs.OffTrackAlarmDistance = Length.FromMeters(100) ;
            IPlanData plan_data = Toolbox.LoadPlan(prefs, filename);
            Assert.IsTrue(Toolbox.TryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(plan_data.Segments, tracks));

            var crossroads = plan_data.GetCrossroadsList();

            //Toolbox.SaveGpxWaypoints("debug.gpx", crossroads);

            // +3 because of the implicit endpoints
            Assert.AreEqual(13+3, crossroads.Count);

            Assert.AreEqual(16.8936197293979, crossroads[0].Latitude.Degrees, precision);
            Assert.AreEqual(3.1787316182833, crossroads[0].Longitude.Degrees, precision);

            Assert.AreEqual(16.8887736999992, crossroads[1].Latitude.Degrees, precision);
            Assert.AreEqual(3.13779059999673, crossroads[1].Longitude.Degrees, precision);

            Assert.AreEqual(16.8076035000023, crossroads[2].Latitude.Degrees, precision);
            Assert.AreEqual(3.10079109998162, crossroads[2].Longitude.Degrees, precision);

            Assert.AreEqual(16.8592042125708, crossroads[3].Latitude.Degrees, precision);
            Assert.AreEqual(3.14995261250127, crossroads[3].Longitude.Degrees, precision);

            Assert.AreEqual(16.8397027750001, crossroads[4].Latitude.Degrees, precision);
            Assert.AreEqual(3.16074990000049, crossroads[4].Longitude.Degrees, precision);

            Assert.AreEqual(16.8064137001461, crossroads[5].Latitude.Degrees, precision);
            Assert.AreEqual(3.12735877501504, crossroads[5].Longitude.Degrees, precision);

            Assert.AreEqual(16.8024285999989, crossroads[6].Latitude.Degrees, precision);
            Assert.AreEqual(3.20027810000194, crossroads[6].Longitude.Degrees, precision);

            Assert.AreEqual(16.8418774601993, crossroads[7].Latitude.Degrees, precision);
            Assert.AreEqual(3.15659887886566, crossroads[7].Longitude.Degrees, precision);

            Assert.AreEqual(16.8605504750011, crossroads[8].Latitude.Degrees, precision);
            Assert.AreEqual(3.13799612500389, crossroads[8].Longitude.Degrees, precision);

            Assert.AreEqual(16.858500900001, crossroads[9].Latitude.Degrees, precision);
            Assert.AreEqual(3.16086709999214, crossroads[9].Longitude.Degrees, precision);

            Assert.AreEqual(16.8752200999987, crossroads[10].Latitude.Degrees, precision);
            Assert.AreEqual(3.13840510000206, crossroads[10].Longitude.Degrees, precision);

            Assert.AreEqual(16.843227172543, crossroads[11].Latitude.Degrees, precision);
            Assert.AreEqual(3.16077687101759, crossroads[11].Longitude.Degrees, precision);

            Assert.AreEqual(16.8544919000013, crossroads[12].Latitude.Degrees, precision);
            Assert.AreEqual(3.18811959999266, crossroads[12].Longitude.Degrees, precision);

            // endpoints
            Assert.AreEqual(16.800115, crossroads[13].Latitude.Degrees, precision);
            Assert.AreEqual(3.2648134, crossroads[13].Longitude.Degrees, precision);

            Assert.AreEqual(16.8975644, crossroads[14].Latitude.Degrees, precision);
            Assert.AreEqual(3.415575, crossroads[14].Longitude.Degrees, precision);

            Assert.AreEqual(16.8951309, crossroads[15].Latitude.Degrees, precision);
            Assert.AreEqual(3.4033871, crossroads[15].Longitude.Degrees, precision);
        }

        [TestMethod]
        public void IntersectionTest()
        {
            // "X" like figure
            const string filename = @"Data/intersection.gpx";

            var prefs = Toolbox.CreatePreferences();
            prefs.OffTrackAlarmDistance = Length.FromMeters(100) ;
            IPlanData plan_data = Toolbox.LoadPlan(prefs, filename);
            Assert.IsTrue(Toolbox.TryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(plan_data.Segments, tracks));
            // +4 because of the implicit endpoints
            Assert.AreEqual(1+4, plan_data.Crossroads.Count);

            GeoPoint pt = plan_data.Crossroads.First().Key;
            Assert.AreEqual(38.815190752937724, pt.Latitude.Degrees, precision);
            Assert.AreEqual(-3.79047983062, pt.Longitude.Degrees, precision);
        }

        [TestMethod]
        public void IntersectionApartTest()
        {
            // "+" like figure but arms are a little separated from the middle point
            const string filename = @"Data/intersection-apart.gpx";

            var prefs = Toolbox.CreatePreferences();
            prefs.OffTrackAlarmDistance = Length.FromMeters(100) ;
            IPlanData plan_data = Toolbox.LoadPlan(prefs, filename);
            Assert.IsTrue(Toolbox.TryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(plan_data.Segments, tracks));
            // +4 because of the implicit endpoints
            Assert.AreEqual(1+4, plan_data.Crossroads.Count);

            GeoPoint pt = plan_data.Crossroads.First().Key;
            Assert.AreEqual(38.7864047129328, pt.Latitude.Degrees, precision);
            Assert.AreEqual(-3.80869112804318, pt.Longitude.Degrees, precision);
        }

        [TestMethod]
        public void PassingByTest()
        {
            // "><" like figure
            const string filename = @"Data/passing-by.gpx";

            var prefs = Toolbox.CreatePreferences();
            prefs.OffTrackAlarmDistance = Length.FromMeters(100) ;
            IPlanData plan_data = Toolbox.LoadPlan(prefs, filename);
            Assert.IsTrue(Toolbox.TryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(plan_data.Segments, tracks));
            // +4 because of the implicit endpoints
            Assert.AreEqual(1+4, plan_data.Crossroads.Count);

            GeoPoint pt = plan_data.Crossroads.First().Key;
            Assert.AreEqual(38.8446733513524, pt.Latitude.Degrees, precision);
            Assert.AreEqual(-3.7756934500661, pt.Longitude.Degrees, precision);
        }

        [TestMethod]
        public void TJuctionTest()
        {
            // "-<" like figure (3 arms)
            const string filename = @"Data/t-junction.gpx";

            var prefs = Toolbox.CreatePreferences();
            prefs.OffTrackAlarmDistance = Length.FromMeters(100) ;
            IPlanData plan_data = Toolbox.LoadPlan(prefs, filename);
            Assert.IsTrue(Toolbox.TryLoadGpx(filename, out var tracks, out var waypoints, onProgress: null, CancellationToken.None));

            Assert.IsTrue(containsPoints(plan_data.Segments, tracks));
            // +3 because of the implicit endpoints
            Assert.AreEqual(1+3, plan_data.Crossroads.Count);

            GeoPoint pt = plan_data.Crossroads.First().Key;
            Assert.AreEqual(38.795438090984, pt.Latitude.Degrees, precision);
            Assert.AreEqual(-3.80688648895, pt.Longitude.Degrees, precision);


        }

        private bool containsPoints(IEnumerable<ISegment> segments, List<List<GeoPoint>> tracks)
        {
            var segmented = new HashSet<GeoPoint>(segments.SelectMany(it => it.Points()));
            return segmented.IsSupersetOf(tracks.SelectMany(it => it));
        }
    }
}