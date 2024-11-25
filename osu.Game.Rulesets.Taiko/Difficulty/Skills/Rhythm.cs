﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    /// <summary>
    /// Calculates the rhythm coefficient of taiko difficulty.
    /// </summary>
    public class Rhythm : StrainDecaySkill
    {
        protected override double SkillMultiplier => 1.6;
        protected override double StrainDecayBase => 0.4;

        private readonly double greatHitWindow;

        public Rhythm(Mod[] mods, double greatHitWindow)
            : base(mods)
        {
            this.greatHitWindow = greatHitWindow;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            double difficulty = RhythmEvaluator.EvaluateDifficultyOf(current, greatHitWindow);

            if (current.DeltaTime > 400)
                difficulty *= 0.5;

            return difficulty;
        }
    }
}
