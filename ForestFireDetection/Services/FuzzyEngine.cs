    namespace ForestFireDetection.Services
{
    /// <summary>
    /// Mamdani fuzzy inference engine for computing FireScore from temperature, humidity, and smoke.
    /// </summary>
    public class FuzzyEngine
    {
        private static double TriangularMembership(double x, double a, double b, double c)
        {
            if (x <= a || x >= c) return 0.0;
            if (x == b) return 1.0;
            return x < b
                ? (x - a) / (b - a)
                : (c - x) / (c - b);
        }

        /// <summary>
        /// Compute FireScore (0–100) using Mamdani fuzzy logic.
        /// </summary>
        public double ComputeFireScore(double temp, double hum, double smoke)
        {
            double tLow = TriangularMembership(temp, 0, 0, 30);
            double tMed = TriangularMembership(temp, 0, 30, 60);
            double tHigh = TriangularMembership(temp, 30, 60, 70);

            double hDry = TriangularMembership(hum, 0, 0, 50);
            double hNorm = TriangularMembership(hum, 0, 50, 100);
            double hHumid = TriangularMembership(hum, 50, 100, 100);

            double sLow = TriangularMembership(smoke, 0, 0, 35);
            double sMed = TriangularMembership(smoke, 0, 35, 70);
            double sHigh = TriangularMembership(smoke, 35, 70, 70);

            double fireLow = 0.0, fireMed = 0.0, fireHigh = 0.0;

            // Rules → Fire=Low
            fireLow = Max(fireLow,
                Min(tLow, hHumid, sLow),
                Min(tLow, hHumid, sMed),
                Min(tLow, hNorm, sLow),
                Min(tMed, hHumid, sLow),
                Min(tLow, hDry, sLow));

            // Rules → Fire=Med
            fireMed = Max(fireMed,
                Min(tMed, hNorm, sMed),
                Min(tHigh, hHumid, sLow));

            // Rules → Fire=High
            fireHigh = Max(fireHigh,
                Min(tHigh, hNorm, sMed),
                Min(tHigh, hNorm, sHigh),
                Min(tLow, hDry, sHigh),
                Min(tMed, hDry, sHigh),
                Min(tHigh, hDry, sMed),
                Min(tHigh, hDry, sHigh));

            // Centroid defuzzification
            double numerator = 0.0, denominator = 0.0;
            for (double z = 0; z <= 100; z += 1.0)
            {
                double mLowOut = TriangularMembership(z, 0, 0, 50);
                double mMedOut = TriangularMembership(z, 0, 50, 100);
                double mHighOut = TriangularMembership(z, 50, 100, 100);

                double mAgg = Math.Max(
                    Math.Max(Math.Min(mLowOut, fireLow), Math.Min(mMedOut, fireMed)),
                    Math.Min(mHighOut, fireHigh));

                numerator += z * mAgg;
                denominator += mAgg;
            }

            return denominator > 0 ? numerator / denominator : 0.0;
        }

        private static double Min(params double[] values) => values.Min();
        private static double Max(params double[] values) => values.Max();
    }
}
