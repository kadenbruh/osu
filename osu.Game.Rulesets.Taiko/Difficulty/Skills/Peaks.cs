// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    public class TaikoStrain : IComparable<TaikoStrain>
    {
        /// <summary>
        /// Returns the <i>p</i>-norm of an <i>n</i>-dimensional vector.
        /// </summary>
        /// <param name="p">The value of <i>p</i> to calculate the norm for.</param>
        /// <param name="values">The coefficients of the vector.</param>
        public static double Norm(double p, params double[] values) => Math.Pow(values.Sum(x => Math.Pow(x, p)), 1 / p);

        private const double final_multiplier = 0.084375;
        private const double rhythm_skill_multiplier = 0.2 * final_multiplier;
        private const double colour_skill_multiplier = 0.375 * final_multiplier;
        private const double stamina_skill_multiplier = 0.375 * final_multiplier;

        public readonly double Rhythm;
        public readonly double Stamina;
        public readonly double Colour;
        public readonly double Combined;

        public TaikoStrain(double pattern, double stamina, double colour)
        {
            Rhythm = pattern * rhythm_skill_multiplier;
            Stamina = stamina * stamina_skill_multiplier;
            Colour = colour * colour_skill_multiplier;
            Combined = Norm(3, Rhythm, Stamina, Colour);
        }

        int IComparable<TaikoStrain>.CompareTo(TaikoStrain? other)
        {
            return other == null ? 1 : Combined.CompareTo(other.Combined);
        }
    }

    public class Peaks : Skill
    {
        private readonly Rhythm rhythm;
        private readonly Stamina stamina;
        private readonly Colour colour;

        // These stats are only defined after DifficultyValue() is called
        public double RhythmStat { get; private set; }
        public double StaminaStat { get; private set; }
        public double ColourStat { get; private set; }

        public Peaks(Mod[] mods)
            : base(mods)
        {
            rhythm = new Rhythm(mods);
            stamina = new Stamina(mods);
            colour = new Colour(mods);
        }

        public override void Process(DifficultyHitObject current)
        {
            rhythm.Process(current);
            stamina.Process(current);
            colour.Process(current);
        }

        /// <summary>
        /// Returns the combined star rating of the beatmap, calculated using peak strains from all sections of the map.
        /// </summary>
        /// <remarks>
        /// For each section, the peak strains of all separate skills are combined into a single peak strain for the section.
        /// The resulting partial rating of the beatmap is a weighted sum of the combined peaks (higher peaks are weighted more).
        /// </remarks>
        public override double DifficultyValue()
        {
            List<TaikoStrain> peaks = new List<TaikoStrain>();

            var rhythmPeaks = rhythm.GetCurrentStrainPeaks().ToList();
            var staminaPeaks = stamina.GetCurrentStrainPeaks().ToList();
            var colourPeaks = colour.GetCurrentStrainPeaks().ToList();

            for (int i = 0; i < rhythmPeaks.Count; i++)
            {
                TaikoStrain peak = new TaikoStrain(rhythmPeaks[i], staminaPeaks[i], colourPeaks[i]);

                // Sections with 0 strain are excluded to avoid worst-case time complexity of the following sort (e.g. /b/2351871).
                // These sections will not contribute to the difficulty.
                if (peak.Combined > 0)
                    peaks.Add(peak);
            }

            double combinedDifficulty = 0;
            double rhythmDifficulty = 0;
            double staminaDifficulty = 0;
            double colourDifficulty = 0;
            double weight = 1;

            foreach (TaikoStrain strain in peaks.OrderByDescending(d => d))
            {
                combinedDifficulty += strain.Combined * weight;
                rhythmDifficulty += strain.Rhythm * weight;
                staminaDifficulty += strain.Stamina * weight;
                colourDifficulty += strain.Colour * weight;
                weight *= 0.9;
            }

            RhythmStat = rhythmDifficulty;
            StaminaStat = staminaDifficulty;
            ColourStat = colourDifficulty;

            return combinedDifficulty;
        }
    }
}
