using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    /// <summary>
    /// Calculates the rhythm coefficient of taiko difficulty using the TaikoRhythmEvaluator.
    /// </summary>
    public class Rhythm : StrainDecaySkill
    {
        private readonly RhythmEvaluationState state = new RhythmEvaluationState();

        protected override double SkillMultiplier => 10;
        protected override double StrainDecayBase => 0;

        public Rhythm(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            if (current is not TaikoDifficultyHitObject taikoObject)
                return 0.0;

            return TaikoRhythmEvaluator.EvaluateDifficultyOf(taikoObject, state);
        }
    }
}
