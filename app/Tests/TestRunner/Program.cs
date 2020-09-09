using System;
using TrackRadar.Tests;

namespace TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new TurnTest();
            test.TurnKindsOnBearingTest();
            test.DuplicateTurnPointTest();
            Console.WriteLine("Hello World!");
        }
    }
}
