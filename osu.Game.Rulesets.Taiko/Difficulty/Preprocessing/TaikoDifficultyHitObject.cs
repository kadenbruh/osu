// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Taiko.Objects;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Colour;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Rhythm;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing
{
    /// <summary>
    /// Represents a single hit object in taiko difficulty calculation.
    /// </summary>
    public class TaikoDifficultyHitObject : DifficultyHitObject
    {
        /// <summary>
        /// The list of all <see cref="TaikoDifficultyHitObject"/> of the same colour as this <see cref="TaikoDifficultyHitObject"/> in the beatmap.
        /// </summary>
        private readonly IReadOnlyList<TaikoDifficultyHitObject>? monoDifficultyHitObjects;

        /// <summary>
        /// The index of this <see cref="TaikoDifficultyHitObject"/> in <see cref="monoDifficultyHitObjects"/>.
        /// </summary>
        public readonly int MonoIndex;

        /// <summary>
        /// The list of all <see cref="TaikoDifficultyHitObject"/> that is either a regular note or finisher in the beatmap
        /// </summary>
        private readonly IReadOnlyList<TaikoDifficultyHitObject> noteDifficultyHitObjects;

        /// <summary>
        /// The index of this <see cref="TaikoDifficultyHitObject"/> in <see cref="noteDifficultyHitObjects"/>.
        /// </summary>
        public readonly int NoteIndex;

        /// <summary>
        /// The rhythm required to hit this hit object.
        /// </summary>
        public readonly TaikoDifficultyHitObjectRhythm Rhythm;

        /// <summary>
        /// Colour data for this hit object. This is used by colour evaluator to calculate colour difficulty, but can be used
        /// by other skills in the future.
        /// </summary>
        public readonly TaikoDifficultyHitObjectColour Colour;

        /// <summary>
        /// Creates a new difficulty hit object.
        /// </summary>
        /// <param name="hitObject">The gameplay <see cref="HitObject"/> associated with this difficulty object.</param>
        /// <param name="lastObject">The gameplay <see cref="HitObject"/> preceding <paramref name="hitObject"/>.</param>
        /// <param name="lastLastObject">The gameplay <see cref="HitObject"/> preceding <paramref name="lastObject"/>.</param>
        /// <param name="clockRate">The rate of the gameplay clock. Modified by speed-changing mods.</param>
        /// <param name="objects">The list of all <see cref="DifficultyHitObject"/>s in the current beatmap.</param>
        /// <param name="centreHitObjects">The list of centre (don) <see cref="DifficultyHitObject"/>s in the current beatmap.</param>
        /// <param name="rimHitObjects">The list of rim (kat) <see cref="DifficultyHitObject"/>s in the current beatmap.</param>
        /// <param name="noteObjects">The list of <see cref="DifficultyHitObject"/>s that is a hit (i.e. not a drumroll or swell) in the current beatmap.</param>
        /// <param name="index">The position of this <see cref="DifficultyHitObject"/> in the <paramref name="objects"/> list.</param>
        public TaikoDifficultyHitObject(HitObject hitObject, HitObject lastObject, HitObject lastLastObject, double clockRate,
                                        List<DifficultyHitObject> objects,
                                        List<TaikoDifficultyHitObject> centreHitObjects,
                                        List<TaikoDifficultyHitObject> rimHitObjects,
                                        List<TaikoDifficultyHitObject> noteObjects, int index)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            noteDifficultyHitObjects = noteObjects;

            // Create the Colour object, its properties should be filled in by TaikoDifficultyPreprocessor
            Colour = new TaikoDifficultyHitObjectColour();
            Rhythm = createRhythmChange(lastObject, lastLastObject, clockRate);

            switch ((hitObject as Hit)?.Type)
            {
                case HitType.Centre:
                    MonoIndex = centreHitObjects.Count;
                    centreHitObjects.Add(this);
                    monoDifficultyHitObjects = centreHitObjects;
                    break;

                case HitType.Rim:
                    MonoIndex = rimHitObjects.Count;
                    rimHitObjects.Add(this);
                    monoDifficultyHitObjects = rimHitObjects;
                    break;
            }

            if (hitObject is Hit)
            {
                NoteIndex = noteObjects.Count;
                noteObjects.Add(this);
            }
        }

        /// <summary>
        /// Returns a <see cref="TaikoDifficultyHitObjectRhythm"/> representing the rhythm change of the current object.
        /// </summary>
        /// <param name="lastObject">The gameplay <see cref="HitObject"/> preceding this one.</param>
        /// <param name="lastLastObject">The gameplay <see cref="HitObject"/> preceding <paramref name="lastObject"/>.</param>
        /// <param name="clockRate">The rate of the gameplay clock.</param>
        private TaikoDifficultyHitObjectRhythm createRhythmChange(HitObject lastObject, HitObject lastLastObject, double clockRate)
        {
            double prevLength = (lastObject.StartTime - lastLastObject.StartTime) / clockRate;
            double ratio = DeltaTime / prevLength;

            return new TaikoDifficultyHitObjectRhythm(ratio);
        }

        public TaikoDifficultyHitObject? PreviousMono(int backwardsIndex) => monoDifficultyHitObjects?.ElementAtOrDefault(MonoIndex - (backwardsIndex + 1));

        public TaikoDifficultyHitObject? NextMono(int forwardsIndex) => monoDifficultyHitObjects?.ElementAtOrDefault(MonoIndex + (forwardsIndex + 1));

        public TaikoDifficultyHitObject? PreviousNote(int backwardsIndex) => noteDifficultyHitObjects.ElementAtOrDefault(NoteIndex - (backwardsIndex + 1));

        public TaikoDifficultyHitObject? NextNote(int forwardsIndex) => noteDifficultyHitObjects.ElementAtOrDefault(NoteIndex + (forwardsIndex + 1));
    }
}
