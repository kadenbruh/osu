using System;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Evaluators
{
    public static class TaikoRhythmEvaluator
    {
        private const double strain_decay = 0.96;
        private const int rhythm_history_max_length = 8;

        public static double EvaluateDifficultyOf(TaikoDifficultyHitObject hitObject, RhythmEvaluationState state)
        {
            if (!(hitObject.BaseObject is Hit))
            {
                ResetState(state);
                return 0.0;
            }

            state.CurrentStrain *= strain_decay;
            state.NotesSinceRhythmChange++;

            if (hitObject.Rhythm.Difficulty == 0.0)
                return 0.0;

            double objectStrain = hitObject.Rhythm.Difficulty
                                  * calculateRepetitionPenalties(hitObject, state)
                                  * calculatePatternLengthPenalty(state.NotesSinceRhythmChange)
                                  * calculateSpeedPenalty(hitObject.DeltaTime);

            state.NotesSinceRhythmChange = 0;
            state.CurrentStrain += objectStrain;
            return state.CurrentStrain;
        }

        private static double calculateRepetitionPenalties(TaikoDifficultyHitObject hitObject, RhythmEvaluationState state)
        {
            double penalty = 1;
            state.RhythmHistory.Enqueue(hitObject);

            for (int recentPatterns = 2; recentPatterns <= rhythm_history_max_length / 2; recentPatterns++)
            {
                if (hasMatchingPattern(recentPatterns, state, out int startIndex))
                {
                    int notesSince = hitObject.Index - state.RhythmHistory[startIndex].Index;
                    penalty *= applyRepetitionPenalty(notesSince);
                }
            }

            return penalty;
        }

        private static bool hasMatchingPattern(int length, RhythmEvaluationState state, out int startIndex)
        {
            startIndex = state.RhythmHistory.Count - length - 1;

            for (; startIndex >= 0; startIndex--)
            {
                if (isPatternMatch(startIndex, length, state))
                    return true;
            }

            return false;
        }

        private static bool isPatternMatch(int start, int length, RhythmEvaluationState state)
        {
            for (int i = 0; i < length; i++)
            {
                if (state.RhythmHistory[start + i].Rhythm != state.RhythmHistory[state.RhythmHistory.Count - length + i].Rhythm)
                    return false;
            }

            return true;
        }

        private static double applyRepetitionPenalty(int notesSince) => Math.Min(1.0, 0.032 * notesSince);

        private static double calculatePatternLengthPenalty(int patternLength)
        {
            double shortPenalty = Math.Min(0.15 * patternLength, 1.0);
            double longPenalty = Math.Clamp(2.5 - 0.15 * patternLength, 0.0, 1.0);
            return Math.Min(shortPenalty, longPenalty);
        }

        private static double calculateSpeedPenalty(double deltaTime)
        {
            if (deltaTime < 80) return 1.0;
            if (deltaTime < 210) return Math.Max(0, 1.4 - 0.005 * deltaTime);
            return 0.0;
        }

        private static void ResetState(RhythmEvaluationState state)
        {
            state.CurrentStrain = 0.0;
            state.NotesSinceRhythmChange = 0;
        }
    }
}
