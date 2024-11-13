// Copyright (c) ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    /// <summary>
    /// Calculates the rhythm coefficient of taiko difficulty.
    /// </summary>
    public class Rhythm : StrainDecaySkill
    {
        protected override double SkillMultiplier => 10;
        protected override double StrainDecayBase => 0;

        /// <summary>
        /// The note-based decay for rhythm strain.
        /// </summary>
        /// <remarks>
        /// <see cref="StrainDecayBase"/> is not used here, as it's note-based rather than time-based.
        /// </remarks>
        private const double strain_decay = 0.96;

        /// <summary>
        /// Maximum number of entries in <see cref="rhythmHistory"/>.
        /// </summary>
        private const int rhythm_history_max_length = 8;

        /// <summary>
        /// Contains the last <see cref="rhythm_history_max_length"/> rhythms.
        /// </summary>
        private readonly LimitedCapacityQueue<TaikoDifficultyHitObject> rhythmHistory =
            new LimitedCapacityQueue<TaikoDifficultyHitObject>(rhythm_history_max_length);

        /// <summary>
        /// Contains the rolling rhythm strain.
        /// Used to apply per-note decay.
        /// </summary>
        private double currentStrain;

        /// <summary>
        /// Number of notes since the last rhythm change has taken place.
        /// </summary>
        private int notesSinceRhythmChange;

        /// <summary>
        /// Tracks unique rhythm patterns within the current history window.
        /// </summary>
        private readonly HashSet<double> uniqueRhythms = new HashSet<double>();

        // Parameters for change frequency penalty
        private const int max_changes_without_penalty = 6; // Maximum allowed changes before applying penalty

        public Rhythm(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            // Drum rolls and swells are exempt.
            if (!(current.BaseObject is Hit))
            {
                resetRhythmAndStrain();
                return 0.0;
            }

            // Apply strain decay
            currentStrain *= strain_decay;

            TaikoDifficultyHitObject hitObject = (TaikoDifficultyHitObject)current;
            notesSinceRhythmChange += 1;

            // Rhythm difficulty zero (due to rhythm not changing) => no rhythm strain.
            if (hitObject.Rhythm.Difficulty == 0.0)
            {
                return 0.0;
            }

            double objectStrain = hitObject.Rhythm.Difficulty;

            // Check for unique rhythm patterns
            bool isNewRhythm = uniqueRhythms.Add(hitObject.Rhythm.Difficulty);

            // Enqueue the current hit object into rhythmHistory
            rhythmHistory.Enqueue(hitObject);

            // Apply penalties for repetitive rhythms
            objectStrain *= repetitionPenalties(isNewRhythm);

            // Apply pattern length penalty
            objectStrain *= patternLengthPenalty(notesSinceRhythmChange);

            // Apply speed penalty
            objectStrain *= speedPenalty(hitObject.DeltaTime);

            // Reset notes since rhythm change after applying penalties
            notesSinceRhythmChange = 0;

            // Incorporate rhythm entropy using EntropyCalculator
            double entropy = EntropyCalculator.CalculateEntropy(
                rhythmHistory.Select(o => o.Rhythm.Difficulty),
                0.1
            );

            // Corrected strain adjustment formula
            objectStrain *= (1.0 + entropy * 2);

            // Apply change frequency penalty if necessary
            objectStrain *= applyChangeFrequencyPenalty();

            currentStrain += objectStrain;
            return currentStrain;
        }

        /// <summary>
        /// Applies a penalty if rhythm changes are too frequent within the defined window.
        /// </summary>
        /// <returns>A multiplier to adjust the strain.</returns>
        private double applyChangeFrequencyPenalty()
        {
            // Calculate the number of rhythm changes in the current window
            int changeCount = 0;
            double previousRhythm = -1.0; // Initialize with an invalid rhythm

            // Iterate in reverse to check recent rhythms
            foreach (var hitObject in rhythmHistory.Reverse())
            {
                if (previousRhythm == -1.0)
                {
                    previousRhythm = hitObject.Rhythm.Difficulty;
                    continue;
                }

                if (Math.Abs(hitObject.Rhythm.Difficulty - previousRhythm) > 0.05)
                {
                    changeCount++;
                    previousRhythm = hitObject.Rhythm.Difficulty;
                }

                if (changeCount >= max_changes_without_penalty)
                    break;
            }

            if (changeCount > max_changes_without_penalty)
            {
                // Apply a penalty to reduce strain due to excessive rhythm changes
                return 0.90;
            }

            return 1.0; // No penalty
        }

        /// <summary>
        /// Returns a penalty multiplier based on whether the rhythm is new or repetitive.
        /// </summary>
        /// <param name="isNewRhythm">Indicates whether the rhythm is a new pattern.</param>
        /// <returns>The calculated penalty.</returns>
        private double repetitionPenalties(bool isNewRhythm)
        {
            if (!isNewRhythm)
            {
                // Apply a penalty for repeating rhythms to reduce strain
                return 0.95;
            }

            return 1.0; // No penalty
        }

        /// <summary>
        /// Calculates a penalty based on the number of notes since the last rhythm change.
        /// Both rare and frequent rhythm changes are penalised.
        /// </summary>
        /// <param name="patternLength">Number of notes since the last rhythm change.</param>
        /// <returns>The calculated penalty.</returns>
        private static double patternLengthPenalty(int patternLength)
        {
            double shortPatternPenalty = Math.Min(0.15 * patternLength, 1.0);
            double longPatternPenalty = Math.Clamp(2.5 - 0.15 * patternLength, 0.0, 1.0);
            return Math.Min(shortPatternPenalty, longPatternPenalty);
        }

        /// <summary>
        /// Calculates a penalty for objects that do not require alternating hands.
        /// </summary>
        /// <param name="deltaTime">Time (in milliseconds) since the last hit object.</param>
        /// <returns>The calculated penalty.</returns>
        private double speedPenalty(double deltaTime)
        {
            if (deltaTime < 80) return 1.0;
            if (deltaTime < 210) return Math.Max(0.0, 1.4 - 0.005 * deltaTime);

            resetRhythmAndStrain();
            return 0.0;
        }

        /// <summary>
        /// Resets the rolling strain value, notes since last rhythm change, and unique rhythms.
        /// </summary>
        private void resetRhythmAndStrain()
        {
            currentStrain = 0.0;
            notesSinceRhythmChange = 0;
            uniqueRhythms.Clear(); // Reset unique rhythms on reset
        }
    }
}
