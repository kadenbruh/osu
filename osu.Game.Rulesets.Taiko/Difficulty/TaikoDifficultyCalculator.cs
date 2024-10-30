// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour;
using osu.Game.Rulesets.Taiko.Difficulty.Skills;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Scoring;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoDifficultyCalculator : DifficultyCalculator
    {
        public override int Version => 20241007;

        public TaikoDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            return new Skill[]
            {
                new Rhythm(mods),
                new Colour(mods),
                new Stamina(mods)
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
            List<DifficultyHitObject> difficultyHitObjects = new List<DifficultyHitObject>();
            List<TaikoDifficultyHitObject> centreObjects = new List<TaikoDifficultyHitObject>();
            List<TaikoDifficultyHitObject> rimObjects = new List<TaikoDifficultyHitObject>();
            List<TaikoDifficultyHitObject> noteObjects = new List<TaikoDifficultyHitObject>();

            for (int i = 2; i < beatmap.HitObjects.Count; i++)
            {
                difficultyHitObjects.Add(
                    new TaikoDifficultyHitObject(
                        beatmap.HitObjects[i], beatmap.HitObjects[i - 1], beatmap.HitObjects[i - 2], clockRate, difficultyHitObjects,
                        centreObjects, rimObjects, noteObjects, difficultyHitObjects.Count)
                );
            }

            TaikoColourDifficultyPreprocessor.ProcessAndAssign(difficultyHitObjects);

            return difficultyHitObjects;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new TaikoDifficultyAttributes { Mods = mods };

            var combined = (Peaks)skills[0];

            // These have to be read after combined.DifficultyValue() is set
            double colourRating = combined.ColourStat;
            double rhythmRating = combined.RhythmStat;
            double staminaRating = combined.StaminaStat;

            double combinedRating = logScale(combined.DifficultyValue());
            double starRating = spreadScaling(combinedRating);

            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            TaikoDifficultyAttributes attributes = new TaikoDifficultyAttributes
            {
                StarRating = starRating,
                Mods = mods,
                StaminaDifficulty = staminaRating,
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
        /// <param name="combinedRating">The raw peaks skill rating before re-scaling.</param>
        private double logScale(double combinedRating)
        {
            if (combinedRating < 0) return combinedRating;

            return 10.43 * Math.Log(combinedRating / 8 + 1);
        }

        private double sineCurve(double combinedRating)
        {
            return 11.5 * Math.Sinh(1.0 / 16.0 * combinedRating);
        }

        /// <summary>
        /// Adjusts the star-rating curve in a non-linear way.
        /// </summary>
        /// <param name="combinedRating"></param>
        /// <returns></returns>
        private double spreadScaling(double combinedRating)
        {
            return Math.Floor((1.0 / 2.6) * (sineCurve(2 * combinedRating) + sineCurve(combinedRating)) * 100.0) / 100.0;
        }
    }
}
