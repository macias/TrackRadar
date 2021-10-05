using Geo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using TrackRadar;
using TrackRadar.Implementation;
using TrackRadar.Tests.Implementation;

namespace TestRunner
{
    internal sealed class LoaderProgress
    {
        private double lastSeenValue;
        private GpxLoader.Stage lastStage;
        private long lastTimestamp;
        private readonly List<(GpxLoader.Stage stage, double time)> history;

        public IEnumerable<(GpxLoader.Stage stage, double time)> History => this.history;

        public LoaderProgress()
        {
            this.lastTimestamp = Stopwatch.GetTimestamp();
            this.history = new List<(GpxLoader.Stage stage, double time)>();
        }

        public void OnProgress(GpxLoader.Stage stage, long step,long total)
        {
            double value = GpxLoader.RecomputeProgress(stage, step, total);
            if (value < lastSeenValue)
                throw new ArgumentException();
            lastSeenValue = value;
            if (lastStage == stage)
                return;
            long now = Stopwatch.GetTimestamp();
            history.Add((lastStage,(now-lastTimestamp+0.0)/Stopwatch.Frequency));
            lastTimestamp = now;
            lastStage = stage;
        }
    }
}
