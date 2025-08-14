using UnityEngine;

namespace BetterWeapons
{
    static partial class Calculator
    {
        public static class NormalDistribution
        {
            private const int IterationLimit = 10;
            private const float Tiny = 1e-7f;

            public static float Random(VarianceBounds bounds, int step = -1)
            {
                // Precompute mean once
                float mean = (bounds.Max + bounds.Min) * 0.5f;
                int iterations = 0;
                float value;

                do
                {
                    // Avoid log(0) by clamping lower bound
                    float u1 = Mathf.Max(UnityEngine.Random.value, Tiny);
                    float u2 = UnityEngine.Random.value;

                    // Box–Muller transform for N(0,1)
                    float z = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);

                    value = z * bounds.StandardDeviation + mean;

                    // Optional step rounding (supports non-int step sizes)
                    if (step > 0)
                    {
                        float s = step;
                        value = Mathf.Round(value / s) * s;
                    }

                    iterations++;
                }
                while ((value < bounds.Min || value > bounds.Max) && iterations < IterationLimit);

                // Fallback to mean if we fail to generate a value in range
                if (iterations == IterationLimit)
                    value = mean;

                return value;
            }
        }
    }
}
