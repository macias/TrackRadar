using System;
using System.Diagnostics;

namespace TrackRadar
{
    public sealed class Statistics
    {
        private readonly object threadLock;

        bool isComputing;
        int skippedComputing;
        int totalLocationUpdates;
        long lastUpdate;
        long totalIdle; // for how long did gps NOT updated us
        double minDist;
        double maxDist;
        double avgDist;
        // off-track distances are stored as >0 values
        // on-track distances as <= 0 values
        double signedCurrDist;
        long computingTime;
        float currAccuracy;

        // "signed" means -- negative values are considered on the track, positive ones -- off the track
        public double SignedDistance { get { lock (this.threadLock) return this.signedCurrDist; } }
        public float Accuracy { get { lock (this.threadLock) return this.currAccuracy; } }

        public Statistics()
        {
            this.threadLock = new object();
            this.currAccuracy = float.MaxValue;
        }
        public void Reset()
        {
            lock (threadLock)
            {
                this.isComputing = false;
                skippedComputing = 0;
                this.totalLocationUpdates = 0;
                this.minDist = this.maxDist = this.avgDist = this.signedCurrDist = 0;
                totalIdle = 0;
                lastUpdate = Stopwatch.GetTimestamp();
                computingTime = 0;
            }
        }

        internal bool CanUpdate()
        {
            lock (threadLock)
            {
                var now = Stopwatch.GetTimestamp();
                totalIdle += (now - lastUpdate);
                lastUpdate = now;

                ++this.totalLocationUpdates;
                bool result = !this.isComputing;
                if (result)
                {
                    this.isComputing = true;
                    computingTime -= Stopwatch.GetTimestamp();
                }
                else
                    ++this.skippedComputing;

                return result;
            }
        }

        internal void UpdateCompleted(double dist,float accuracy)
        {
            lock (threadLock)
            {
                this.signedCurrDist = dist;
                this.currAccuracy = accuracy;
                dist = Math.Abs(dist);
                int computed = totalLocationUpdates - skippedComputing - 1;
                if (computed == 0)
                    this.minDist = this.maxDist = this.avgDist = dist;
                else
                {
                    this.minDist = Math.Min(dist, minDist);
                    this.maxDist = Math.Max(dist, maxDist);
                    this.avgDist = (this.avgDist * computed + dist) / (computed + 1);
                }

                this.isComputing = false;
                computingTime += Stopwatch.GetTimestamp();
            }
        }

        public override string ToString()
        {
            lock (threadLock)
            {
                long time = computingTime;
                if (this.isComputing)
                    time += Stopwatch.GetTimestamp();

                var total = this.totalLocationUpdates;
                var skipped = this.skippedComputing;
                var computed = (total - skipped);

                TimeSpan avg_comp_time = (computed == 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(time * 1.0 / (Stopwatch.Frequency * computed)));
                TimeSpan avg_update_every = (total == 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(totalIdle * 1.0 / (Stopwatch.Frequency * total)));

                return $"Updates: {total} , skipped: {skipped}, dist: {minDist}, {maxDist}, {avgDist} , "
                    + $"avg comp: {avg_comp_time}, update every: {avg_update_every}";
            }
        }
    }
}