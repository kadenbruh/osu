// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm
{
    /// <summary>
    /// A base class for grouping <see cref="IHasInterval"/> instances by their interval.
    /// In scenarios where an interval change occurs, the <see cref="IHasInterval"/> is added to the group with the smaller interval.
    /// </summary>
    /// <typeparam name="ChildType">The type of child objects that implement <see cref="IHasInterval"/>.</typeparam>
    public abstract class EvenRhythm<ChildType>
        where ChildType : IHasInterval
    {
        /// <summary>
        /// Gets the list of child objects within this rhythm group.
        /// </summary>
        public IReadOnlyList<ChildType> Children { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EvenRhythm{ChildType}"/> class.
        /// Groups child objects from the provided data list based on their intervals.
        /// </summary>
        /// <param name="data">The list of <see cref="IHasInterval"/> instances to group.</param>
        /// <param name="currentIndex">
        /// The current index in the <paramref name="data"/> list to start grouping from.
        /// This index is updated to reflect the position after the last grouped child.
        /// </param>
        /// <param name="marginOfError">
        /// The allowable margin of error for interval differences within a group.
        /// Intervals differing by less than or equal to this value are considered equal.
        /// </param>
        protected EvenRhythm(List<ChildType> data, ref int currentIndex, double marginOfError)
        {
            List<ChildType> children = new List<ChildType>();
            Children = children;

            // Add the first child to the group
            children.Add(data[currentIndex]);
            currentIndex++;

            // Iterate through the data to group children with similar intervals
            for (; currentIndex < data.Count - 1; currentIndex++)
            {
                ChildType current = data[currentIndex];
                ChildType next = data[currentIndex + 1];

                if (!IsFlat(current, next, marginOfError))
                {
                    // If the next interval is significantly larger, include the current child in this group
                    if (next.Interval > current.Interval + marginOfError)
                    {
                        children.Add(current);
                        currentIndex++;
                    }

                    // An interval change has occurred; end the current group
                    break;
                }

                // No significant interval change; add the current child to the group
                children.Add(current);
            }

            // Handle the last element if it hasn't been grouped yet
            if (currentIndex < data.Count && (data.Count <= 2 || IsFlat(data[^1], data[^2], marginOfError)))
            {
                children.Add(data[currentIndex]);
                currentIndex++;
            }
        }

        /// <summary>
        /// Determines whether two <see cref="IHasInterval"/> instances have similar intervals within the specified margin of error.
        /// </summary>
        /// <param name="current">The current <see cref="IHasInterval"/> instance.</param>
        /// <param name="next">The next <see cref="IHasInterval"/> instance.</param>
        /// <param name="marginOfError">The allowable margin of error for interval differences.</param>
        /// <returns>
        /// <c>true</c> if the absolute difference between the intervals of <paramref name="current"/> and <paramref name="next"/> is
        /// less than or equal to <paramref name="marginOfError"/>; otherwise, <c>false</c>.
        /// </returns>
        protected bool IsFlat(ChildType current, ChildType next, double marginOfError)
        {
            return Math.Abs(current.Interval - next.Interval) <= marginOfError;
        }
    }
}
