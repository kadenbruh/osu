
using System;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class ReadingEvaluator
    {
        private const double high_sv_multiplier = 1.0;

        /// <summary>
        /// Calculates the influence of higher slider velocities on hitobject difficulty.
        /// The bonus is determined based on the EffectiveBPM, shifting within a defined range
        /// between the upper and lower boundaries to reflect how increased slider velocity impacts difficulty.
        /// </summary>
        /// <param name="noteObject">The hit object to evaluate.</param>
        /// <returns>The reading difficulty value for the given hit object.</returns>
        public static double EvaluateDifficultyOf(TaikoDifficultyHitObject noteObject)
        {
            double effectiveBPM = noteObject.EffectiveBPM;

            const double velocity_max = 640;
            const double velocity_min = 480;

            const double center = (velocity_max + velocity_min) / 2;
            const double range = velocity_max - velocity_min;

            return high_sv_multiplier * sigmoid(effectiveBPM, center, range);
        }

        /// <summary>
        /// Calculates the object density based on the DeltaTime, EffectiveBPM, and CurrentSliderVelocity.
        /// </summary>
        /// <param name = "noteObject">The current noteObject to evaluate.</param>
        /// <returns>The calculated object density.</returns>
        public static double CalculateObjectDensity(TaikoDifficultyHitObject noteObject)
        {
            return 150 - (150 - 50)
                * sigmoid(noteObject.DeltaTime, 200, 300);
        }

        /// <summary>
        /// Calculates a smooth transition using a sigmoid function.
        /// </summary>
        /// <param name="value">The input value.</param>
        /// <param name="center">The midpoint of the curve where the output transitions most rapidly.</param>
        /// <param name="range">Determines how steep or gradual the curve is around the center.</param>
        /// <returns>The calculated sigmoid value.</returns>
        private static double sigmoid(double value, double center, double range)
        {
            range /= 10;
            return 1 / (1 + Math.Exp(-(value - center) / range));
        }
    }
}
