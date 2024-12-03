// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class ReadingEvaluator
    {
        private const double low_sv_multiplier = 4.0;
        private const double low_velocity_threshold = 150;

        /// <summary>
        /// This one removes the high sv bonus so I can see the effects of easy mod.
        /// Calculates the influence of higher and lower slider velocities on hitobject difficulty.
        /// The higher bonus is determined based on the EffectiveBPM, shifting within a defined range
        /// between the upper and lower boundaries to reflect how increased slider velocity impacts difficulty.
        /// The lower bonus is based on EffectiveBPM compared to a low velocity threshold.
        /// </summary>
        /// <param name="noteObject">The hit object to evaluate.</param>
        /// <returns>The reading difficulty value for the given hit object.</returns>
        public static double EvaluateDifficultyOf(TaikoDifficultyHitObject noteObject)
        {
            double effectiveBPM = noteObject.EffectiveBPM;

            double low_sv_bonus = 0;
            if (effectiveBPM < low_velocity_threshold)
            {
                low_sv_bonus = low_sv_multiplier * DifficultyCalculationUtils.Logistic(effectiveBPM, low_velocity_threshold, 1 / 100);
            }
            return low_sv_bonus;

        }
        /// <summary>
        /// Calculates the object density based on the DeltaTime, EffectiveBPM, and CurrentSliderVelocity.
        /// </summary>
        /// <param name="noteObject">The current noteObject to evaluate.</param>
        /// <returns>The calculated object density.</returns>
        public static double CalculateObjectDensity(TaikoDifficultyHitObject noteObject)
        {
            double objectDensity = 50 * DifficultyCalculationUtils.Logistic(noteObject.DeltaTime, 200, 1.0 / 300);

            return 1 - DifficultyCalculationUtils.Logistic(noteObject.EffectiveBPM, objectDensity, 1.0 / 240);
        }
    }
}
