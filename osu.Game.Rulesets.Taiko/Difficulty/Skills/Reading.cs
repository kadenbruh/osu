// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Reading;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    public class Reading : StrainDecaySkill
    {
        protected override double SkillMultiplier => 1;
        protected override double StrainDecayBase => 0.4;

        private const double high_sv_multiplier = 1;
        private const double low_sv_multiplier = 1;

        public Reading(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            TaikoDifficultyHitObject taikoDifficultyHitObject = (TaikoDifficultyHitObject)current;
            return svBonus(taikoDifficultyHitObject);
        }

        private double svBonus(TaikoDifficultyHitObject current)
        {
            const double high_sv_upper_bound = 640;
            const double high_sv_lower_bound = 480;
            const double high_sv_center = (high_sv_upper_bound + high_sv_lower_bound) / 2;
            const double high_sv_width = high_sv_upper_bound - high_sv_lower_bound;

            // Center and width of delta time range for low sv calculation. We use delta time to determine density. The
            // lower the delta time (higher density), the higher the low sv bonus center. i.e. higher density = low sv
            // bonus starts at higher sv
            const double low_sv_delta_time_center = 200;
            const double low_sv_delta_time_width = 300;

            // Maximum center for low sv (for high density)
            const double low_sv_center_upper_bound = 200;
            // Minimum center for low sv (for low density)
            const double low_sv_center_lower_bound = 100;
            // Width of low sv center range
            const double low_sv_width = 160;
            // Calculate low sv center, considering density
            double lowSvCenter = low_sv_center_upper_bound - (low_sv_center_upper_bound - low_sv_center_lower_bound) * sigmoid(current.DeltaTime, low_sv_delta_time_center, low_sv_delta_time_width);

            double highSvBonus = sigmoid(current.Reading.EffectiveBPM, high_sv_center, high_sv_width);
            double lowSvBonus = 1 - sigmoid(current.Reading.EffectiveBPM, lowSvCenter, low_sv_width);

            return high_sv_multiplier * highSvBonus + low_sv_multiplier * lowSvBonus;
        }

        private double sigmoid(double value, double center, double width)
        {
            width /= 10;
            return 1 / (1 + Math.Exp(-(value - center) / width));
        }
    }
}
