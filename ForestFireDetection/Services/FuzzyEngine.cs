namespace ForestFireDetection.Services
{
    /// <summary>
    /// Mamdani fuzzy inference engine for computing FireScore from temperature, humidity, and smoke.
    /// </summary>
    public class FuzzyEngine
    {
        // Triangular membership function helper
        private double TriangularMembership(double x, double a, double b, double c)
        {
            if (x <= a || x >= c) return 0.0;
            if (x == b) return 1.0;
            if (x < b)  // rising edge
                return (x - a) / (b - a);
            else        // falling edge
                return (c - x) / (c - b);
        }

        /// <summary>
        /// Compute FireScore (0–100) using Mamdani fuzzy logic.
        /// </summary>
        public double ComputeFireScore(double temp, double hum, double smoke)
        {
            // Fuzzify temperature
            double tLow = TriangularMembership(temp, 0, 0, 30);
            double tMed = TriangularMembership(temp, 0, 30, 60);
            double tHigh = TriangularMembership(temp, 30, 60, 70);
            // Fuzzify humidity
            double hDry = TriangularMembership(hum, 0, 0, 50);
            double hNorm = TriangularMembership(hum, 0, 50, 100);
            double hHumid = TriangularMembership(hum, 50, 100, 100);
            // Fuzzify smoke
            double sLow = TriangularMembership(smoke, 0, 0, 35);
            double sMed = TriangularMembership(smoke, 0, 35, 70);
            double sHigh = TriangularMembership(smoke, 35, 70, 70);

            // Initialize output fuzzy set degrees
            double fireLow = 0.0, fireMed = 0.0, fireHigh = 0.0;

            // Evaluate rules (min for AND, then accumulate with max)
            double strength;

            // Example Rule 1: IF Temp=Low AND Humidity=Humid AND Smoke=Low THEN Fire=Low
            strength = Math.Min(Math.Min(tLow, hHumid), sLow);
            fireLow = Math.Max(fireLow, strength);

            // Rule 2: IF Temp=Low AND Humidity=Humid AND Smoke=Med THEN Fire=Low
            strength = Math.Min(Math.Min(tLow, hHumid), sMed);
            fireLow = Math.Max(fireLow, strength);

            // Rule 3: IF Temp=Low AND Humidity=Norm AND Smoke=Low THEN Fire=Low
            strength = Math.Min(Math.Min(tLow, hNorm), sLow);
            fireLow = Math.Max(fireLow, strength);

            // Rule 4: IF Temp=Med AND Humidity=Humid AND Smoke=Low THEN Fire=Low
            strength = Math.Min(Math.Min(tMed, hHumid), sLow);
            fireLow = Math.Max(fireLow, strength);

            // Rule 5: IF Temp=Low AND Humidity=Dry AND Smoke=Low THEN Fire=Low
            strength = Math.Min(Math.Min(tLow, hDry), sLow);
            fireLow = Math.Max(fireLow, strength);

            // Rule 6: IF Temp=Med AND Humidity=Norm AND Smoke=Med THEN Fire=Med
            strength = Math.Min(Math.Min(tMed, hNorm), sMed);
            fireMed = Math.Max(fireMed, strength);

            // Rule 7: IF Temp=High AND Humidity=Humid AND Smoke=Low THEN Fire=Med
            strength = Math.Min(Math.Min(tHigh, hHumid), sLow);
            fireMed = Math.Max(fireMed, strength);

            // Rule 8: IF Temp=High AND Humidity=Norm AND Smoke=Med THEN Fire=High
            strength = Math.Min(Math.Min(tHigh, hNorm), sMed);
            fireHigh = Math.Max(fireHigh, strength);

            // Rule 9: IF Temp=High AND Humidity=Norm AND Smoke=High THEN Fire=High
            strength = Math.Min(Math.Min(tHigh, hNorm), sHigh);
            fireHigh = Math.Max(fireHigh, strength);

            // Rule 10: IF Temp=Low AND Humidity=Dry AND Smoke=High THEN Fire=High
            strength = Math.Min(Math.Min(tLow, hDry), sHigh);
            fireHigh = Math.Max(fireHigh, strength);

            // Rule 11: IF Temp=Med AND Humidity=Dry AND Smoke=High THEN Fire=High
            strength = Math.Min(Math.Min(tMed, hDry), sHigh);
            fireHigh = Math.Max(fireHigh, strength);

            // Rule 12: IF Temp=High AND Humidity=Dry AND Smoke=Med THEN Fire=High
            strength = Math.Min(Math.Min(tHigh, hDry), sMed);
            fireHigh = Math.Max(fireHigh, strength);

            // Rule 13: IF Temp=High AND Humidity=Dry AND Smoke=High THEN Fire=High
            strength = Math.Min(Math.Min(tHigh, hDry), sHigh);
            fireHigh = Math.Max(fireHigh, strength);


            // (Additional rules can be added similarly...)

            // DEFUZZIFICATION: Centroid of aggregated output
            double step = 1.0;  // integration step (0–100 output range)
            double numerator = 0.0, denominator = 0.0;
            for (double z = 0; z <= 100; z += step)
            {
                // Output membership for each fuzzy FireScore set at level z
                double mLowOut = TriangularMembership(z, 0, 0, 50);
                double mMedOut = TriangularMembership(z, 0, 50, 100);
                double mHighOut = TriangularMembership(z, 50, 100, 100);
                // Aggregate via fuzzy OR (max of clipped values)
                double mAgg = Math.Max(
                                Math.Max(Math.Min(mLowOut, fireLow),
                                         Math.Min(mMedOut, fireMed)),
                                Math.Min(mHighOut, fireHigh)
                             );
                numerator += z * mAgg;
                denominator += mAgg;
            }
            // Avoid divide-by-zero
            return (denominator > 0) ? (numerator / denominator) : 0.0;
        }
    }
}
