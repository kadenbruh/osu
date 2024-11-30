﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
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
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Reading;
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
        private const double reading_skill_multiplier = 0.10 * difficulty_multiplier;
        private const double colour_skill_multiplier = 0.425 * difficulty_multiplier;
        private const double stamina_skill_multiplier = 0.375 * difficulty_multiplier;

        private double simpleRhythmPenalty = 1;
        private double simpleColourPenalty = 1;

        private double colourDifficultStrains;
        private double rhythmDifficultStrains;
        private double staminaDifficultStrains;

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
                new Reading(mods),
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
            var hitWindows = new HitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            var difficultyHitObjects = new List<DifficultyHitObject>();
            var centreObjects = new List<TaikoDifficultyHitObject>();
            var rimObjects = new List<TaikoDifficultyHitObject>();
            var noteObjects = new List<TaikoDifficultyHitObject>();
            EffectiveBPMPreprocessor bpmLoader = new EffectiveBPMPreprocessor(beatmap, noteObjects);

            // Generate TaikoDifficultyHitObjects from the beatmap's hit objects.
            for (int i = 2; i < beatmap.HitObjects.Count; i++)
            {
                difficultyHitObjects.Add(new TaikoDifficultyHitObject(
                    beatmap.HitObjects[i],
                    beatmap.HitObjects[i - 1],
                    beatmap.HitObjects[i - 2],
                    clockRate,
                    difficultyHitObjects,
                    centreObjects,
                    rimObjects,
                    noteObjects,
                    difficultyHitObjects.Count
                ));
            }

            var groupedHitObjects = EvenHitObjects.GroupHitObjects(noteObjects);

            TaikoColourDifficultyPreprocessor.ProcessAndAssign(difficultyHitObjects);
            EvenPatterns.GroupPatterns(groupedHitObjects);
            bpmLoader.ProcessEffectiveBPM(beatmap.ControlPointInfo, clockRate);

            return difficultyHitObjects;
        }

        /// <summary>
        /// Calculates the combined penalty based on the relationship between rhythm and colour ratings.
        /// Lower skill values are penalized more heavily relative to predefined thresholds and their
        /// interaction with the opposing skill rating.
        /// </summary>
        private double simplePatternPenalty(double rhythmRating, double colourRating, double clockRate)
        {
            const double rhythm_threshold = 2.5;
            const double rhythm_upper_bound = rhythm_threshold * 2;

            double colourThreshold = 1.25 * clockRate;

            simpleRhythmPenalty = patternRating(rhythmRating, rhythm_threshold, rhythm_upper_bound, colourRating);
            simpleRhythmPenalty = Math.Max(0, simpleRhythmPenalty);

            // We count difficult stamina strains to ensure that even if there's no rhythm, very heavy stamina maps still give their respective difficulty.
            if (staminaDifficultStrains > 1250)
            {
                double scale = Math.Min(1, 1250 / Math.Min(2000, staminaDifficultStrains)); // Capped out at 2000 difficult strains to prevent overly long maps abusing this bonus.
                simpleRhythmPenalty *= 0.85 * scale;
            }

            if (colourRating < colourThreshold)
            {
                double colourPenaltyFactor = (colourThreshold - colourRating) / colourThreshold;
                simpleColourPenalty = 0.50 * colourPenaltyFactor;
            }

            return simpleRhythmPenalty;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new TaikoDifficultyAttributes { Mods = mods };

            Colour colour = (Colour)skills.First(x => x is Colour);
            Rhythm rhythm = (Rhythm)skills.First(x => x is Rhythm);
            Reading reading = (Reading)skills.First(x => x is Reading);
            Stamina stamina = (Stamina)skills.First(x => x is Stamina);
            Stamina singleColourStamina = (Stamina)skills.Last(x => x is Stamina);

            double colourRating = colour.DifficultyValue() * colour_skill_multiplier;
            double rhythmRating = rhythm.DifficultyValue() * rhythm_skill_multiplier;
            double readingRating = reading.DifficultyValue() * reading_skill_multiplier;
            double objectDensity = reading.ObjectDensity;
            double staminaRating = stamina.DifficultyValue() * stamina_skill_multiplier;
            double monoStaminaRating = singleColourStamina.DifficultyValue() * stamina_skill_multiplier;
            double monoStaminaFactor = staminaRating == 0 ? 1 : Math.Pow(monoStaminaRating / staminaRating, 5);

            colourDifficultStrains = colour.CountTopWeightedStrains();
            rhythmDifficultStrains = rhythm.CountTopWeightedStrains();
            staminaDifficultStrains = stamina.CountTopWeightedStrains() * Math.Min(clockRate, 1.25);

            double patternPenalty = simplePatternPenalty(rhythmRating, colourRating, clockRate);

            double combinedRating = combinedDifficultyValue(rhythm, reading, colour, stamina);
            double starRating = rescale(combinedRating * 1.8);

            // TODO: This is temporary measure as we don't detect abuse of multiple-input playstyles of converts within the current system.
            if (beatmap.BeatmapInfo.Ruleset.OnlineID == 0)
            {
                starRating *= 0.80;
                // For maps with low colour variance and high stamina requirement, multiple inputs are more likely to be abused.
                if (colourRating < 2 && staminaRating > 8)
                    starRating *= 0.725;
            }

            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            TaikoDifficultyAttributes attributes = new TaikoDifficultyAttributes
            {
                StarRating = starRating,
                Mods = mods,
                StaminaDifficulty = staminaRating,
                MonoStaminaFactor = monoStaminaFactor,
                SimplePattern = patternPenalty,
                RhythmDifficulty = rhythmRating * 8,
                ReadingDifficulty = readingRating * 1.5,
                ObjectDensity = objectDensity,
                ColourDifficulty = colourRating,
                StaminaTopStrains = staminaDifficultStrains,
                RhythmTopStrains = rhythmDifficultStrains,
                ColourTopStrains = colourDifficultStrains,
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
        private double combinedDifficultyValue(Rhythm rhythm, Reading reading, Colour colour, Stamina stamina)
        {
            List<double> peaks = new List<double>();

            var colourPeaks = colour.GetCurrentStrainPeaks().ToList();
            var rhythmPeaks = rhythm.GetCurrentStrainPeaks().ToList();
            var readingPeaks = reading.GetCurrentStrainPeaks().ToList();
            var staminaPeaks = stamina.GetCurrentStrainPeaks().ToList();

            for (int i = 0; i < colourPeaks.Count; i++)
            {
                // Peaks uses separate constants due to strain pertaining differently to display values.
                double baseColourPeak = colourPeaks[i] * 0.035859375;
                double colourPeak = baseColourPeak * Math.Exp(-simpleRhythmPenalty / 14);
                double rhythmPeak = rhythmPeaks[i] * 0.03790625 * simpleColourPenalty;
                double staminaPeak = staminaPeaks[i] * 0.031640625;
                double readingPeak = readingPeaks[i] * reading_skill_multiplier;

                double peak = norm(1.5, colourPeak, staminaPeak);
                peak = norm(2, peak, rhythmPeak, readingPeak);

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
        /// Calculates the penalty for skills based on their relationship making up a pattern.
        /// Penalising ratings where patterns have a major difference in value.
        /// </summary>
        private double patternRating(double rating, double threshold, double upperBound, double otherRating)
        {
            if (rating > threshold)
                return 0;

            // To prevent against skewed values, we treat 0 and 0.01 as the same to prevent infinity values.
            rating = Math.Max(0.01, rating);
            otherRating = Math.Max(0.01, otherRating);

            // Penalize based on logarithmic difference from the skill-based threshold, scaled by the influence of the other rating
            return Math.Log(threshold / rating) * Math.Min(upperBound, Math.Log(Math.Max(1, otherRating - upperBound)) + upperBound);
        }

        /// <summary>
        /// Returns the <i>p</i>-norm of an <i>n</i>-dimensional vector.
        /// </summary>
        /// <param name="p">The value of <i>p</i> to calculate the norm for.</param>
        /// <param name="values">The coefficients of the vector.</param>
        private double norm(double p, params double[] values) => Math.Pow(values.Sum(x => Math.Pow(x, p)), 1 / p);
    }
}
