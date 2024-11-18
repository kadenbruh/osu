// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm
{
    /// <summary>
    /// Represents a group of <see cref="TaikoDifficultyHitObject"/>s with no rhythm variation.
    /// </summary>
    public class EvenHitObjects : EvenRhythm<TaikoDifficultyHitObject>, IHasInterval
    {
        public TaikoDifficultyHitObject FirstHitObject => Children[0];

        /// <summary>
        /// <see cref="DifficultyHitObject.StartTime"/> of the first hit object.
        /// </summary>
        public double StartTime => Children[0].StartTime;

        /// <summary>
        /// The interval between the first and final hit object within this group.
        /// </summary>
        public double Duration => Children[^1].StartTime - Children[0].StartTime;

        /// <summary>
        /// The previous group of <see cref="EvenHitObjects"/>, if any.
        /// </summary>
        public EvenHitObjects? Previous { get; private set; }

        /// <summary>
        /// The interval in ms of each hit object in this <see cref="EvenHitObjects"/>. This is only defined if there are
        /// more than one hit object in this group.
        /// </summary>
        public double? HitObjectInterval { get; private set; }

        /// <summary>
        /// The ratio of <see cref="HitObjectInterval"/> between this and the previous <see cref="EvenHitObjects"/>.
        /// In the case where one or both of the <see cref="HitObjectInterval"/> is undefined, this will have a value of 1.
        /// </summary>
        public double HitObjectIntervalRatio { get; private set; } = 1;

        /// <summary>
        /// The interval between the <see cref="StartTime"/> of this and the previous <see cref="EvenHitObjects"/>.
        /// </summary>
        public double Interval { get; private set; } = double.PositiveInfinity;

        public EvenHitObjects(EvenHitObjects? previous, List<TaikoDifficultyHitObject> data, ref int i)
            : base(data, ref i, 3)
        {
            Previous = previous;

            foreach (var hitObject in Children)
            {
                hitObject.Rhythm.EvenHitObjects = this;
            }

            calculateIntervals();
        }

        public static List<EvenHitObjects> GroupHitObjects(List<TaikoDifficultyHitObject> data)
        {
            var flatPatterns = new List<EvenHitObjects>();
            int i = 0;

            while (i < data.Count)
            {
                var previous = flatPatterns.Count > 0 ? flatPatterns[^1] : null;
                flatPatterns.Add(new EvenHitObjects(previous, data, ref i));
            }

            return flatPatterns;
        }

        private void calculateIntervals()
        {
            if (Children.Count > 1)
            {
                HitObjectInterval = (Children[^1].StartTime - Children[0].StartTime) / (Children.Count - 1);
            }

            if (Previous?.HitObjectInterval != null && HitObjectInterval != null)
            {
                HitObjectIntervalRatio = HitObjectInterval.Value / Previous.HitObjectInterval.Value;
            }

            if (Previous != null)
            {
                Interval = StartTime - Previous.StartTime;
            }
        }
    }
}
