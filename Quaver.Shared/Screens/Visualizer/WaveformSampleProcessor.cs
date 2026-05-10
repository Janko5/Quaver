using System;
using Microsoft.Xna.Framework;

namespace Quaver.Shared.Screens.Visualizer
{
    internal sealed class WaveformSampleProcessor
    {
        private const float NoiseGate = 0.02f;
        private const float Gain = 1.35f;
        private const float Attack = 0.18f;
        private const float Release = 0.08f;

        private readonly float[] smoothedMin;
        private readonly float[] smoothedMax;
        private readonly float[] edgeFade;

        public int Columns { get; }

        public WaveformSampleProcessor(int columns)
        {
            Columns = Math.Max(1, columns);
            smoothedMin = new float[Columns];
            smoothedMax = new float[Columns];
            edgeFade = new float[Columns];

            for (var i = 0; i < Columns; i++)
            {
                if (Columns <= 2)
                {
                    edgeFade[i] = 1f;
                    continue;
                }

                // Keep only subtle edge shaping. Previous strong fade created
                // the "flat top with dropped sides" look.
                var distanceFromEdge = Math.Min(i, Columns - 1 - i);
                var fadeWidth = Math.Max(1, Columns / 40);
                var t = MathHelper.Clamp(distanceFromEdge / (float)fadeWidth, 0f, 1f);
                var smooth = t * t * (3f - 2f * t);
                edgeFade[i] = 0.90f + smooth * 0.10f;
            }
        }

        public void Clear()
        {
            Array.Clear(smoothedMin, 0, smoothedMin.Length);
            Array.Clear(smoothedMax, 0, smoothedMax.Length);
        }

        public void Process(float[] samples, int sampleCount, float[] outputMin, float[] outputMax)
        {
            if (outputMin.Length < Columns || outputMax.Length < Columns)
                throw new ArgumentException("Output buffers must have at least Columns elements.");

            if (samples == null || sampleCount <= 0)
            {
                DecayToSilence(outputMin, outputMax);
                return;
            }

            sampleCount = Math.Min(sampleCount, samples.Length);

            for (var column = 0; column < Columns; column++)
            {
                var start = column * sampleCount / Columns;
                var end = (column + 1) * sampleCount / Columns;

                var min = 0f;
                var max = 0f;
                var sumSquares = 0f;
                var count = 0;

                for (var i = start; i < end; i++)
                {
                    var value = samples[i];
                    if (float.IsNaN(value) || float.IsInfinity(value))
                        continue;

                    if (value < min)
                        min = value;
                    if (value > max)
                        max = value;

                    sumSquares += value * value;
                    count++;
                }

                // Use envelope energy instead of raw min/max extremes.
                var rms = count > 0 ? (float)Math.Sqrt(sumSquares / count) : 0f;
                var peak = Math.Max(Math.Abs(min), Math.Abs(max));
                var energy = rms * 0.75f + peak * 0.25f;
                var shaped = ShapeAmplitude(energy) * edgeFade[column];

                smoothedMin[column] = Smooth(smoothedMin[column], shaped);
                smoothedMax[column] = Smooth(smoothedMax[column], shaped);

                outputMin[column] = smoothedMin[column];
                outputMax[column] = smoothedMax[column];
            }
        }

        private void DecayToSilence(float[] outputMin, float[] outputMax)
        {
            for (var i = 0; i < Columns; i++)
            {
                smoothedMin[i] = Smooth(smoothedMin[i], 0f);
                smoothedMax[i] = Smooth(smoothedMax[i], 0f);
                outputMin[i] = smoothedMin[i];
                outputMax[i] = smoothedMax[i];
            }
        }

        private static float ShapeAmplitude(float value)
        {
            var sign = Math.Sign(value);
            var magnitude = Math.Abs(value);

            if (magnitude < NoiseGate)
                return 0f;

            magnitude = MathHelper.Clamp((magnitude - NoiseGate) * Gain, 0f, 1f);
            magnitude = (float)Math.Sqrt(magnitude);

            return sign * magnitude;
        }

        private static float Smooth(float current, float target)
        {
            var factor = Math.Abs(target) > Math.Abs(current) ? Attack : Release;
            return current + (target - current) * factor;
        }
    }
}
