// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
using osu.Game.Rulesets.Taiko.Difficulty.Skills;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Scoring;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 1.35;

        private double greatHitWindow;

        public override int Version => 20220902;

        public TaikoDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);
            greatHitWindow = hitWindows.WindowFor(HitResult.Great) / clockRate;

            return new Skill[]
            {
                new Peaks(mods, greatHitWindow)
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

            TaikoDifficultyPreprocessor.Process(Beatmap, difficultyHitObjects, noteObjects);

            return difficultyHitObjects;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new TaikoDifficultyAttributes { Mods = mods };

            var combined = (Peaks)skills[0];

            double colourRating = combined.ColourDifficultyValue * difficulty_multiplier;
            double rhythmRating = combined.RhythmDifficultyValue * difficulty_multiplier;
            double staminaRating = combined.StaminaDifficultyValue * difficulty_multiplier;
            double readingRating = combined.ReadingDifficultyValue * difficulty_multiplier;
            Console.WriteLine(readingRating);

            double combinedRating = combined.DifficultyValue() * difficulty_multiplier;
            double starRating = rescale(combinedRating * 1.4);

            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            return new TaikoDifficultyAttributes
            {
                StarRating = starRating,
                Mods = mods,
                StaminaDifficulty = staminaRating,
                RhythmDifficulty = rhythmRating,
                ColourDifficulty = colourRating,
                ReadingDifficulty = readingRating,
                PeakDifficulty = combinedRating,
                GreatHitWindow = greatHitWindow,
                MaxCombo = beatmap.HitObjects.Count(h => h is Hit),
            };
        }

        /// <summary>
        /// Applies a re-scaling aimed to increase spread of difficulty values at the higher-end.
        /// </summary>
        /// <param name="difficulty">The raw difficulty produced by peaks before re-scaling.</param>
        private double rescale(double difficulty)
        {
            if (difficulty < 0) return difficulty;

            return 10.43 * Math.Log(difficulty / 8 + 1);
        }
    }
}
