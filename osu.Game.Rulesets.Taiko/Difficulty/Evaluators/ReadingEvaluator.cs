using System;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class ReadingEvaluator
    {
        private const double high_sv_multiplier = 1.0;

        /// <summary>
        /// Evaluates the reading difficulty and returns the calculated difficulty value.
        /// </summary>
        /// <param name="noteObject">The hit object to evaluate.</param>
        /// <returns>The reading difficulty value for the given hit object.</returns>
        public static double EvaluateDifficultyOf(TaikoDifficultyHitObject noteObject)
        {
            if (noteObject.BaseObject is Swell || noteObject.BaseObject is DrumRoll)
            {
                return 1;
            }

            double sliderVelocityBonus = calculateHighVelocityBonus(noteObject.EffectiveBPM);

            // Only return a difficulty value based on high slider velocity bonus.
            return high_sv_multiplier * sliderVelocityBonus;
        }

        /// <summary>
        /// Calculates and returns the object density for a given hit object.
        /// </summary>
        /// <param name="noteObject">The hit object to evaluate for density.</param>
        /// <returns>The object density value for the given hit object.</returns>
        public static double GetObjectDensity(TaikoDifficultyHitObject noteObject)
        {
            return calculateObjectDensity(noteObject.DeltaTime, noteObject.EffectiveBPM, noteObject.CurrentSliderVelocity);
        }

        /// <summary>
        /// Calculates the influence of higher slider velocities on beatmap difficulty.
        /// The bonus is determined based on the EffectiveBPM, shifting within a defined range
        /// between the upper and lower boundaries to reflect how increased slider velocity impacts difficulty.
        /// </summary>
        private static double calculateHighVelocityBonus(double effectiveBPM)
        {
            // The maximum and minimum center value for the impact of EffectiveBPM.
            const double velocity_max = 640;
            const double velocity_min = 480;

            const double center = (velocity_max + velocity_min) / 2;
            const double range = velocity_max - velocity_min;

            return sigmoid(effectiveBPM, center, range);
        }

        /// <summary>
        /// Calculates the object density based on the DeltaTime, EffectiveBPM, and CurrentSliderVelocity.
        /// </summary>
        /// <param name="deltaTime">The time between hit objects.</param>
        /// <param name="effectiveBPM">The effective BPM of the hit object.</param>
        /// <param name="currentSliderVelocity">The current slider velocity of the hit object.</param>
        /// <returns>The calculated object density.</returns>
        private static double calculateObjectDensity(double deltaTime, double effectiveBPM, double currentSliderVelocity)
        {
            // The maximum and minimum center value for density.
            const double density_max = 300;
            const double density_min = 50;

            const double center = 200;
            const double range = 2000;

            // Adjusts the penalty for low SV based on object density.
            return density_max - (density_max - density_min) *
                sigmoid(deltaTime * currentSliderVelocity, center, range);
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
