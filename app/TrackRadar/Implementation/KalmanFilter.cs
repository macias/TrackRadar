namespace TrackRadar.Implementation
{
    internal sealed class KalmanFilter
    {
        public KalmanFilter()
        {
            // https://stackoverflow.com/questions/47210512/using-pykalman-on-raw-acceleration-data-to-calculate-position
            // https://stackoverflow.com/questions/55947643/kalman-filter-prediction-in-case-of-missing-measurement-and-only-positions-are-k
        }

        private (Matrix X, Matrix P) prediction(Matrix X, Matrix P, Matrix Q, Matrix F)
        {
            X = F * X;
            P = F * P * F.T() + Q;
            return (X, P);
        }

        private (Matrix X, Matrix P) update(Matrix X, Matrix P, Matrix y, Matrix R, Matrix H)
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