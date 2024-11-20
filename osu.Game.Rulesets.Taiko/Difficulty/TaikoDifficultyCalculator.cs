// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm;
using osu.Game.Rulesets.Taiko.Difficulty.Skills;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Scoring;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 0.084375;
        private const double rhythm_skill_multiplier = 0.07 * difficulty_multiplier;
        private const double colour_skill_multiplier = 0.425 * difficulty_multiplier;
        private const double stamina_skill_multiplier = 0.375 * difficulty_multiplier;

        private double simpleRhythmPenalty;
        private double simpleColourPenalty;

        public override int Version => 20241007;

        public TaikoDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            HitWindows hitWindows = new HitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            return new Skill[]
            {
                new Rhythm(mods, hitWindows.WindowFor(HitResult.Great) / clockRate),
                new Colour(mods),
                new Stamina(mods, false),
                new Stamina(mods, true)
            };
        }

        protected override Mod[] DifficultyAdjustmentMods => new Mod[]
        {
            new TaikoModDoubleTime(),
            new TaikoModHalfTime(),
            new TaikoModEasy(),
            new TaikoModHardRock(),
        };

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, double clockRate)
        {
            HitWindows hitWindows = new HitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            List<DifficultyHitObject> difficultyHitObjects = new List<DifficultyHitObject>();
            List<TaikoDifficultyHitObject> centreObjects = new List<TaikoDifficultyHitObject>();
            List<TaikoDifficultyHitObject> rimObjects = new List<TaikoDifficultyHitObject>();
            List<TaikoDifficultyHitObject> noteObjects = new List<TaikoDifficultyHitObject>();

            for (int i = 2; i < beatmap.HitObjects.Count; i++)
            {
                difficultyHitObjects.Add(
                    new TaikoDifficultyHitObject(
                        beatmap.HitObjects[i], beatmap.HitObjects[i - 1], beatmap.HitObjects[i - 2], clockRate,
                        difficultyHitObjects, centreObjects, rimObjects, noteObjects, difficultyHitObjects.Count)
                );
            }

            TaikoColourDifficultyPreprocessor.ProcessAndAssign(difficultyHitObjects);
            EvenPatterns.GroupPatterns(EvenHitObjects.GroupHitObjects(noteObjects));

            return difficultyHitObjects;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new TaikoDifficultyAttributes { Mods = mods };

            Colour colour = (Colour)skills.First(x => x is Colour);
            Rhythm rhythm = (Rhythm)skills.First(x => x is Rhythm);
            Stamina stamina = (Stamina)skills.First(x => x is Stamina);
            Stamina singleColourStamina = (Stamina)skills.Last(x => x is Stamina);

            double colourRating = colour.DifficultyValue() * colour_skill_multiplier;
            double rhythmRating = rhythm.DifficultyValue() * rhythm_skill_multiplier;
            double staminaRating = stamina.DifficultyValue() * stamina_skill_multiplier;
            double monoStaminaRating = singleColourStamina.DifficultyValue() * stamina_skill_multiplier;
            double monoStaminaFactor = staminaRating == 0 ? 1 : Math.Pow(monoStaminaRating / staminaRating, 5);

            const double rhythm_threshold = 2.5; // Where rhythmRating = threshold, 0 penalty applies.
            // Check if DoubleTime mod is active
            bool isDoubleTime = mods.Any(h => h is TaikoModDoubleTime);
            // Set the colour threshold based on whether DoubleTime is active
            double colourThreshold = isDoubleTime ? 6.0 : 4.0;

            const double rhythm_upper_bound = rhythm_threshold * 2; // Upper bound of the penalty for rhythm.
            double colourUpperBound = colourThreshold * 2;

            // Only apply the rhythm penalty when rhythmRating is below the threshold
            simpleRhythmPenalty = rhythmRating <= rhythm_threshold
                ? Math.Log(rhythm_threshold / rhythmRating) * Math.Min(rhythm_upper_bound, Math.Log(Math.Max(1, colourRating - rhythm_upper_bound)) + rhythm_upper_bound)
                : 0;

            // Only apply the colour penalty when colourRating is below the threshold
            simpleColourPenalty = colourRating <= colourThreshold
                ? Math.Log(colourThreshold / colourRating) * Math.Min(colourUpperBound, Math.Log(Math.Max(1, rhythmRating - colourUpperBound)) + colourUpperBound)
                : 0;

            // Ensure values never decrease when increasing difficulty
            simpleRhythmPenalty = Math.Max(0, simpleRhythmPenalty);
            simpleColourPenalty = Math.Max(0, simpleColourPenalty);

            double combinedRating = combinedDifficultyValue(rhythm, colour, stamina);
            double starRating = rescale(combinedRating * 1.6);

            // TODO: This is temporary measure as we don't detect abuse of multiple-input playstyles of converts within the current system.
            if (beatmap.BeatmapInfo.Ruleset.OnlineID == 0)
            {
                starRating *= 0.925;
                // For maps with low colour variance and high stamina requirement, multiple inputs are more likely to be abused.
                if (colourRating < 2 && staminaRating > 8)
                    starRating *= 0.80;
            }

            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            TaikoDifficultyAttributes attributes = new TaikoDifficultyAttributes
            {
                StarRating = starRating,
                Mods = mods,
                StaminaDifficulty = staminaRating,
                MonoStaminaFactor = monoStaminaFactor,
                SimpleColour = simpleColourPenalty,
                SimpleRhythm = simpleRhythmPenalty,
                RhythmDifficulty = rhythmRating,
                ColourDifficulty = colourRating,
                PeakDifficulty = combinedRating,
                GreatHitWindow = hitWindows.WindowFor(HitResult.Great) / clockRate,
                OkHitWindow = hitWindows.WindowFor(HitResult.Ok) / clockRate,
                MaxCombo = beatmap.GetMaxCombo(),
            };

            return attributes;
        }

        /// <summary>
        /// Applies a final re-scaling of the star rating.
        /// </summary>
        /// <param name="sr">The raw star rating value before re-scaling.</param>
        private double rescale(double sr)
        {
            if (sr < 0) return sr;

            return 10.43 * Math.Log(sr / 8 + 1);
        }

        /// <summary>
        /// Returns the combined star rating of the beatmap, calculated using peak strains from all sections of the map.
        /// </summary>
        /// <remarks>
        /// For each section, the peak strains of all separate skills are combined into a single peak strain for the section.
        /// The resulting partial rating of the beatmap is a weighted sum of the combined peaks (higher peaks are weighted more).
        /// </remarks>
        private double combinedDifficultyValue(Rhythm rhythm, Colour colour, Stamina stamina)
        {
            List<double> peaks = new List<double>();

            var colourPeaks = colour.GetCurrentStrainPeaks().ToList();
            var rhythmPeaks = rhythm.GetCurrentStrainPeaks().ToList();
            var staminaPeaks = stamina.GetCurrentStrainPeaks().ToList();

            for (int i = 0; i < colourPeaks.Count; i++)
            {
                double baseColourPeak = colourPeaks[i] * 0.035859375;
                double colourPeak = baseColourPeak * Math.Exp(-simpleRhythmPenalty / 14);
                double baserhythmPeak = rhythmPeaks[i] * 0.01190625;
                double rhythmPeak = baserhythmPeak * Math.Exp(-simpleColourPenalty * 2);
                double staminaPeak = staminaPeaks[i] * 0.031640625;

                double peak = norm(1.5, colourPeak, staminaPeak);
                peak = norm(2, peak, rhythmPeak);

                // Sections with 0 strain are excluded to avoid worst-case time complexity of the following sort (e.g. /b/2351871).
                // These sections will not contribute to the difficulty.
                if (peak > 0)
                    peaks.Add(peak);
            }

            double difficulty = 0;
            double weight = 1;

            foreach (double strain in peaks.OrderDescending())
            {
                difficulty += strain * weight;
                weight *= 0.9;
            }

            return difficulty;
        }

        /// <summary>
        /// Returns the <i>p</i>-norm of an <i>n</i>-dimensional vector.
        /// </summary>
        /// <param name="p">The value of <i>p</i> to calculate the norm for.</param>
        /// <param name="values">The coefficients of the vector.</param>
        private double norm(double p, params double[] values) => Math.Pow(values.Sum(x => Math.Pow(x, p)), 1 / p);
    }
}
