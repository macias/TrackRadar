using System;
using System.Collections.Generic;
using System.Linq;

namespace TrackRadar.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var test = new TurnTest();
            double time_s = Enumerable.Range(0, 30).Select(_ => test.RideWithTurns(new List<Geo.GeoPoint>(),out var _)).Average();
            Console.WriteLine($"Executed in {time_s}s");
            Console.WriteLine("done");
            Console.ReadLine();
        }
    }
}