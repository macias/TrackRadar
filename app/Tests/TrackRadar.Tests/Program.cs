using System;
using System.Collections.Generic;
using System.Linq;
using TrackRadar.Tests.Implementation;

namespace TrackRadar.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            //SpeedEval();
            new MatrixTest().InverseTest();
            //new MatrixTest().MultiplicationTest();
            Console.WriteLine("done");
            Console.ReadLine();
        }

        private static void SpeedEval()
        {
            var test = new TurnTest();
            double time_s = Enumerable.Range(0, 30).Select(_ => test.RideWithTurns(out var _, out var _)).Average();
            Console.WriteLine($"Executed in {time_s}s");
        }
    }
}