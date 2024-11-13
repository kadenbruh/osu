using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class PatternEvaluator
    {
        private static readonly double BinSize = 10.0;
        private static readonly double SlopeThreshold = 0.05;
        private static readonly double EntropyScalingFactor = 0.3;
        private static readonly double CVScalingFactor = 0.2;
        private static readonly double VarianceScalingDivisor = 500000.0;
        private static readonly double EntropyThreshold = 2.0;
        private static readonly double CVThreshold = 0.2;
        private static readonly double LinearTrendReductionFactor = 0.9;
        private static readonly double RepetitivePatternAdjustment = 0.95;
        private static readonly double RepetitivePatternTolerance = 20.0;

        private static double getCappedInterval(double interval, double maxIntervalCap = 500)
        {
            return interval > maxIntervalCap ? maxIntervalCap : interval;
        }

        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            if (!(current is TaikoDifficultyHitObject taikoCurrent) || !(taikoCurrent.Previous(0) is TaikoDifficultyHitObject previousTaiko))
                return 0.0;

            List<TaikoDifficultyHitObject> patternGroup = gatherPatternGroup(taikoCurrent, 10);

            double patternComplexityFactor = calculatePatternComplexityFactor(patternGroup);

            double rhythmFactor = 1.0;
            bool isBaseline = taikoCurrent.Rhythm.Difficulty == 0.3 || taikoCurrent.Rhythm.Difficulty == 0.5;

            if (!isBaseline)
            {
                rhythmFactor = 1 + ((taikoCurrent.Rhythm.Difficulty - 0.5) * 1.25);
                rhythmFactor = Math.Clamp(rhythmFactor, 0.5, 3.0);

                if (hasRepetitivePatterns(patternGroup, patternLength: 2, RepetitivePatternTolerance))
                {
                    rhythmFactor *= RepetitivePatternAdjustment;
                }
            }

            double difficulty = patternComplexityFactor * rhythmFactor;

            return difficulty;
        }

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

        private static double calculatePatternComplexityFactor(List<TaikoDifficultyHitObject> patternGroup)
        {
            if (patternGroup.Count <= 1)
                return 1.0;

            var intervals = patternGroup
                            .Zip(patternGroup.Skip(1), (a, b) => getCappedInterval(a.StartTime - b.StartTime))
                            .ToList();

            double averageInterval = EntropyCalculator.CalculateAverage(intervals);
            double variance = EntropyCalculator.CalculateVariance(intervals, averageInterval);
            double cv = EntropyCalculator.CalculateCoefficientOfVariation(intervals);
            double entropy = EntropyCalculator.CalculateEntropy(intervals, BinSize);

            bool linearTrend = hasLinearTrend(intervals, SlopeThreshold);

            double complexityFactor = 1.0;

            complexityFactor += variance / VarianceScalingDivisor;

            if (cv > CVThreshold)
            {
                complexityFactor += cv * CVScalingFactor;
            }

            if (entropy > EntropyThreshold)
            {
                complexityFactor += entropy * EntropyScalingFactor;
            }

            if (linearTrend)
            {
                complexityFactor *= LinearTrendReductionFactor;
            }

            return complexityFactor;
        }

        private static bool hasLinearTrend(List<double> intervals, double slopeThreshold)
        {
            if (intervals.Count <= 2)
                return false;

            int n = intervals.Count;
            double sumX = 0.0, sumY = 0.0, sumXy = 0.0, sumX2 = 0.0;

            for (int i = 0; i < n; i++)
            {
                double x = i;
                double y = intervals[i];
                sumX += x;
                sumY += y;
                sumXy += x * y;
                sumX2 += x * x;
            }

            double denominator = (n * sumX2 - sumX * sumX);
            if (denominator == 0)
                return false;

            double slope = (n * sumXy - sumX * sumY) / denominator;

            return Math.Abs(slope) > slopeThreshold;
        }

        private static bool hasRepetitivePatterns(List<TaikoDifficultyHitObject> patternGroup, int patternLength = 2, double tolerance = 20.0)
        {
            if (patternGroup.Count < patternLength * 2)
                return false;

            var lastIntervals = patternGroup
                                .Take(patternLength)
                                .Zip(patternGroup.Skip(1), (a, b) => getCappedInterval(a.StartTime - b.StartTime))
                                .ToList();

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
