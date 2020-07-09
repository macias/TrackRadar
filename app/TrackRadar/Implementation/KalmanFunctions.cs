namespace TrackRadar.Implementation
{
    internal static class KalmanFunctions
    {
        public static void Init(double posX, double posY, double posAccuracy,
            out Matrix X, out Matrix P, 
            //out Matrix Q, 
            out Matrix H)
        {
            X = InitStateX(posX, posY);
            P = InitCovarianceP(posAccuracy);
           // Q = InitSystemNoiseQ(50, 50, 5, 5, 3, 0.4);
            H = InitObservationH();
        }
        public static Matrix InitStateX(double posX, double posY)
        {
            var result = Matrix.Create(6, 1);
            result[0, 0] = posX;
            result[1, 0] = posY;
            return result;
        }

        public static Matrix InitCovarianceP(params double[] values)
        {
            return Matrix.Diagonal(values);
        }

        public static Matrix InitCovarianceP(double posAccuracy)
        {
            return InitCovarianceP(posAccuracy, posAccuracy, 0, 0, 0, 0);
        }

        public static Matrix InitSystemNoiseQ(params double[] values)
        {
            return Matrix.Diagonal(values);
        }

        public static Matrix InitTransitionF(double dt)
        {
            double dt_2 = dt * dt;
            return Matrix.Create(new double[,] {
                { 1, 0, dt,  0, 0.5 * dt_2,          0 },
                { 0, 1,  0, dt,          0, 0.5 * dt_2 },
                { 0, 0,  1,  0,         dt,          0 },
                { 0, 0,  0,  1,          0,         dt },
                { 0, 0,  0,  0,          1,          0 },
                { 0, 0,  0,  0,          0,          1 },
            });
        }

        public static Matrix InitObservationH()
        {
            return Matrix.Create(new double[,] {
                { 1, 0, 0, 0, 0, 0 },
                { 0, 1, 0, 0, 0, 0 },
            });
        }

        public static Matrix InitMeasurementNoiseR(double posAccuracy)
        {
            return Matrix.Diagonal(posAccuracy, posAccuracy);
        }

        //public KalmanFunctions()
        //{
            // https://stackoverflow.com/questions/47210512/using-pykalman-on-raw-acceleration-data-to-calculate-position
            // https://stackoverflow.com/questions/55947643/kalman-filter-prediction-in-case-of-missing-measurement-and-only-positions-are-k
        //}

        public static (Matrix X, Matrix P) Prediction(Matrix X, Matrix P, Matrix Q, Matrix F)
        {
            X = F * X;
            P = F * P * F.T() + Q;
            return (X, P);
        }

        public static (Matrix X, Matrix P) Update(Matrix X, Matrix P, Matrix y, Matrix R, Matrix H)
        {
            Matrix Inn = y - H * X;
            Matrix S = H * P * H.T() + R;
            Matrix K = P * H.T() * S.Inv();

            X = X + K * Inn;
            P = P - K * H * P;

            return (X, P);
        }
    }
}