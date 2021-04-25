using Geo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using TrackRadar;
using TrackRadar.Tests.Implementation;

namespace TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            //convertTrackToPoints("Data/tight-turns.tracked.gpx", "tight-points.gpx");
            //CheckLoading();            Measure();

            //CheckLoadingOne();
            var test = new TrackRadar.Tests.TurnTest(); test.RideWithTurnsTest();

            //RunAllTests();
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }

        public static void Measure()
        {
            string plan_filename = @"C:\Projekty\TrackRadar\priv-data\warmup-n-merged.plan.gpx";
            string tracked_filename = @"C:\Projekty\TrackRadar\priv-data\warmup-n-merged.tracked.gpx";

            var times = Toolbox.Ride(Toolbox.CreatePreferences(), plan_filename, tracked_filename, null, out _, out _, out _);
            // android-max 0.14-0.40
            // without turns ~0.14
            Console.WriteLine($"times {times}, on android {times.MaxUpdate * 6}, {times.AvgUpdate * 6}");
        }

        public static void CheckLoadingOne()
        {
            var prefs = Toolbox.CreatePreferences();
            string plan_filename = System.IO.Directory.GetFiles(@"C:\Projekty\TrackRadar\priv-data\plan\", "*.gpx")[0];

            Console.WriteLine(plan_filename);

            Toolbox.TryLoadGpx(plan_filename, out List<List<GeoPoint>> tracks, out List<GeoPoint> waypoints, null, CancellationToken.None);

            tracks = tracks.Skip(0).Take(8).ToList();

            tracks.RemoveAt(1);
            tracks.RemoveAt(6);
            tracks.RemoveAt(0);
            tracks.RemoveAt(0);
            tracks.RemoveAt(1);
            tracks.RemoveAt(1);

            tracks[0].RemoveRange(2, 6);

            waypoints = waypoints.Take(0).ToList();

//         Toolbox.SaveGpx("novelty.gpx", tracks, waypoints);
            Toolbox.ProcessTrackData(tracks: tracks, waypoints: waypoints,
                offTrackDistance: prefs.OffTrackAlarmDistance, segmentLengthLimit: GeoMapFactory.SegmentLengthLimit,
                null, CancellationToken.None);

        }

        private static void Offset(ref List<List<GeoPoint>> tracks,
            ref List<GeoPoint> waypoints,
            double latOffset, double lonOffset)
        {
            tracks = tracks.Select(t => t.Select(x => GeoPoint.FromDegrees(x.Latitude.Degrees + latOffset, x.Longitude.Degrees + lonOffset)).ToList()).ToList();
            waypoints = waypoints.Select(x => GeoPoint.FromDegrees(x.Latitude.Degrees + latOffset, x.Longitude.Degrees + lonOffset)).ToList();
        }

        public static void CheckLoading()
        {
            var prefs = Toolbox.CreatePreferences();
            foreach (string plan_filename in System.IO.Directory.GetFiles(@"C:\Projekty\TrackRadar\priv-data\plan\", "*.gpx"))
            {
                Console.WriteLine(plan_filename);
                GpxLoader.ReadGpx(plan_filename, prefs.OffTrackAlarmDistance, Toolbox.OnProgressValidator(), CancellationToken.None);
            }
        }

        private static void RunAllTests()
        {
            var executor = new Program();
            executor.RunAll(typeof(TrackRadar.Tests.KalmanTest).Assembly);
            executor.RunAll(typeof(Geo.Tests.DistanceTests).Assembly);
        }

        public void RunAll(Assembly asm)
        {
            foreach (Type type in asm.GetTypes())
            {
                if (type.GetCustomAttribute(typeof(TestClassAttribute)) != null)
                {
                    RunTests(type);
                }
            }
        }

        private void RunTests(Type type)
        {
            object instance = Activator.CreateInstance(type);
            foreach (MethodInfo minfo in type.GetMethods())
            {
                if (minfo.GetCustomAttribute(typeof(TestMethodAttribute)) == null)
                    continue;

                Console.WriteLine($"{type.Name}.{minfo.Name}");
                var attrs = minfo.GetCustomAttributes<DataRowAttribute>();
                if (attrs.Any())
                {
                    foreach (DataRowAttribute a in attrs)
                        minfo.Invoke(instance, a.Data);
                }
                else
                    minfo.Invoke(instance, new object[] { });
            }
        }

        private static void convertTrackToPoints(string trackPath,string pointsPath)
        {
            if (System.IO.File.Exists(pointsPath))
                throw new ArgumentException($"File {pointsPath} already exists");
            Toolbox.SaveGpxWaypoints(pointsPath, Toolbox.ReadTrackPoints(trackPath));
        }
    }
}
