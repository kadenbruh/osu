using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class PatternEvaluator
    {
        private static double getCappedInterval(double interval, double maxIntervalCap = 500.0)
        {
            // Breaks return a high interval, thus we cap it to prevent this behaviour.
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

            // Simplified calculation for the pattern complexity factor based on variance.
            double patternComplexityFactor = calculatePatternComplexityFactor(patternGroup);

            // Incorporate rhythm difficulty scaling, removing the two baseline rhythmic values.
            double rhythmFactor = 1.0;
            bool isBaseline = taikoCurrent.Rhythm.Difficulty == 0.3 || taikoCurrent.Rhythm.Difficulty == 0.5;

            if (!isBaseline)
            {
                // Apply scaling for rhythm difficulty.
                rhythmFactor = 1 + ((taikoCurrent.Rhythm.Difficulty - 0.5) * 1.25);
                rhythmFactor = Math.Clamp(rhythmFactor, 0.5, 3.0); // Prevent extreme scaling.
            }

            Console.WriteLine($"Rhythm Factor: {rhythmFactor}"); // TODO

            // Combine the pattern complexity factor with the rhythm factor.
            double difficulty = patternComplexityFactor * rhythmFactor;

            Console.WriteLine($"Final Difficulty: {difficulty}"); // TODO

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

            // Base the complexity factor directly on variance.
            double complexityFactor = 1.0 + (variance / 1000000.0); // Adjust divisor for scaling.
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
    }
}
