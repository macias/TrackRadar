namespace TrackRadar.Implementation
{
    internal sealed class KalmanFilter
    {
        private readonly Matrix Q;
        private readonly ITimeStamper timeStamper;
        private Matrix X;
        private Matrix P;
        private Matrix H;
        private Matrix R;

        private long lastTime;

        public KalmanFilter(ITimeStamper timeStamper)
        {
            Q = KalmanFunctions.InitSystemNoiseQ(0, 0, 0, 0, 0, 0);
            this.timeStamper = timeStamper;

            this.lastTime = timeStamper.GetBeforeTimeTimestamp();
        }

        public void Compute(long now, double posX, double posY, double posAccuracy,out double estX,out double estY)
        {
            if (this.lastTime == timeStamper.GetBeforeTimeTimestamp())
            {
                KalmanFunctions.Init(posX, posY, posAccuracy, out X, out P, out H);
                (estX,estY) = (posX, posY);
            }
            else
            {
                double dt = timeStamper.GetSecondsSpan(now, lastTime);
                Matrix F = KalmanFunctions.InitTransitionF(dt);
                R = KalmanFunctions.InitMeasurementNoiseR(posAccuracy);
                (X, P) = KalmanFunctions.Prediction(X, P, Q, F);
                var y = Matrix.Create(2, 1, posX, posY);
                (X, P) = KalmanFunctions.Update(X, P, y, R, H);
                (estX,estY) = (X[0, 0], X[1, 0]);
            }

            this.lastTime = now;
        }

        public void Reset()
        {
            this.lastTime = timeStamper.GetBeforeTimeTimestamp();
        }
    }
}