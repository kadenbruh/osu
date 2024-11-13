using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class PatternEvaluator
    {
        private static readonly double BinSize = 10.0; // For interval binning in entropy
        private static readonly double SlopeThreshold = 0.05;
        private static readonly double EntropyScalingFactor = 0.3;
        private static readonly double CVScalingFactor = 0.2;
        private static readonly double VarianceScalingDivisor = 500000.0;
        private static readonly double EntropyThreshold = 2.0;
        private static readonly double CVThreshold = 0.2;
        private static readonly double LinearTrendReductionFactor = 0.9;
        private static readonly double RepetitivePatternAdjustment = 0.95;
        private static readonly double RepetitivePatternTolerance = 20.0; // ms

        private static double getCappedInterval(double interval, double maxIntervalCap = 500)
        {
            // Caps the interval to prevent excessively high values
            return interval > maxIntervalCap ? maxIntervalCap : interval;
        }

        /// <summary>
        /// Evaluates the difficulty of the current hit object based on the rhythmic complexity within the pattern.
        /// </summary>
        /// <param name="current">The current <see cref="TaikoDifficultyHitObject"/> to evaluate.</param>
        /// <returns>The calculated difficulty value.</returns>
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            if (!(current is TaikoDifficultyHitObject taikoCurrent) || !(taikoCurrent.Previous(0) is TaikoDifficultyHitObject previousTaiko))
                return 0.0;

            List<TaikoDifficultyHitObject> patternGroup = gatherPatternGroup(taikoCurrent, 10); // Gather up to 10 previous notes.

            // Calculate pattern complexity factor
            double patternComplexityFactor = calculatePatternComplexityFactor(patternGroup);

            // Incorporate rhythm difficulty scaling, removing the two baseline rhythmic values.
            double rhythmFactor = 1.0;
            bool isBaseline = taikoCurrent.Rhythm.Difficulty == 0.3 || taikoCurrent.Rhythm.Difficulty == 0.5;

            if (!isBaseline)
            {
                // Apply scaling for rhythm difficulty.
                rhythmFactor = 1 + ((taikoCurrent.Rhythm.Difficulty - 0.5) * 1.25);
                rhythmFactor = Math.Clamp(rhythmFactor, 0.5, 3.0); // Prevent extreme scaling.

                // Further adjust rhythmFactor based on repetitive patterns
                if (hasRepetitivePatterns(patternGroup, patternLength: 2, RepetitivePatternTolerance))
                {
                    rhythmFactor *= RepetitivePatternAdjustment; // Example adjustment factor
                }
            }

            // Combine the pattern complexity factor with the rhythm factor.
            double difficulty = patternComplexityFactor * rhythmFactor;

            return difficulty;
        }

        /// <summary>
        /// Gathers a group of hit objects forming the current rhythmic pattern sequence.
        /// </summary>
        private static List<TaikoDifficultyHitObject> gatherPatternGroup(TaikoDifficultyHitObject start, int maxNotes)
        {
            List<TaikoDifficultyHitObject> group = new List<TaikoDifficultyHitObject> { start };

            for (int i = 1; i < maxNotes; i++)
            {
                if (start.Previous(i) is TaikoDifficultyHitObject previous)
                    group.Add(previous);
                else
                    break;
            }

            return group;
        }

        /// <summary>
        /// Calculates the complexity factor for a given group of hit objects based on rhythmic intervals.
        /// </summary>
        private static double calculatePatternComplexityFactor(List<TaikoDifficultyHitObject> patternGroup)
        {
            if (patternGroup.Count <= 1)
                return 1.0;

            double averageInterval = calculateAverageInterval(patternGroup);
            if (averageInterval == 0)
                return 1.0;

            double variance = calculateIntervalVariance(patternGroup, averageInterval);
            double cv = calculateCoefficientOfVariation(variance, averageInterval);

            // Calculate entropy with binning
            double entropy = calculateRhythmEntropy(patternGroup, BinSize);

            // Calculate intervals list
            var intervals = patternGroup
                            .Zip(patternGroup.Skip(1), (a, b) => getCappedInterval(a.StartTime - b.StartTime))
                            .ToList();

            // Detect linear trend with adjusted sensitivity
            bool linearTrend = hasLinearTrend(intervals, SlopeThreshold);

            // Base the complexity factor on variance and coefficient of variation
            double complexityFactor = 1.0;

            // Incorporate Variance
            complexityFactor += variance / VarianceScalingDivisor;

            // Incorporate Coefficient of Variation
            if (cv > CVThreshold)
            {
                complexityFactor += cv * CVScalingFactor;
            }

            // Incorporate Entropy
            if (entropy > EntropyThreshold)
            {
                complexityFactor += entropy * EntropyScalingFactor;
            }

            // If a linear trend is detected, reduce complexity factor
            if (linearTrend)
            {
                complexityFactor *= LinearTrendReductionFactor;
            }

            return complexityFactor;
        }

        private static double calculateAverageInterval(List<TaikoDifficultyHitObject> group)
        {
            if (group.Count <= 1)
                return 0.0;

            double totalInterval = 0.0;

            for (int i = 1; i < group.Count; i++)
            {
                double interval = group[i - 1].StartTime - group[i].StartTime;
                interval = getCappedInterval(interval); // Apply the cap.
                totalInterval += interval;
            }

            return totalInterval / (group.Count - 1);
        }

        private static double calculateIntervalVariance(List<TaikoDifficultyHitObject> group, double averageInterval)
        {
            if (group.Count <= 1)
                return 0.0;

            double varianceSum = 0.0;

            for (int i = 1; i < group.Count; i++)
            {
                double interval = group[i - 1].StartTime - group[i].StartTime;
                interval = getCappedInterval(interval);

                double deviation = interval - averageInterval;
                varianceSum += deviation * deviation;
            }

            return varianceSum / (group.Count - 1);
        }

        private static double calculateCoefficientOfVariation(double variance, double averageInterval)
        {
            if (averageInterval == 0)
                return 0.0;

            double standardDeviation = Math.Sqrt(variance);
            return standardDeviation / averageInterval;
        }

        private static double calculateRhythmEntropy(List<TaikoDifficultyHitObject> patternGroup, double binSize)
        {
            if (patternGroup.Count <= 1)
                return 0.0;

            // Calculate intervals
            var intervals = patternGroup
                            .Zip(patternGroup.Skip(1), (a, b) => getCappedInterval(a.StartTime - b.StartTime))
                            .ToList();

            // Bin intervals
            var binnedIntervals = intervals.Select(i => Math.Round(i / binSize) * binSize).ToList();

            // Group intervals and calculate probabilities
            var intervalGroups = binnedIntervals.GroupBy(i => i).Select(g => new { Interval = g.Key, Count = g.Count() }).ToList();
            int total = intervals.Count;

            return intervalGroups.Select(group => (double)group.Count / total).Aggregate(0.0, (current, probability) => current - probability * Math.Log(probability, 2));
        }

        private static bool hasLinearTrend(List<double> intervals, double slopeThreshold)
        {
            if (intervals.Count <= 2)
                return false;

            // Perform a simple linear regression
            int n = intervals.Count;
            double sumX = 0.0, sumY = 0.0, sumXY = 0.0, sumX2 = 0.0;

            for (int i = 0; i < n; i++)
            {
                double x = i;
                double y = intervals[i];
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            double denominator = (n * sumX2 - sumX * sumX);
            if (denominator == 0)
                return false;

            double slope = (n * sumXY - sumX * sumY) / denominator;

            // Check if slope exceeds the threshold
            return Math.Abs(slope) > slopeThreshold;
        }

        private static bool hasRepetitivePatterns(List<TaikoDifficultyHitObject> patternGroup, int patternLength = 2, double tolerance = 20.0)
        {
            if (patternGroup.Count < patternLength * 2)
                return false;

            // Extract the last 'patternLength' intervals
            var lastIntervals = patternGroup
                                .Take(patternLength)
                                .Zip(patternGroup.Skip(1), (a, b) => getCappedInterval(a.StartTime - b.StartTime))
                                .ToList();

            // Extract the preceding 'patternLength' intervals
            var precedingIntervals = patternGroup
                                     .Skip(patternLength)
                                     .Take(patternLength)
                                     .Zip(patternGroup.Skip(patternLength + 1), (a, b) => getCappedInterval(a.StartTime - b.StartTime))
                                     .ToList();

            if (lastIntervals.Count != precedingIntervals.Count)
                return false;

            for (int i = 0; i < lastIntervals.Count; i++)
            {
                if (Math.Abs(lastIntervals[i] - precedingIntervals[i]) > tolerance)
                    return false;
            }

            return true;
        }
    }
}
