﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Rulesets.Taiko.Difficulty
{
    public class TaikoDifficultyAttributes : DifficultyAttributes
    {
        /// <summary>
        /// The difficulty corresponding to the stamina skill.
        /// </summary>
        [JsonProperty("stamina_difficulty")]
        public double StaminaDifficulty { get; set; }

        /// <summary>
        /// The ratio of stamina difficulty from mono-color (single colour) streams to total stamina difficulty.
        /// </summary>
        [JsonProperty("mono_stamina_factor")]
        public double MonoStaminaFactor { get; set; }

        /// <summary>
        /// The difficulty corresponding to the rhythm skill.
        /// </summary>
        [JsonProperty("rhythm_difficulty")]
        public double RhythmDifficulty { get; set; }

        /// <summary>
        /// The difficulty corresponding to the reading skill.
        /// </summary>
        [JsonProperty("reading_difficulty")]
        public double ReadingDifficulty { get; set; }

        /// <summary>
        /// The difficulty corresponding to the colour skill.
        /// </summary>
        [JsonProperty("colour_difficulty")]
        public double ColourDifficulty { get; set; }

        /// <summary>
        /// The difficulty corresponding to the hardest parts of the map.
        /// </summary>
        [JsonProperty("peak_difficulty")]
        public double PeakDifficulty { get; set; }

        /// <summary>
        /// The perceived hit window for a GREAT hit inclusive of rate-adjusting mods (DT/HT/etc).
        /// </summary>
        /// <remarks>
        /// Rate-adjusting mods don't directly affect the hit window, but have a perceived effect as a result of adjusting audio timing.
        /// </remarks>
        [JsonProperty("great_hit_window")]
        public double GreatHitWindow { get; set; }

        /// <summary>
        /// The perceived hit window for an OK hit inclusive of rate-adjusting mods (DT/HT/etc).
        /// </summary>
        /// <remarks>
        /// Rate-adjusting mods don't directly affect the hit window, but have a perceived effect as a result of adjusting audio timing.
        /// </remarks>
        [JsonProperty("ok_hit_window")]
        public double OkHitWindow { get; set; }

        /// <summary>
        /// The calculated object density for a beatmap. Scaled based on the time between consecutive objects.
        /// </summary>
        [JsonProperty("hit_object_density")]
        public double ObjectDensity { get; set; }

        public override IEnumerable<(int attributeId, object value)> ToDatabaseAttributes()
        {
            foreach (var v in base.ToDatabaseAttributes())
                yield return v;

            yield return (ATTRIB_ID_DIFFICULTY, StarRating);
            yield return (ATTRIB_ID_GREAT_HIT_WINDOW, GreatHitWindow);
            yield return (ATTRIB_ID_OK_HIT_WINDOW, OkHitWindow);
            yield return (ATTRIB_ID_MONO_STAMINA_FACTOR, MonoStaminaFactor);
            yield return (ATTRIB_ID_OBJECT_DENSITY, ObjectDensity);
        }

        public override void FromDatabaseAttributes(IReadOnlyDictionary<int, double> values, IBeatmapOnlineInfo onlineInfo)
        {
            base.FromDatabaseAttributes(values, onlineInfo);

            StarRating = values[ATTRIB_ID_DIFFICULTY];
            GreatHitWindow = values[ATTRIB_ID_GREAT_HIT_WINDOW];
            OkHitWindow = values[ATTRIB_ID_OK_HIT_WINDOW];
            MonoStaminaFactor = values[ATTRIB_ID_MONO_STAMINA_FACTOR];
            ObjectDensity = values[ATTRIB_ID_OBJECT_DENSITY];
        }
    }
}
