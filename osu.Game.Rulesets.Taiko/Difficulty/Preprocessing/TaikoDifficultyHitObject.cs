// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko.Objects;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing
{
    /// <summary>
    /// Represents a single hit object in taiko difficulty calculation.
    /// </summary>
    public class TaikoDifficultyHitObject : DifficultyHitObject
    {
        /// <summary>
        /// The rhythm required to hit this hit object.
        /// </summary>
        public readonly TaikoDifficultyHitObjectRhythm Rhythm;

        /// <summary>
        /// The hit type of this hit object.
        /// </summary>
        public readonly HitType? HitType;

        /// <summary>
        /// Creates a list of <see cref="TaikoDifficultyHitObject"/>s from a <see cref="IBeatmap"/>s.
        /// This is moved here from TaikoDifficultyCalculator
        /// </summary>
        /// <param name="beatmap">The beatmap from which the list of <see cref="TaikoDifficultyHitObject"/> is created.</param>
        /// <param name="clockRate">The rate at which the gameplay clock is run at.</param>
        public static List<DifficultyHitObject> Create(IBeatmap beatmap, double clockRate)
        {
            List<DifficultyHitObject> taikoDifficultyHitObjects = new List<DifficultyHitObject>();

            for (int i = 2; i < beatmap.HitObjects.Count; i++)
            {
                taikoDifficultyHitObjects.Add(
                    new TaikoDifficultyHitObject(
                        beatmap.HitObjects[i],
                        beatmap.HitObjects[i - 1],
                        beatmap.HitObjects[i - 2],
                        clockRate, taikoDifficultyHitObjects,
                        taikoDifficultyHitObjects.Count
                    )
                );
            }

            return taikoDifficultyHitObjects;
        }

        /// <summary>
        /// Creates a new difficulty hit object.
        /// </summary>
        /// <param name="hitObject">The gameplay <see cref="HitObject"/> associated with this difficulty object.</param>
        /// <param name="lastObject">The gameplay <see cref="HitObject"/> preceding <paramref name="hitObject"/>.</param>
        /// <param name="lastLastObject">The gameplay <see cref="HitObject"/> preceding <paramref name="lastObject"/>.</param>
        /// <param name="clockRate">The rate of the gameplay clock. Modified by speed-changing mods.</param>
        /// <param name="objects">The list of <see cref="DifficultyHitObject"/>s in the current beatmap.</param>
        /// /// <param name="index">The position of this <see cref="DifficultyHitObject"/> in the <paramref name="objects"/> list.</param>
        public TaikoDifficultyHitObject(HitObject hitObject, HitObject lastObject, HitObject lastLastObject, double clockRate, List<DifficultyHitObject> objects, int index)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            var currentHit = hitObject as Hit;

            Rhythm = getClosestRhythm(lastObject, lastLastObject, clockRate);
            HitType = currentHit?.Type;
        }

        /// <summary>
        /// List of most common rhythm changes in taiko maps.
        /// </summary>
        /// <remarks>
        /// The general guidelines for the values are:
        /// <list type="bullet">
        /// <item>rhythm changes with ratio closer to 1 (that are <i>not</i> 1) are harder to play,</item>
        /// <item>speeding up is <i>generally</i> harder than slowing down (with exceptions of rhythm changes requiring a hand switch).</item>
        /// </list>
        /// </remarks>
        private static readonly TaikoDifficultyHitObjectRhythm[] common_rhythms =
        {
            new TaikoDifficultyHitObjectRhythm(1, 1, 0.0),
            new TaikoDifficultyHitObjectRhythm(2, 1, 0.3),
            new TaikoDifficultyHitObjectRhythm(1, 2, 0.5),
            new TaikoDifficultyHitObjectRhythm(3, 1, 0.3),
            new TaikoDifficultyHitObjectRhythm(1, 3, 0.35),
            new TaikoDifficultyHitObjectRhythm(3, 2, 0.6), // purposefully higher (requires hand switch in full alternating gameplay style)
            new TaikoDifficultyHitObjectRhythm(2, 3, 0.4),
            new TaikoDifficultyHitObjectRhythm(5, 4, 0.5),
            new TaikoDifficultyHitObjectRhythm(4, 5, 0.7)
        };

        /// <summary>
        /// Returns the closest rhythm change from <see cref="common_rhythms"/> required to hit this object.
        /// </summary>
        /// <param name="lastObject">The gameplay <see cref="HitObject"/> preceding this one.</param>
        /// <param name="lastLastObject">The gameplay <see cref="HitObject"/> preceding <paramref name="lastObject"/>.</param>
        /// <param name="clockRate">The rate of the gameplay clock.</param>
        private TaikoDifficultyHitObjectRhythm getClosestRhythm(HitObject lastObject, HitObject lastLastObject, double clockRate)
        {
            double prevLength = (lastObject.StartTime - lastLastObject.StartTime) / clockRate;
            double ratio = DeltaTime / prevLength;

            return common_rhythms.OrderBy(x => Math.Abs(x.Ratio - ratio)).First();
        }
    }
}
