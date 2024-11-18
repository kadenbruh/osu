// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm
{
    /// <summary>
    /// Represents <see cref="EvenHitObjects"/> grouped by their <see cref="EvenHitObjects.StartTime"/>'s interval.
    /// </summary>
    public class EvenPatterns : EvenRhythm<EvenHitObjects>
    {
        /// <summary>
        /// The previous group of <see cref="EvenPatterns"/>, if any.
        /// </summary>
        public EvenPatterns? Previous { get; private set; }

        /// <summary>
        /// The <see cref="EvenHitObjects.Interval"/> between children <see cref="EvenHitObjects"/> within this group.
        /// If there is more than one child, this will have the value of the second child's <see cref="EvenHitObjects.Interval"/>.
        /// Otherwise, it will take the value of the first child's <see cref="EvenHitObjects.Interval"/>.
        /// </summary>
        public double ChildrenInterval => Children.Count > 1 ? Children[1].Interval : Children[0].Interval;

        /// <summary>
        /// The ratio of <see cref="ChildrenInterval"/> between this and the previous <see cref="EvenPatterns"/>.
        /// In the case where there is no previous <see cref="EvenPatterns"/>, or the previous <see cref="ChildrenInterval"/> is zero,
        /// this will have a value of 1.
        /// </summary>
        public double IntervalRatio => (Previous != null && Previous.ChildrenInterval != 0)
            ? ChildrenInterval / Previous.ChildrenInterval
            : 1.0d;

        /// <summary>
        /// The first hit object in the first <see cref="EvenHitObjects"/> of this pattern.
        /// </summary>
        public TaikoDifficultyHitObject FirstHitObject => Children[0].FirstHitObject;

        /// <summary>
        /// All hit objects contained within this pattern.
        /// </summary>
        public IEnumerable<TaikoDifficultyHitObject> AllHitObjects => getAllHitObjects();

        /// <summary>
        /// Initializes a new instance of the <see cref="EvenPatterns"/> class.
        /// </summary>
        /// <param name="previous">The previous <see cref="EvenPatterns"/> group, if any.</param>
        /// <param name="data">The list of <see cref="EvenHitObjects"/> to group.</param>
        /// <param name="i">The current index in the <paramref name="data"/> list.</param>
        private EvenPatterns(EvenPatterns? previous, List<EvenHitObjects> data, ref int i)
            : base(data, ref i, 3)
        {
            Previous = previous;

            foreach (var hitObject in AllHitObjects)
            {
                hitObject.Rhythm.EvenPatterns = this;
            }
        }

        /// <summary>
        /// Groups the provided <see cref="EvenHitObjects"/> into <see cref="EvenPatterns"/>.
        /// </summary>
        /// <param name="data">The list of <see cref="EvenHitObjects"/> to group.</param>
        /// <returns>A list of grouped <see cref="EvenPatterns"/>.</returns>
        public static void GroupPatterns(List<EvenHitObjects> data)
        {
            var evenPatterns = new List<EvenPatterns>();
            int i = 0;

            while (i < data.Count)
            {
                var previous = evenPatterns.Count > 0 ? evenPatterns[^1] : null;
                evenPatterns.Add(new EvenPatterns(previous, data, ref i));
            }
        }

        /// <summary>
        /// Retrieves all hit objects within this pattern.
        /// </summary>
        /// <returns>An enumerable of all <see cref="TaikoDifficultyHitObject"/>s.</returns>
        private IEnumerable<TaikoDifficultyHitObject> getAllHitObjects()
        {
            foreach (var evenHitObject in Children)
            {
                foreach (var hitObject in evenHitObject.Children)
                {
                    yield return hitObject;
                }
            }
        }
    }
}
