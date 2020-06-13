using System;

namespace TrackRadar.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            new RadarTest().TestSignalChecker();
            Console.WriteLine("done");
            Console.ReadLine();
        }
    }
}