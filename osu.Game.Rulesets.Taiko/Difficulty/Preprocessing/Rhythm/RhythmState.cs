// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Utils;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm
{
    public class RhythmEvaluationState
    {
        public double CurrentStrain { get; set; }
        public LimitedCapacityQueue<TaikoDifficultyHitObject> RhythmHistory { get; } = new LimitedCapacityQueue<TaikoDifficultyHitObject>(8);
        public int NotesSinceRhythmChange { get; set; }
    }
}
