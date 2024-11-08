using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    public class Reading : StrainDecaySkill
    {
        protected override double SkillMultiplier => 1.0;
        protected override double StrainDecayBase => 0.4;

        private double currentStrain;

        public double ObjectDensity { get; private set; }

        public Reading(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            if (current is not TaikoDifficultyHitObject taikoObject)
                return 0.0;

            currentStrain *= StrainDecayBase;
            currentStrain += ReadingEvaluator.EvaluateDifficultyOf(taikoObject) * SkillMultiplier;

            // Set the objectDensity using the separate method from the evaluator.
            ObjectDensity = ReadingEvaluator.GetObjectDensity(taikoObject);

            return currentStrain;
        }
    }
}
