using Geo;
using Gpx;
using MathUnit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TrackRadar.Implementation;
using TrackRadar.Tests.Implementation;

namespace TrackRadar.Tests
{
    [TestClass]
    public class TurnTest
    {
        private const double precision = 0.00000001;

        [TestMethod]
        public void GeneralAttentionNeededAfterStartTest()
        {
            // the original problem was program created endpoint so close to turn (this is OK), then when riding program recognized as double turn (not OK)
            // so it decided to drop general attention alarm assuming we are tight turns scenario (we are not)

            string plan_filename = Toolbox.TestData("attention-needed-near-start.plan.gpx");
            string tracked_filename = Toolbox.TestData("attention-needed-near-start.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            var stats = Toolbox.Ride(prefs, playDuration: TimeSpan.FromSeconds(2.229), plan_filename, tracked_filename, null,
                out var alarmCounters, out var alarms, out var messages);

            Assert.AreEqual(4, alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 7), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 22), alarms[a++]); // we want here general attention alarm, not direction
            Assert.AreEqual((Alarm.LeftEasy, 24), alarms[a++]);
            Assert.AreEqual((Alarm.LeftEasy, 26), alarms[a++]);
        }


        [TestMethod]
        public void TurningAfterStartTest()
        {
            string plan_filename = Toolbox.TestData("turning-after-start.plan.gpx");
            string tracked_filename = Toolbox.TestData("turning-after-start.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            var stats = Toolbox.Ride(prefs, playDuration: TimeSpan.FromSeconds(2.229), plan_filename, tracked_filename, null,
                out var alarmCounters, out var alarms, out var messages);

            Assert.AreEqual(3, alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Crossroad, 12), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 14), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 16), alarms[a++]);
        }

        [TestMethod]
        public void ComingBackToOffsetTurnTest()
        {
            // we are coming from off-track position towards turn point which does not lie on actual track turn

            // the purpose of this test is to check we have just generic alarms on the turn-point, not directional ones
            // because when coming back we are moving towards track, not along track, so giving directions would be wrong

            string plan_filename = Toolbox.TestData("coming-back-to-offset-turn.plan.gpx");
            string tracked_filename = Toolbox.TestData("coming-back-to-offset-turn.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            Toolbox.Ride(prefs, plan_filename, tracked_filename, null,
                out var alarmCounters, out var alarms, out var messages);

            Toolbox.PrintAlarms(alarms);
            Assert.AreEqual(5, alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.OffTrack, 3), alarms[a++]);
            Assert.AreEqual((Alarm.OffTrack, 13), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 15), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 20), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 22), alarms[a++]);
        }

        [TestMethod]
        public void AttentionTurnTest()
        {
            // we are accelerating towards turn so at first it led to unwanted alarm with direction info (it should be generic attention alarm, as it is now)
            string plan_filename = Toolbox.TestData("attention-turn.plan.gpx");
            string tracked_filename = Toolbox.TestData("attention-turn.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            Toolbox.Ride(prefs, playDuration: TimeSpan.FromSeconds(2.229), plan_filename, tracked_filename, null,
                out var alarmCounters, out var alarms, out var messages);

            Assert.AreEqual(4, alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 16), alarms[a++]); // despite we are accelerating the program should give us generic alarm (now is OK)
            Assert.AreEqual((Alarm.LeftCross, 18), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 20), alarms[a++]);
        }

        [TestMethod]
        public void TwoRegularTurnsTest()
        {
            //            |
            //            |
            ///           |
            //      *-----*
            //      |
            //      |
            ///     |
            // nothing fancy, sanity test

            var prefs = Toolbox.CreatePreferences();
            Speed ride_speed = prefs.RidingSpeedThreshold + Speed.FromKilometersPerHour(10);

            // flat line
            var plan_points = new[] { GeoPoint.FromDegrees(52.985, 25.025), GeoPoint.FromDegrees(53, 25.025), GeoPoint.FromDegrees(53, 25.050), GeoPoint.FromDegrees(53.015, 25.050) };
            var turning_points = plan_points.Skip(1).SkipLast(1).ToList();

            IPlanData gpx_data = Toolbox.CreateBasicTrackData(track: plan_points, waypoints: turning_points, prefs.OffTrackAlarmDistance);

            var track_points = plan_points.ToList();
            Toolbox.PopulateTrackDensely(track_points, ride_speed);

            Toolbox.Ride(prefs, gpx_data, track_points, out var alarm_counters, out var alarms, out var messages);

            Assert.AreEqual(10, alarms.Count);

            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 496), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 498), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 500), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 1007), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 1009), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 1011), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 1520), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 1522), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 1524), alarms[a++]);
        }

        [TestMethod]
        public void TwoRegularTurnsWithLongAlarmsTest()
        {
            //            |
            //            |
            //            |
            //      *-----*
            //      |
            //      |
            //      |
            // nothing fancy, sanity test

            var prefs = Toolbox.CreatePreferences();
            Speed ride_speed = prefs.RidingSpeedThreshold + Speed.FromKilometersPerHour(10);

            // flat line
            var plan_points = new[] { GeoPoint.FromDegrees(52.985, 25.025), GeoPoint.FromDegrees(53, 25.025), GeoPoint.FromDegrees(53, 25.050), GeoPoint.FromDegrees(53.015, 25.050) };
            var turning_points = plan_points.Skip(1).SkipLast(1).ToList();

            IPlanData gpx_data = Toolbox.CreateBasicTrackData(track: plan_points, waypoints: turning_points, prefs.OffTrackAlarmDistance);

            var track_points = plan_points.ToList();
            Toolbox.PopulateTrackDensely(track_points, ride_speed);

            Toolbox.Ride(prefs, playDuration: TimeSpan.FromSeconds(2.229), gpx_data, track_points, out var alarm_counters, out var alarms, out var messages);

            Assert.AreEqual(10, alarms.Count);

            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 495), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 497), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 499), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 1007), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 1009), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 1011), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 1519), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 1521), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 1523), alarms[a++]);
        }

        [TestMethod]
        public void TwoRegularTurnsWithStopTest()
        {
            //            |
            //            |
            //            |
            //      *-O---*
            //      |
            //      |
            //      |
            // same as regular test, only here O we simulate stop

            var prefs = Toolbox.CreatePreferences();
            Speed ride_speed = prefs.RidingSpeedThreshold + Speed.FromKilometersPerHour(10);

            // flat line
            var plan_points = new[] { GeoPoint.FromDegrees(52.985, 25.025), GeoPoint.FromDegrees(53, 25.025), GeoPoint.FromDegrees(53, 25.050), GeoPoint.FromDegrees(53.015, 25.050) };
            var turning_points = plan_points.Skip(1).SkipLast(1).ToList();

            IPlanData gpx_data = Toolbox.CreateBasicTrackData(track: plan_points, waypoints: turning_points, prefs.OffTrackAlarmDistance);

            var track_points = plan_points.ToList();
            Toolbox.PopulateTrackDensely(track_points, ride_speed);

            track_points.InsertRange(600, Enumerable.Range(0, 100).Select(_ => track_points[600]));

            Toolbox.Ride(prefs, gpx_data, track_points, out var alarm_counters, out var alarms, out var messages);

            Assert.AreEqual(12, alarms.Count);

            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 496), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 498), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 500), alarms[a++]);

            Assert.AreEqual((Alarm.Disengage, 602), alarms[a++]);
            Assert.AreEqual((Alarm.Engaged, 703), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 1107), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 1109), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 1111), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 1620), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 1622), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 1624), alarms[a++]);
        }

        [TestMethod]
        public void LeavingEndpointTest()
        {
            var prefs = Toolbox.LowThresholdSpeedPreferences();
            Speed ride_speed = prefs.RidingSpeedThreshold + Speed.FromKilometersPerHour(10);

            // flat line
            var plan_points = new[] { GeoPoint.FromDegrees(40, 5), GeoPoint.FromDegrees(40.01, 5) };

            IPlanData gpx_data = Toolbox.CreateBasicTrackData(track: plan_points, waypoints: null, prefs.OffTrackAlarmDistance);

            var track_points = new[] { plan_points.First(), plan_points.Last() }.ToList();
            Toolbox.PopulateTrackDensely(track_points);

            Toolbox.Ride(prefs, gpx_data, track_points, out var alarm_counters, out var alarms, out var messages);

            Assert.AreEqual(4, alarms.Count());

            int a = 0;
            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 496), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 498), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 500), alarms[a++]);
        }

        [TestMethod]
        public void EndingRideTest()
        {
            // we have very long segment, and 3 turnings points. The purpose of the test is to check if we get
            // notification for the "middle" turn point which is far from segment points (but it lies on the segment)

            var prefs = Toolbox.LowThresholdSpeedPreferences();
            Speed ride_speed = prefs.RidingSpeedThreshold + Speed.FromKilometersPerHour(10);

            // flat line
            GeoPoint endpoint = GeoPoint.FromDegrees(40.005, 5);
            var plan_points = new[] { GeoPoint.FromDegrees(40, 5), GeoPoint.FromDegrees(40.01, 5) };

            IPlanData gpx_data = Toolbox.CreateTrackData(track: plan_points,
                waypoints: null, endpoints: new[] { endpoint }, prefs.OffTrackAlarmDistance);

            var track_points = new[] { plan_points.First(), endpoint }.ToList();
            Toolbox.PopulateTrackDensely(track_points);

            Toolbox.Ride(prefs, gpx_data, track_points, out var alarm_counters, out var alarms, out var messages);

            Assert.AreEqual(4, alarms.Count());

            int a = 0;
            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 240), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 242), alarms[a++]); // no directions, because it is an endpoint
            Assert.AreEqual((Alarm.Crossroad, 244), alarms[a++]);
        }

        [TestMethod]
        public void PickingMiddleTurnTest()
        {
            // we have very long segment, and 3 turnings points. The purpose of the test is to check if we get
            // notification for the "middle" turn point which is far from segment points (but it lies on the segment)

            var prefs = Toolbox.LowThresholdSpeedPreferences();
            Speed ride_speed = prefs.RidingSpeedThreshold + Speed.FromKilometersPerHour(10);

            // flat line
            var turning_points = new[] { GeoPoint.FromDegrees(40, 5), GeoPoint.FromDegrees(40.05, 5), GeoPoint.FromDegrees(40.1, 5) };

            IPlanData gpx_data = Toolbox.CreateBasicTrackData(track: new[] { turning_points.First(), turning_points.Last() },
                waypoints: turning_points, prefs.OffTrackAlarmDistance);

            var track_points = new[] { turning_points.First(), turning_points.Last() }.ToList();
            Toolbox.PopulateTrackDensely(track_points);

            Toolbox.Ride(prefs, gpx_data, track_points, out var alarm_counters, out var alarms, out var messages);

            Assert.AreEqual(7, alarms.Count());
            //            Assert.AreEqual(2, messages.Count());

            int a = 0;
            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);
            // Assert.AreEqual((TurnLookout.LeavingTurningPoint, 5), messages[0]);
            // Assert.AreEqual((TurnLookout.LeavingTurningPoint, 6), messages[1]);

            Assert.AreEqual((Alarm.Crossroad, 2031), alarms[a++]);
            Assert.AreEqual((Alarm.GoAhead, 2033), alarms[a++]);
            Assert.AreEqual((Alarm.GoAhead, 2035), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 4079), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 4081), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 4083), alarms[a++]);
        }

        [TestMethod]
        public void TightTurnsTest()
        {
            // please note the tracked file went off-track, so the last alarms are junk
            string plan_filename = Toolbox.TestData("tight-turns.plan.gpx");
            string tracked_filename = Toolbox.TestData("tight-turns.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            Toolbox.Ride(prefs, plan_filename, tracked_filename, null,
                out var alarmCounters, out var alarms, out var messages);

            Assert.AreEqual(8, alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.DoubleTurn, 9), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 11), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 13), alarms[a++]);

            //Assert.AreEqual((Alarm.DoubleTurn, 24), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 27), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 29), alarms[a++]);

            Assert.AreEqual((Alarm.OffTrack, 46), alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 48), alarms[a++]);
        }

        [TestMethod]
        public void DoubleTurnForkedTest()
        {
            // we have plan in shape of 
            // T
            // when coming to the middle turn program should NOT give double-turn warning (despite the adjacent turn is in range)
            // because there are two possible outgoing tracks, user was notified about this by generic alarm (instead of navigational
            // one) so she/he has to slow down anyway, so there is no point in messing with yet another watch-out/slow-down alarm
            string plan_filename = Toolbox.TestData("double-turn-forked.plan.gpx");
            string tracked_filename = Toolbox.TestData("no-back-double-turn.tracked.gpx"); // yes, the track is from another test

            var prefs = Toolbox.CreatePreferences();
            Toolbox.LoadData(prefs, plan_filename, tracked_filename,
                out IPlanData plan_data, out List<GeoPoint> track_points);

            Toolbox.Ride(prefs, plan_data, track_points,
                out var alarmCounters, out var alarms, out var messages);

            Assert.AreEqual(9, alarms.Count);

            Assert.AreEqual((Alarm.Engaged, 3), alarms[0]);
            Assert.AreEqual((Alarm.Crossroad, 10), alarms[1]);
            // this is kind of wrong, but since we allow manually created turn points and track this can happen
            // the "error" in manual plan is so big that program detects angled turn, this is "by desing"
            // maybe we can mitigate a bit, but it is rather wasting time
            Assert.AreEqual((Alarm.RightEasy, 12), alarms[2]);
            Assert.AreEqual((Alarm.RightEasy, 14), alarms[3]);
            Assert.AreEqual((Alarm.Crossroad, 39), alarms[4]);
            Assert.AreEqual((Alarm.Crossroad, 41), alarms[5]);
            Assert.AreEqual((Alarm.Crossroad, 43), alarms[6]);
            // no double-turn because this turn is forked
            Assert.AreEqual((Alarm.Crossroad, 46), alarms[7]);
            Assert.AreEqual((Alarm.Disengage, 67), alarms[8]);
        }

        [TestMethod]
        public void NoBackDoubleTurnTest()
        {
            // those files are based on real one, but they are rigged a bit to try tricking the program
            // the track looks like
            // L
            // the idea is program should warn (double-turn) before getting to middle turn, but
            // the distances are set in such way that the closest turn is the one in the back (the one we came from)
            // program should ignore it and correctly warn about _incoming_ adjacent turn

            string plan_filename = Toolbox.TestData("no-back-double-turn.plan.gpx");
            string tracked_filename = Toolbox.TestData("no-back-double-turn.tracked.gpx");

            var prefs = Toolbox.CreatePreferences();
            Toolbox.LoadData(prefs, plan_filename, tracked_filename,
                out IPlanData plan_data, out List<GeoPoint> track_points);

            Toolbox.Ride(prefs, plan_data, track_points,
                out var alarmCounters, out var alarms, out var messages);

            Assert.AreEqual(10, alarms.Count);

            Assert.AreEqual((Alarm.Engaged, 3), alarms[0]);
            Assert.AreEqual((Alarm.Crossroad, 10), alarms[1]);
            // this is kind of wrong, but since we allow manually created turn points and track this can happen
            // the "error" in manual plan is so big that program detects angled turn, this is "by desing"
            // maybe we can mitigate a bit, but it is rather wasting time
            Assert.AreEqual((Alarm.RightEasy, 12), alarms[2]);
            Assert.AreEqual((Alarm.RightEasy, 14), alarms[3]);
            Assert.AreEqual((Alarm.Crossroad, 39), alarms[4]);
            Assert.AreEqual((Alarm.RightCross, 41), alarms[5]);
            Assert.AreEqual((Alarm.RightCross, 43), alarms[6]);
            Assert.AreEqual((Alarm.DoubleTurn, 45), alarms[7]);
            Assert.AreEqual((Alarm.Crossroad, 47), alarms[8]);
            Assert.AreEqual((Alarm.Disengage, 67), alarms[9]);
        }


        [TestMethod]
        public void TightTurnsShiftedTest()
        {
            // please note the tracked file went off-track, so the last alarms are junk
            string plan_filename = Toolbox.TestData("tight-turns-shifted.plan.gpx");
            string tracked_filename = Toolbox.TestData("tight-turns.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            Toolbox.Ride(prefs, plan_filename, tracked_filename, null,
                out var alarmCounters, out var alarms, out var messages);

            Assert.AreEqual(8, alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.DoubleTurn, 9), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 11), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 13), alarms[a++]);

            // Assert.AreEqual((Alarm.DoubleTurn, 23), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 26), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 28), alarms[a++]);

            Assert.AreEqual((Alarm.OffTrack, 46), alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 48), alarms[a++]);
        }

        [TestMethod]
        public void TightTurnsSpeedUpExitTest()
        {
            // please note the tracked file went off-track, so the last alarms are junk
            string plan_filename = Toolbox.TestData("tight-turns.plan.gpx");
            string tracked_filename = Toolbox.TestData("tight-turns.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            Toolbox.LoadData(prefs, plan_filename, tracked_filename,
                out IPlanData plan_data, out List<GeoPoint> track_points);

            // speed up riding through the second turn
            track_points.RemoveAt(28);

            Toolbox.Ride(prefs, plan_data, track_points,
                out var alarmCounters, out var alarms, out var messages);

            Assert.AreEqual(7, alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.DoubleTurn, 9), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 11), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 13), alarms[a++]);

            //Assert.AreEqual((Alarm.DoubleTurn, 24), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 27), alarms[a++]);
            // up to this point it should be the same as non-speed-up version
            // there is no room before turn-point to squeeze in another alarm

            Assert.AreEqual((Alarm.OffTrack, 45), alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 47), alarms[a++]);
        }

        [TestMethod]
        public void TightTurnsShiftedSpeedUpExitTest()
        {
            // please note the tracked file went off-track, so the last alarms are junk
            string plan_filename = Toolbox.TestData("tight-turns-shifted.plan.gpx");
            string tracked_filename = Toolbox.TestData("tight-turns.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            Toolbox.LoadData(prefs, plan_filename, tracked_filename,
                out IPlanData plan_data, out List<GeoPoint> track_points);

            // speed up riding through the second turn
            track_points.RemoveAt(27);

            Toolbox.Ride(prefs, plan_data, track_points,
                out var alarmCounters, out var alarms, out var messages);

            Assert.AreEqual(8, alarms.Count);
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.DoubleTurn, 9), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 11), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 13), alarms[a++]);

            // Assert.AreEqual((Alarm.DoubleTurn, 23), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 26), alarms[a++]);
            // up to this point it should be the same as non-speed-up version
            Assert.AreEqual((Alarm.LeftCross, 28), alarms[a++]);

            Assert.AreEqual((Alarm.OffTrack, 45), alarms[a++]);
            Assert.AreEqual((Alarm.Disengage, 47), alarms[a++]);
        }

        [TestMethod]
        public void ReverseTightTurnsTest()
        {
            string plan_filename = Toolbox.TestData("tight-turns.plan.gpx");
            string tracked_filename = Toolbox.TestData("tight-turns.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            Toolbox.Ride(prefs, plan_filename, tracked_filename, null,
                out var alarmCounters, out var alarms, out var messages, reverse: true);

            Assert.AreEqual(3, alarms.Count);

            Assert.AreEqual((Alarm.Crossroad, 78), alarms[0]);
            Assert.AreEqual((Alarm.LeftCross, 91), alarms[1]);
            Assert.AreEqual((Alarm.LeftCross, 93), alarms[2]);

        }

        [TestMethod]
        public void ReverseTightTurnsShiftedTest()
        {
            string plan_filename = Toolbox.TestData("tight-turns-shifted.plan.gpx");
            string tracked_filename = Toolbox.TestData("tight-turns.tracked.gpx");

            var prefs = Toolbox.CreatePreferences(); // regular thresholds for speed
            Toolbox.Ride(prefs, plan_filename, tracked_filename, null,
                out var alarmCounters, out var alarms, out var messages, reverse: true);

            Assert.AreEqual(3, alarms.Count);

            // first turn is between points 90 and 91
            Assert.AreEqual((Alarm.Crossroad, 78), alarms[0]);
            Assert.AreEqual((Alarm.LeftCross, 91), alarms[1]);
            Assert.AreEqual((Alarm.LeftCross, 93), alarms[2]);
        }

        [TestMethod]
        public void ZTwoTurnsSlowSpeedTest()
        {
            string plan_filename = Toolbox.TestData("z-two-turns.plan.gpx");
            string tracked_filename = Toolbox.TestData("z-two-turns.mocked.gpx");

            var prefs = Toolbox.CreatePreferences();
            IPlanData gpx_data = Toolbox.LoadPlan(prefs, plan_filename);

            var track_points = Toolbox.ReadTrackGpxPoints(tracked_filename).Select(it => GpxHelper.FromGpx(it)).ToList();

            Toolbox.PopulateTrackDensely(track_points);

            Toolbox.Ride(prefs, gpx_data, track_points, out IReadOnlyDictionary<Alarm, int> alarmCounters,
                out IReadOnlyList<(Alarm alarm, int index)> alarms,
                out IReadOnlyList<(string message, int index)> messages);

            Assert.AreEqual(2, alarms.Count());

            Assert.AreEqual((Alarm.Engaged, 1027), alarms[0]); // this point is the second turn (right one)
            Assert.AreEqual((Alarm.Crossroad, 1039), alarms[1]);



            Assert.AreEqual((TurnLookout.LeavingTurningPoint, 1041), messages[0]);
        }

        [TestMethod]
        public void AlternateTurnsTest()
        {
            // shape like this:
            //     +-----
            // ----+

            var track_points = new[] {
                GeoPoint.FromDegrees(50.918540800,2.921173700),
                GeoPoint.FromDegrees(50.914250700,2.912756900),
                GeoPoint.FromDegrees(50.914156000,2.913014400),
                GeoPoint.FromDegrees(50.909114600,2.904568100),
            }.ToList();

            // ensuring "tail" segments are short enough
            TestHelper.IsGreaterThan(GeoMapFactory.SegmentLengthLimit, GeoCalculator.GetDistance(track_points[0], track_points[1]));
            TestHelper.IsGreaterThan(GeoMapFactory.SegmentLengthLimit, GeoCalculator.GetDistance(track_points[2], track_points[3]));

            var prefs = Toolbox.CreatePreferences();
            GeoPoint[] waypoints = new[] { track_points[1], track_points[2] };
            var plan_data = Toolbox.CreateBasicTrackData(track_points, waypoints, prefs.OffTrackAlarmDistance);

            // Toolbox.SaveGpx("alt-turns.plan.gpx", plan_data);

            Assert.AreEqual(6, plan_data.Segments.Count());

#if DEBUG
            IEnumerable<DEBUG_TrackToTurnHack> alts = plan_data.Graph.DEBUG_TrackToTurns.Where(it => it.Alternate.HasValue)
                .ToArray();
            var track_points_with_alt = alts.Select(it => it.TrackPoint).ToHashSet();
            // checking if the alternate turn points are assigned only to the single section + two alternates
            // because of the implicit endpoints
            Assert.AreEqual(1 + 2, plan_data.Segments
                .Where(seg => track_points_with_alt.Contains(seg.A) || track_points_with_alt.Contains(seg.B))
                .Select(it => it.SectionId)
                .Distinct()
                .Count());
            // alternate turn points are not placed on turn points
            Assert.IsFalse(track_points_with_alt.Any(it => waypoints.Contains(it)));
            // we should have alternative turn point, basically one turn part should point to the other
            // (1 regular + 2 because of the implicit endpoints)
            Assert.AreEqual(1 + 2, alts.Select(it => it.Alternate.Value.TurnPoint).Distinct().Count());
#endif
            Toolbox.PopulateTrackDensely(track_points, Speed.FromKilometersPerHour(17));

            //  Toolbox.SaveGpxSegments("alt-turns.mocked.gpx", track_points);
            // Toolbox.SaveGpxWaypoints("alt-turns.points.gpx", track_points);

            Toolbox.Ride(prefs, plan_data, track_points, out var alarm_counters, out var alarms, out var messages);

            Assert.AreEqual(9, alarms.Count());
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.DoubleTurn, 240), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 242), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 244), alarms[a++]);

            //Assert.AreEqual((Alarm.DoubleTurn, 255), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 257), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 259), alarms[a++]);

            // reaching endpoint alarms
            Assert.AreEqual((Alarm.Crossroad, 504), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 506), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 508), alarms[a++]);
        }


        [TestMethod]
        public void AlternateTurnsWithStartingTurnTest()
        {
            // shape like this:
            //     +-----
            // ----+

            var track_points = new[] {
                GeoPoint.FromDegrees(50.918540800,2.921173700),
                GeoPoint.FromDegrees(50.914250700,2.912756900),
                GeoPoint.FromDegrees(50.914156000,2.913014400),
                GeoPoint.FromDegrees(50.909114600,2.904568100),
            }.ToList();

            var prefs = Toolbox.CreatePreferences();
            Speed riding_speed = Speed.FromKilometersPerHour(17);

            GeoPoint[] waypoints = new[] {
                // extra starting turn -- program should not take it into account on the next turn 
                track_points[0],
                track_points[1], track_points[2] };

            // making sure the distance between turns are big enough so the clear part will kick off
            TestHelper.IsGreaterThan(GeoCalculator.GetDistance(waypoints[0], waypoints[1]),
                TurnLookout.GetTurnClearDistance(riding_speed * prefs.TurnAheadAlarmDistance));


            var plan_data = Toolbox.CreateBasicTrackData(track_points, waypoints, prefs.OffTrackAlarmDistance);

            Toolbox.PopulateTrackDensely(track_points, riding_speed);

            Toolbox.Ride(prefs, plan_data, track_points, out var alarm_counters, out var alarms, out var messages);

            Assert.AreEqual(9, alarms.Count());
            int a = 0;

            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);

            Assert.AreEqual((Alarm.DoubleTurn, 240), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 242), alarms[a++]);
            Assert.AreEqual((Alarm.LeftCross, 244), alarms[a++]);

            // Assert.AreEqual((Alarm.DoubleTurn, 255), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 257), alarms[a++]);
            Assert.AreEqual((Alarm.RightCross, 259), alarms[a++]);

            Assert.AreEqual((Alarm.Crossroad, 504), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 506), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 508), alarms[a++]);
        }

        [TestMethod]
        public void WalkingPastTurnPointTest()
        {
            // shape like this:
            //     +-----
            //     |

            // this is accident-test, I found out that with this data the first alarms is after second turn because the computed
            // speed was too low. This is OK, but the program gave wrong turn-info -- so this test serves as opportunity to tackle
            // with walking speed and turns handling

            var track_points = new[] {
                GeoPoint.FromDegrees(50.914156000, 2.913014400),
                GeoPoint.FromDegrees(50.914250700, 2.912756900),
                GeoPoint.FromDegrees(50.918540800, 2.921173700),
            }.ToList();

            var prefs = Toolbox.CreatePreferences();
            Speed riding_speed = Speed.FromKilometersPerHour(17);

            GeoPoint[] waypoints = track_points.ToArray();

            var plan_data = Toolbox.CreateBasicTrackData(track_points, waypoints, prefs.OffTrackAlarmDistance);

            Toolbox.PopulateTrackDensely(track_points, riding_speed);

            Toolbox.Ride(prefs, playDuration: null, plan_data, track_points,
                out var alarm_counters, out var alarms, out var messages,
                out TurnLookout lookout);

            // making sure the distance between turns are small enough so in theory there should be double turn alarm
            Length double_turn_limit = lookout.GetDoubleTurnLengthLimit(riding_speed);
            TestHelper.IsGreaterThan(double_turn_limit, GeoCalculator.GetDistance(waypoints[0], waypoints[1]));

            Assert.AreEqual(4, alarms.Count());

            Assert.AreEqual((Alarm.Engaged, 11), alarms[0]);

            Assert.AreEqual((Alarm.Crossroad, 248), alarms[1]);
            Assert.AreEqual((Alarm.Crossroad, 250), alarms[2]);
            Assert.AreEqual((Alarm.Crossroad, 252), alarms[3]);
        }

        [TestMethod]
        public void ZTwoTurnsTest()
        {
            string plan_filename = Toolbox.TestData("z-two-turns.plan.gpx");
            string tracked_filename = Toolbox.TestData("z-two-turns.mocked.gpx");

            var prefs = Toolbox.LowThresholdSpeedPreferences();
            IPlanData gpx_data = Toolbox.LoadPlan(prefs, plan_filename);

            var track_points = Toolbox.ReadTrackGpxPoints(tracked_filename).Select(it => GpxHelper.FromGpx(it)).ToList();

            Toolbox.PopulateTrackDensely(track_points);

            Assert.AreEqual(3, gpx_data.Segments.Select(it => it.SectionId).Distinct().Count());

            Toolbox.Ride(prefs, gpx_data, track_points, out var alarm_counters, out var alarms, out var messages);

            Assert.AreEqual(7, alarms.Count());

            Assert.AreEqual(Alarm.Engaged, alarms[0].alarm);
            Assert.AreEqual(3, alarms[0].index);
            Assert.AreEqual(Alarm.Crossroad, alarms[1].alarm);
            Assert.AreEqual(495, alarms[1].index);
            Assert.AreEqual(Alarm.LeftSharp, alarms[2].alarm);
            Assert.AreEqual(497, alarms[2].index);
            Assert.AreEqual(Alarm.LeftSharp, alarms[3].alarm);
            Assert.AreEqual(499, alarms[3].index);
            Assert.AreEqual(Alarm.Crossroad, alarms[4].alarm);
            Assert.AreEqual(1006, alarms[4].index);
            Assert.AreEqual(Alarm.RightSharp, alarms[5].alarm);
            Assert.AreEqual(1008, alarms[5].index);
            Assert.AreEqual(Alarm.RightSharp, alarms[6].alarm);
            Assert.AreEqual(1010, alarms[6].index);
        }

        [TestMethod]
        public void DuplicateTurnPointTest()
        {
            // basically L track with duplicate waypoint at the turn
            string plan_filename = Toolbox.TestData("dup-turn-point.plan.gpx");
            string tracked_filename = Toolbox.TestData("dup-turn-point.tracked.gpx");

            var prefs = Toolbox.CreatePreferences();

            Toolbox.LoadData(prefs, plan_filename, tracked_filename, out IPlanData plan_data,
                out List<GeoPoint> track_points);

            Assert.AreEqual(2, plan_data.Segments.Select(it => it.SectionId).Distinct().Count());

            Toolbox.Ride(prefs, plan_data, track_points, out var alarm_counters, out var alarms, out var messages);

            Assert.AreEqual(4, alarms.Count);
            int a = 0;
            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);
            Assert.AreEqual((Alarm.Crossroad, 63), alarms[a++]);
            Assert.AreEqual((Alarm.RightSharp, 65), alarms[a++]);
            Assert.AreEqual((Alarm.RightSharp, 67), alarms[a++]);
        }

        [TestMethod]
        public void TurnKindsOnBearingTest()
        {
            {
                var turn = new Turn(Angle.FromDegrees(231.745654889123), Angle.FromDegrees(306.502092791698));
                Assert.IsFalse(TurnLookout.LegacyComputeTurnKind(Angle.FromDegrees(293.147847303439), turn, out TurnKind tk));

            }
            {
                // we have basically turn
                // +-
                // |
                // and we are comming from the bottom, going up
                var turn = new Turn(Angle.FromDegrees(359), Angle.FromDegrees(270));
                Assert.IsTrue(TurnLookout.LegacyComputeTurnKind(Angle.FromDegrees(1), turn, out TurnKind tk));
                Assert.AreEqual(TurnKind.RightCross, tk);
            }
            {
                var turn = new Turn(Angle.FromDegrees(1), Angle.FromDegrees(270));
                Assert.IsTrue(TurnLookout.LegacyComputeTurnKind(Angle.FromDegrees(359), turn, out TurnKind tk));
                Assert.AreEqual(TurnKind.RightCross, tk);

            }
        }

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

                IGeoMap map = Toolbox.CreateTrackMap(new[] { a, b, c });
                Assert.IsTrue(TurnCalculator.TryComputeTurn(b, map,
                    turn_ahead_distance, out Turn turn));
                Assert.AreEqual(359.475341177312, turn.BearingA.Degrees, precision);
                Assert.AreEqual(178.802134619193, turn.BearingB.Degrees, precision);
            }

            {
                GeoPoint a = GeoPoint.FromDegrees(37.41428, -94.70178);
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.49093, -94.4816);

                Assert.IsTrue(TurnCalculator.TryComputeTurn(b,
                    Toolbox.CreateTrackMap(new[] { a, b, c }),
                    turn_ahead_distance, out Turn turn));
                Assert.AreEqual(359.475341177312, turn.BearingA.Degrees, precision);
                Assert.AreEqual(312.425971052848, turn.BearingB.Degrees, precision);
            }

            {
                GeoPoint a = GeoPoint.FromDegrees(37.41428, -94.70178);
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.70202, -94.47611);

                Assert.IsTrue(TurnCalculator.TryComputeTurn(b,
                    Toolbox.CreateTrackMap(new[] { a, b, c }),
                    turn_ahead_distance, out Turn turn));
                Assert.AreEqual(359.475341177312, turn.BearingA.Degrees, precision);
                Assert.AreEqual(254.698737269103, turn.BearingB.Degrees, precision);
            }

            {
                GeoPoint a = GeoPoint.FromDegrees(37.41428, -94.70178);
                GeoPoint b = GeoPoint.FromDegrees(37.65278, -94.70453);
                GeoPoint c = GeoPoint.FromDegrees(37.84639, -94.58322);

                Assert.IsTrue(TurnCalculator.TryComputeTurn(b,
                    Toolbox.CreateTrackMap(new[] { a, b, c }),
                    turn_ahead_distance, out Turn turn));
                Assert.AreEqual(359.475341177312, turn.BearingA.Degrees, precision);
                Assert.AreEqual(206.32012655082, turn.BearingB.Degrees, precision);
            }
        }

        [TestMethod]
        public void RideWithTurnsTest()
        {
            const string result_filename = "turning-excercise.result.gpx";
            IReadOnlyList<GpxWayPoint> turn_points = Toolbox.ReadWaypoints(Toolbox.TestData(result_filename)).ToList();

            List<GeoPoint> track_points = new List<GeoPoint>();
            RideWithTurns(track_points, out IReadOnlyList<(Alarm alarm, int index)> alarms);

            // those 3 extras come from implicit endpoint
            Assert.AreEqual(turn_points.Count + 3, alarms.Count());
            int i = 0;
            for (; i < turn_points.Count; ++i)
            {
                Assert.AreEqual(turn_points[i].Latitude.Degrees, track_points[alarms[i].index].Latitude.Degrees, precision);
                Assert.AreEqual(turn_points[i].Longitude.Degrees, track_points[alarms[i].index].Longitude.Degrees, precision);
                Assert.AreEqual(Enum.Parse<Alarm>(turn_points[i].Name), alarms[i].alarm);
            }

            // endpoint (end of ride) alarms
            Assert.AreEqual((Alarm.Crossroad, 19500), alarms[i++]);
            Assert.AreEqual((Alarm.Crossroad, 19502), alarms[i++]);
            Assert.AreEqual((Alarm.Crossroad, 19504), alarms[i++]);
        }

        internal double RideWithTurns(List<GeoPoint> trackPoints, out IReadOnlyList<(Alarm alarm, int index)> alarms)
        {
            const string plan_filename = "turning-excercise.gpx";

            var prefs = Toolbox.CreatePreferences();
            prefs.TurnAheadAlarmDistance = TimeSpan.FromSeconds(13);

            trackPoints.AddRange(Toolbox.ReadTrackGpxPoints(Toolbox.TestData(plan_filename)).Select(it => GpxHelper.FromGpx(it)));

            var gpx_data = Toolbox.CreateBasicTrackData(trackPoints,
                //new TrackData(Enumerable.Range(0, trackPoints.Count - 1)
                //.Select(i => new Segment(trackPoints[i], trackPoints[i + 1])),
                // set each in-track point as turning one
                trackPoints.Skip(1).SkipLast(1), prefs.OffTrackAlarmDistance);

            // populate densely the "ride" points (better than having 1MB file on disk)
            Toolbox.PopulateTrackDensely(trackPoints);

            var clock = new SecondStamper();
            using (var raw_alarm_master = new AlarmMaster(clock))
            {
                raw_alarm_master.PopulateAlarms();
                var counting_alarm_master = new CountingAlarmMaster(raw_alarm_master);
                var service = new Implementation.RadarService(prefs, clock);
                var sequencer = new AlarmSequencer(service, counting_alarm_master);
                IGeoMap map = RadarCore.CreateTrackMap(gpx_data.Segments);
                var lookout = new TurnLookout(service, sequencer, clock, gpx_data, map);
                Speed ride_speed = Speed.FromKilometersPerHour(10);

                long start = Stopwatch.GetTimestamp();

                int point_index = 0;
                GeoPoint last_pt = trackPoints.First();
                foreach (var pt in trackPoints)
                {
                    using (sequencer.OpenAlarmContext(gpsAcquired: false, hasGpsSignal: true))
                    {
                        clock.Advance();
                        counting_alarm_master.SetPointIndex(point_index);
                        PositionCalculator.IsOnTrack(pt, map, prefs.OffTrackAlarmDistance,
                            out ISegment segment, out _, out ArcSegmentIntersection cx_info);
                        lookout.AlarmTurnAhead(pt, segment, cx_info, ride_speed, comebackOnTrack: false, clock.GetTimestamp(), out _);
                        ++point_index;
                        last_pt = pt;
                    }
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

                alarms = counting_alarm_master.Alarms;
                return run_time;
            }
        }



        [TestMethod]
        public void DeadSpotOnTurningPointTest()
        {
            var prefs = Toolbox.CreatePreferences();
            Speed ride_speed = prefs.RidingSpeedThreshold + Speed.FromKilometersPerHour(10);

            // L-shape, but here it is irrelevant
            var points = new[] { GeoPoint.FromDegrees(41,6),
                GeoPoint.FromDegrees(38,6),
                GeoPoint.FromDegrees(38,11) };
            var turning_point = points[1];

            var gpx_data = Toolbox.CreateBasicTrackData(points,
                // set each in-track point as turning one
                points.Skip(1).SkipLast(1), prefs.OffTrackAlarmDistance);

            var clock = new SecondStamper();
            using (var raw_alarm_master = new AlarmMaster(clock))
            {
                raw_alarm_master.PopulateAlarms();
                var counting_alarm_master = new CountingAlarmMaster(raw_alarm_master);
                var service = new Implementation.RadarService(prefs, clock);
                var sequencer = new AlarmSequencer(service, counting_alarm_master);
                IGeoMap map = RadarCore.CreateTrackMap(gpx_data.Segments);
                var lookout = new TurnLookout(service, sequencer, clock, gpx_data, map);
                PositionCalculator.IsOnTrack(turning_point, map, prefs.OffTrackAlarmDistance,
                    out ISegment segment, out _, out ArcSegmentIntersection cx_info);
                // simulate we are exactly at turning point (no bearing then) and look out for program crash
                lookout.AlarmTurnAhead(turning_point, segment, cx_info, ride_speed, comebackOnTrack: false, clock.GetTimestamp(), out _);
            }
        }


        [TestMethod]
        public void LeavingTurningPointTest()
        {
            var prefs = Toolbox.CreatePreferences();

            // L-shape, but here it is irrelevant
            const double leaving_latitude = 38;
            const double leaving_longitude_start = 6;
            const double leaving_longitude_end = 6.1;

            var span_points = new[] { GeoPoint.FromDegrees(38.1,leaving_longitude_start),
                GeoPoint.FromDegrees(leaving_latitude,leaving_longitude_start),
                GeoPoint.FromDegrees(leaving_latitude,leaving_longitude_end) };

            List<GeoPoint> track_points;
            {
                const int parts = 1000;
                track_points = Enumerable.Range(0, parts).Select(i => GeoPoint.FromDegrees(leaving_latitude,
                    leaving_longitude_start + i * (leaving_longitude_end - leaving_longitude_start) / parts))
                    .Take(100)
                    .Skip(10)
                    .ToList();
            }

            var gpx_data = Toolbox.CreateBasicTrackData(track: span_points,
                // set each in-track point as turning one
                waypoints: new[] { span_points[1] },
                prefs.OffTrackAlarmDistance);


            Toolbox.Ride(prefs, gpx_data, track_points, out var alarm_counters, out var alarms, out var messages);

            Assert.AreEqual(1, alarms.Count());
            int a = 0;

            // we don't get crossroad alarm for leaving, because program detects implicit endpoint ahead of us (alternative turn-point)
            // but since it is too far it cannot alarm about it as well
            // thus we got general engaged alarm
            Assert.AreEqual((Alarm.Engaged, 3), alarms[a++]);
        }
    }
}
