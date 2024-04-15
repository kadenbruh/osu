// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Scoring;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoPerformanceCalculator : PerformanceCalculator
    {
        // The estimate ratio of pattern difficulty to peak difficulty, assuming all skills having an even contribution.
        private double patternRatio;

        private int countGreat;
        private int countOk;
        private int countMeh;
        private int countMiss;
        private double? estimatedUr;

        private double effectiveMissCount;

        public TaikoPerformanceCalculator()
            : base(new TaikoRuleset())
        {
        }

        protected override PerformanceAttributes CreatePerformanceAttributes(ScoreInfo score, DifficultyAttributes attributes)
        {
            var taikoAttributes = (TaikoDifficultyAttributes)attributes;

            countGreat = score.Statistics.GetValueOrDefault(HitResult.Great);
            countOk = score.Statistics.GetValueOrDefault(HitResult.Ok);
            countMeh = score.Statistics.GetValueOrDefault(HitResult.Meh);
            countMiss = score.Statistics.GetValueOrDefault(HitResult.Miss);
            estimatedUr = computeDeviationUpperBound(taikoAttributes) * 10;

            // The effectiveMissCount is calculated by gaining a ratio for totalSuccessfulHits and increasing the miss penalty for shorter object counts lower than 1000.
            if (totalSuccessfulHits > 0)
                effectiveMissCount = Math.Max(1.0, 1000.0 / totalSuccessfulHits) * countMiss;

            double multiplier = 1.13;

            // StaminaMultiplier, ColourMultiplier, PatternMultiplier
            patternRatio = Math.Sqrt(Math.Pow(0.35, 2) + Math.Pow(0.275, 2)
                / Math.Sqrt(Math.Pow(0.35, 2) + Math.Pow(0.275, 2) + Math.Pow(0.6, 2)));

            double patternDifficulty = taikoAttributes.PatternDifficulty;
            double readingMultiplier = MathEvaluator.Sigmoid(patternDifficulty / taikoAttributes.PeakDifficulty / patternRatio,
                0.55, 0.4, 0.5, 1.0);

            if (score.Mods.Any(m => m is ModHidden))
                multiplier *= 1 + 0.075 * readingMultiplier;

            if (score.Mods.Any(m => m is ModEasy))
                multiplier *= 0.975;

            double difficultyValue = computeDifficultyValue(score, taikoAttributes, readingMultiplier);
            double accuracyValue = computeAccuracyValue(taikoAttributes);
            double totalValue =
                Math.Pow(
                    Math.Pow(difficultyValue, 1.1) +
                    Math.Pow(accuracyValue, 1.1), 1.0 / 1.1
                ) * multiplier;

            return new TaikoPerformanceAttributes
            {
                Difficulty = difficultyValue,
                Accuracy = accuracyValue,
                EffectiveMissCount = effectiveMissCount,
                EstimatedUr = estimatedUr,
                Total = totalValue
            };
        }

        private double computeDifficultyValue(ScoreInfo score, DifficultyAttributes attributes, double readingMultiplier)
        {
            double difficultyValue = Math.Pow(5 * Math.Max(1.0, attributes.StarRating / 0.115) - 4.0, 2.25) / 1150.0;

            double lengthBonus = 1 + 0.1 * Math.Min(1.0, totalHits / 1500.0);
            difficultyValue *= lengthBonus;

            difficultyValue *= Math.Pow(0.986, effectiveMissCount);

            if (score.Mods.Any(m => m is ModEasy))
                difficultyValue *= 1 - 0.015 * readingMultiplier;

            if (score.Mods.Any(m => m is ModHidden))
                difficultyValue *= 1 + 0.050 * readingMultiplier;

            if (score.Mods.Any(m => m is ModHardRock))
                difficultyValue *= 1 + 0.010 * readingMultiplier;

            if (score.Mods.Any(m => m is ModFlashlight<TaikoHitObject>))
                difficultyValue *= (1 + 0.050 * readingMultiplier) * lengthBonus;

            if (estimatedUr == null)
                return 0;

            return difficultyValue * Math.Pow(SpecialFunctions.Erf(400 / (Math.Sqrt(2) * estimatedUr.Value)), 2.0);
        }

        private double computeAccuracyValue(TaikoDifficultyAttributes attributes)
        {
            if (attributes.GreatHitWindow <= 0 || estimatedUr == null)
                return 0;

            double accuracyValue = Math.Pow(70 / estimatedUr.Value, 1.1) * Math.Pow(attributes.StarRating, 0.4) * 100.0;

            double lengthBonus = Math.Min(1.15, Math.Pow(totalHits / 1500.0, 0.3));
            accuracyValue *= lengthBonus;

            return accuracyValue;
        }

        /// <summary>
        /// Computes an upper bound on the player's tap deviation based on the OD, number of circles and sliders,
        /// and the hit judgements, assuming the player's mean hit error is 0. The estimation is consistent in that
        /// two SS scores on the same map with the same settings will always return the same deviation.
        /// </summary>
        private double? computeDeviationUpperBound(TaikoDifficultyAttributes attributes)
        {
            if (totalSuccessfulHits == 0 || attributes.GreatHitWindow <= 0)
                return null;

            double h300 = attributes.GreatHitWindow;
            double h100 = attributes.OkHitWindow;

            const double z = 2.32634787404; // 99% critical value for the normal distribution (one-tailed).

            // The upper bound on deviation, calculated with the ratio of 300s to objects, and the great hit window.
            double? calcDeviationGreatWindow()
            {
                if (countGreat == 0) return null;

                double n = totalHits;

                // Proportion of greats hit.
                double p = countGreat / n;

                // We can be 99% confident that p is at least this value.
                double pLowerBound = (n * p + z * z / 2) / (n + z * z) - z / (n + z * z) * Math.Sqrt(n * p * (1 - p) + z * z / 4);

                // We can be 99% confident that the deviation is not higher than:
                return h300 / (Math.Sqrt(2) * SpecialFunctions.ErfInv(pLowerBound));
            }

            // The upper bound on deviation, calculated with the ratio of 300s + 100s to objects, and the good hit window.
            // This will return a lower value than the first method when the number of 100s is high, but the miss count is low.
            double? calcDeviationGoodWindow()
            {
                if (totalSuccessfulHits == 0) return null;

                double n = totalHits;

                // Proportion of greats + goods hit.
                double p = totalSuccessfulHits / n;

                // We can be 99% confident that p is at least this value.
                double pLowerBound = (n * p + z * z / 2) / (n + z * z) - z / (n + z * z) * Math.Sqrt(n * p * (1 - p) + z * z / 4);

                // We can be 99% confident that the deviation is not higher than:
                return h100 / (Math.Sqrt(2) * SpecialFunctions.ErfInv(pLowerBound));
            }

            if (calcDeviationGreatWindow() is null)
                return calcDeviationGoodWindow();

            return Math.Min(calcDeviationGreatWindow()!.Value, calcDeviationGoodWindow()!.Value);
        }

        private int totalHits => countGreat + countOk + countMeh + countMiss;

        private int totalSuccessfulHits => countGreat + countOk + countMeh;

        // Utilises a numerical approximation to extend the computation range of ln(erfc(x)).
        private double logErfcApprox(double x) => x <= 5
            ? Math.Log(SpecialFunctions.Erfc(x))
            : -Math.Pow(x, 2) - Math.Log(x * Math.Sqrt(Math.PI)); // https://www.desmos.com/calculator/kdbxwxgf01

        // Subtracts the base value of two logs, circumventing log rules that typically complicate subtraction of non-logarithmic values.
    }
}
