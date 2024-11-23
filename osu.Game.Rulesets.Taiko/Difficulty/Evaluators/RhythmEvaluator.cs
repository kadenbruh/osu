// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public class RhythmEvaluator
    {
        /// <summary>
        /// Multiplier for a given denominator term.
        /// </summary>
        private static double termPenalty(double ratio, int denominator, double power, double multiplier)
        {
            return -multiplier * Math.Pow(Math.Cos(denominator * Math.PI * ratio), power);
        }

        /// <summary>
        /// Gives a bonus for target ratio using a bell-shaped function.
        /// </summary>
        private static double targetedBonus(double ratio, double targetRatio, double width, double multiplier)
        {
            return multiplier * Math.Exp(Math.E * -(Math.Pow(ratio - targetRatio, 2) / Math.Pow(width, 2)));
        }

        private static double ratioDifficulty(double ratio, int terms = 8)
        {
            // Sum of n = 8 terms of periodic penalty.
            double difficulty = 0;

            for (int i = 1; i <= terms; ++i)
            {
                difficulty += termPenalty(ratio, i, 2, 1);
            }

            difficulty += terms;

            // Give bonus to near-1 ratios
            difficulty += targetedBonus(ratio, 1, 0.5, 1);

            // Penalize ratios that are VERY near 1
            difficulty -= targetedBonus(ratio, 1, 0.3, 1);

            return difficulty / Math.Sqrt(8);
        }

        private static bool isConsistentPattern(EvenHitObjects evenHitObjects, double threshold = 0.1)
        {
            // Collect the last 4 intervals (current, last 3 previous).
            List<double?> intervals = new List<double?>
            {
                evenHitObjects.HitObjectInterval,
                evenHitObjects.Previous?.HitObjectInterval,
                evenHitObjects.Previous?.Previous?.HitObjectInterval,
                evenHitObjects.Previous?.Previous?.Previous?.HitObjectInterval
            };

            // Remove null intervals (if any patterns are too short).
            intervals.RemoveAll(interval => interval == null);

            // If there are fewer than 4 valid intervals, skip the consistency check.
            if (intervals.Count < 4)
                return false;

            // Compare all pairs of intervals in the window.
            for (int i = 0; i < intervals.Count; i++)
            {
                for (int j = i + 1; j < intervals.Count; j++)
                {
                    double ratio = intervals[i]!.Value / intervals[j]!.Value;
                    if (Math.Abs(1 - ratio) <= threshold) // If any two intervals are similar, return true.
                        return true;
                }
            }

            // No similar intervals were found.
            return false;
        }

        private static double evaluateDifficultyOf(EvenHitObjects evenHitObjects, double hitWindow)
        {
            double intervalDifficulty = ratioDifficulty(evenHitObjects.HitObjectIntervalRatio);

            // Penalize patterns that can be played with the same interval as the previous pattern.
            double? previousInterval = evenHitObjects.Previous?.HitObjectInterval;

            if (previousInterval != null && evenHitObjects.Children.Count > 1)
            {
                double expectedDurationFromPrevious = (double)previousInterval * evenHitObjects.Children.Count;
                double durationDifference = Math.Abs(evenHitObjects.Duration - expectedDurationFromPrevious);

                intervalDifficulty *= DifficultyCalculationUtils.Logistic(
                    durationDifference / hitWindow,
                    midpointOffset: 0.5,
                    multiplier: 1.5,
                    maxValue: 1);
            }

            // Penalize consistent patterns.
            if (isConsistentPattern(evenHitObjects))
            {
                intervalDifficulty *= 0.4; // Nerf by 30% for consistent patterns.
            }

            // Penalize patterns that can be hit within a single hit window.
            intervalDifficulty *= DifficultyCalculationUtils.Logistic(
                evenHitObjects.Duration / hitWindow,
                midpointOffset: 0.5,
                multiplier: 1,
                maxValue: 1);

            return intervalDifficulty;
        }

        private static double evaluateDifficultyOf(EvenPatterns evenPatterns)
        {
            return ratioDifficulty(evenPatterns.IntervalRatio);
        }

        public static double EvaluateDifficultyOf(DifficultyHitObject hitObject, double hitWindow)
        {
            TaikoDifficultyHitObjectRhythm rhythm = ((TaikoDifficultyHitObject)hitObject).Rhythm;
            double difficulty = 0.0d;

            if (rhythm.EvenHitObjects?.FirstHitObject == hitObject) // Difficulty for EvenHitObjects
                difficulty += evaluateDifficultyOf(rhythm.EvenHitObjects, hitWindow);

            if (rhythm.EvenPatterns?.FirstHitObject == hitObject) // Difficulty for EvenPatterns
                difficulty += evaluateDifficultyOf(rhythm.EvenPatterns) * rhythm.Difficulty;

            return difficulty;
        }
    }
}
