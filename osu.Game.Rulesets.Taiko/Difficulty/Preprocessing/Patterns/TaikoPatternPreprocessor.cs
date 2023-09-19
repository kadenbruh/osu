// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Collections.Generic;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Patterns.Aggregators;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Patterns
{
    public class TaikoPatternPreprocessor
    {
        private readonly ColourAggregator colourAggregator;
        private readonly RhythmAggregator rhythmAggregator;
        private readonly RepetitionAggregator repetitionAggregator;

        public TaikoPatternPreprocessor(HitWindows hitWindows)
        {
            colourAggregator = new ColourAggregator();

            // Using 3ms as the hitwindow, as note timings are stored in ms.
            // Might want to consider using some sort of hit window instead
            rhythmAggregator = new RhythmAggregator(3);

            // streamAggregator = new StreamAggregator(hitWindows.WindowFor(HitResult.Meh));

            repetitionAggregator = new RepetitionAggregator();
        }

        public void ProcessAndAssign(List<TaikoDifficultyHitObject> hitObjects)
        {
            // Group notes by their interval.
            List<FlatRhythmHitObjects> flatRhythmPatterns =
                rhythmAggregator.Group<TaikoDifficultyHitObject, FlatRhythmHitObjects>(hitObjects);
            flatRhythmPatterns.ForEach(item => item.FirstHitObject.Pattern.FlatRhythmPattern = item);

            // Second rhythm pass
            List<SecondPassRhythmPattern> secondPassRhythmPatterns =
                rhythmAggregator.Group<FlatRhythmHitObjects, SecondPassRhythmPattern>(flatRhythmPatterns);
            secondPassRhythmPatterns.ForEach(item => item.FirstHitObject.Pattern.SecondPassRhythmPattern = item);

            // Third rhythm pass
            List<ThirdPassRhythmPattern> thirdPassRhythmPatterns =
                rhythmAggregator.Group<SecondPassRhythmPattern, ThirdPassRhythmPattern>(secondPassRhythmPatterns);
            thirdPassRhythmPatterns.ForEach(item => item.FirstHitObject.Pattern.ThirdPassRhythmPattern = item);

            // Within each flatRhythmPattern, group notes by colour.
            List<MonoPattern> flatMonoPatterns = new List<MonoPattern>();
            flatRhythmPatterns.ForEach(item =>
            {
                List<MonoPattern> grouped = colourAggregator.Group(item.Children);
                // Link cross-rhythm patterns together
                // This is so that a new flat rhythm pattern doesn't always create a new colourPattern
                if (flatMonoPatterns.Count > 0)
                {
                    grouped.First().Previous = flatMonoPatterns.Last();
                }
                flatMonoPatterns.AddRange(grouped);
            });
            flatMonoPatterns.ForEach(item => item.FirstHitObject.Pattern.MonoPattern = item);

            // First pass colour pattern
            List<ColourRhythm> colourPatterns = rhythmAggregator.Group<MonoPattern, ColourRhythm>(flatMonoPatterns);
            colourPatterns.ForEach(item => item.FirstHitObject.Pattern.FirstPassColourPattern = item);

            // Second pass colour pattern
            List<SecondPassColourRhythm> secondPassColourPatterns = rhythmAggregator.Group<ColourRhythm, SecondPassColourRhythm>(colourPatterns);
            secondPassColourPatterns.ForEach(item => item.FirstHitObject.Pattern.SecondPassColourPattern = item);

            // List<DifficultyPattern> streams = streamAggregator.Aggregate(hitObjects);

            // List<ColourSequence> colourSequences = new();
            // foreach (var stream in streams)
            // {
            //     if (stream == null || stream.Children.Count() <= 5)
            //     {
            //         continue;
            //     }

            //     List<MonoPattern> monosInStream = colourAggregator.Group(stream.Children);
            //     colourSequences.AddRange(
            //         repetitionAggregator.Group<MonoPattern, ColourSequence>(monosInStream));
            // }
            // foreach (var item in colourSequences)
            // {
            //     item.FirstHitObject.Pattern.ColourSequence = item;
            // }

            List<MonoPattern> untimedMonos = colourAggregator.Group(hitObjects);
            List<ColourSequence> colourSequences = repetitionAggregator.Group<MonoPattern, ColourSequence>(untimedMonos);
            foreach (var item in colourSequences)
            {
                item.FirstHitObject.Pattern.ColourSequence = item;
            }
        }
    }
}
