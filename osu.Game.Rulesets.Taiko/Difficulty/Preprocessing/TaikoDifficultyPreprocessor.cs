// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Reading;
using osu.Game.Beatmaps;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing
{
    public class TaikoDifficultyPreprocessor
    {
        /// <summary>
        /// Does preprocessing on a list of <see cref="TaikoDifficultyHitObject"/>s.
        /// </summary>
        public static List<DifficultyHitObject> Process(
            IBeatmap beatmap,
            List<DifficultyHitObject> difficultyHitObjects,
            List<TaikoDifficultyHitObject> noteObjects)
        {
            TaikoColourDifficultyPreprocessor.ProcessAndAssign(difficultyHitObjects);
            // Passing noteObjects as a parameter will only assign reading data to regular notes and finisher notes.
            TaikoReadingDifficultyPreprocessor.ProcessAndAssign(beatmap, noteObjects);

            return difficultyHitObjects;
        }
    }
}
