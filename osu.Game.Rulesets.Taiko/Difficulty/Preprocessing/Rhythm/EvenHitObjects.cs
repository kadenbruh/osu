// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm
{
    /// <summary>
    /// Represents a group of <see cref="TaikoDifficultyHitObject"/>s with no rhythm variation.
    /// </summary>
    public class EvenHitObjects : EvenRhythm<TaikoDifficultyHitObject>, IHasInterval
    {
        public TaikoDifficultyHitObject FirstHitObject => Children.First();

        /// <summary>
        /// <see cref="DifficultyHitObject.StartTime"/> of the first hit object.
        /// </summary>
        public double StartTime => Children.First().StartTime;

        /// <summary>
        /// The interval between the first and final hit object within this group.
        /// </summary>
        public double Duration => Children.Last().StartTime - Children.First().StartTime;

        public EvenHitObjects? Previous;

        /// <summary>
        /// The interval in ms of each hit object in this <see cref="EvenHitObjects"/>. This is only defined if there is
        /// more than two hit objects in this <see cref="EvenHitObjects"/>.
        /// </summary>
        public double? HitObjectInterval;

        /// <summary>
        /// The ratio of <see cref="HitObjectInterval"/> between this and the previous <see cref="EvenHitObjects"/>. In the
        /// case where one or both of the <see cref="HitObjectInterval"/> is undefined, this will have a value of 1.
        /// </summary>
        public double HitObjectIntervalRatio = 1;

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

                // Pass the HitObjectInterval to each child.
                hitObject.HitObjectInterval = HitObjectInterval;
            }

            calculateIntervals();
        }

        public static List<EvenHitObjects> GroupHitObjects(List<TaikoDifficultyHitObject> data)
        {
            List<EvenHitObjects> flatPatterns = new List<EvenHitObjects>();

            // Index does not need to be incremented, as it is handled within EvenRhythm's constructor.
            for (int i = 0; i < data.Count;)
            {
                EvenHitObjects? previous = flatPatterns.Count > 0 ? flatPatterns[^1] : null;
                flatPatterns.Add(new EvenHitObjects(previous, data, ref i));
            }

            return flatPatterns;
        }

        private void calculateIntervals()
        {
            HitObjectInterval = Children.Count < 2 ? null : (Children[^1].StartTime - Children[0].StartTime) / (Children.Count - 1);

            if (Previous?.HitObjectInterval != null && HitObjectInterval != null)
            {
                HitObjectIntervalRatio = HitObjectInterval.Value / Previous.HitObjectInterval.Value;
            }

            if (Previous == null)
            {
                return;
            }

            Interval = StartTime - Previous.StartTime;
        }
    }
}
