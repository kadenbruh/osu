﻿using System;
// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing
{
    /// <summary>
    /// Represents a rhythm change in a taiko map.
    /// </summary>
    public class TaikoDifficultyHitObjectRhythm : IEquatable<TaikoDifficultyHitObjectRhythm>
    {
        /// <summary>
        /// The difficulty multiplier associated with this rhythm change.
        /// </summary>
        public readonly double Difficulty;

        /// <summary>
        /// The ratio of current <see cref="osu.Game.Rulesets.Difficulty.Preprocessing.DifficultyHitObject.DeltaTime"/>
        /// to previous <see cref="osu.Game.Rulesets.Difficulty.Preprocessing.DifficultyHitObject.DeltaTime"/> for the rhythm change.
        /// A <see cref="Ratio"/> above 1 indicates a slow-down; a <see cref="Ratio"/> below 1 indicates a speed-up.
        /// </summary>
        public readonly double Ratio;

        public readonly double Leniency;


        /// <summary>
        /// Creates an object representing a rhythm change. Difficulty is calculated from the ratio.
        /// </summary>
        /// <param name="ratio">Ratio of current interval to previous interval</param>
        /// <param name="greatHitWindow">Great hit window of the beatmap</param>
        /// <param name="pastInterval">Interval between this and previous note</param>
        /// <param name="futureInterval">Interval between this and next note</param>
        public TaikoDifficultyHitObjectRhythm(double ratio, double greatHitWindow, double pastInterval, double futureInterval)
        {
            Ratio = ratio;
            Difficulty = difficultyFromRatio(ratio);
            this.Leniency = greatHitWindow / Math.Min(pastInterval, futureInterval);
            // Console.WriteLine(this.Leniency);
        }

        /// <summary>
        /// Considered equal if the ratios are sufficiently close. This is to account for double precision rounding errors.
        /// </summary>
        public bool Equals(TaikoDifficultyHitObjectRhythm other)
        {
            return Math.Abs(other.Ratio - Ratio) < 0.05;
        }

        /// <summary>
        /// Calculate difficulty from ratio
        /// </summary>
        private double difficultyFromRatio(double ratio)
        {
            // Sum of n = 8 terms of periodic penalty. A more common denominator will be penalized multiple time, hence
            // simpler rhythm change will be penalized more.
            // Note that to penalize 1/4 properly, a power-of-two n is required.

            // For offsetting the penalty so that a positive difficulty is given.
            double multiplierSum = 0;
            double difficulty = 0;
            for (int i = 1; i < 8; ++i)
            {
                double currentTermMultiplier = termMultiplier(i);
                difficulty += termPenalty(ratio, i, 2, currentTermMultiplier);
                multiplierSum += currentTermMultiplier;
            }
            difficulty += multiplierSum;

            // Speeding up is more difficult than slowing down(?)
            // difficulty += speedUpBonus(ratio, 1.05, 10, 1);

            // Give bonus to near-1 ratios
            difficulty += targetedBonus(ratio, 1, 0.5, 0.1);

            // Penalize ratios that are VERY near 1
            difficulty -= targetedBonus(ratio, 1, 0.1, 0.3);

            // Re-offset the graph to give 0 at ratio 1
            difficulty += 0.2;

            return difficulty;
        }

        /// <summary>
        /// Gives a bonus for speed up.
        /// </summary>
        private double speedUpBonus(double ratio, double steepness, double multiplier, double upperBound)
        {
            if (ratio > upperBound) return 0;
            return multiplier / Math.Pow(steepness, ratio) - multiplier / Math.Pow(steepness, upperBound);
        }

        /// <summary>
        /// Gives a bonus for target ratio using a bell-shaped function.
        /// </summary>
        private double targetedBonus(double ratio, double targetRatio, double width, double multiplier)
        {
            // Gaussian function
            return multiplier * Math.Exp(Math.E * -(Math.Pow(ratio - targetRatio, 2) / Math.Pow(width, 2)));
        }

        /// <summary>
        /// Multiplier for a given denominator term. Note that multipliers will also affects terms that "lines
        /// up" with the denominator term. i.e. multiplier for n/4 will also affect n/2
        /// </summary>
        private double termMultiplier(int denominator)
        {
            switch (denominator)
            {
                case 1:
                    return 0.14;
                case 2:
                    return 0.1;
                case 3:
                    return 0.18;
                default:
                    return 2 / Math.Pow(denominator, 2);
            }
        }

        /// <summary>
        /// Calculate the penalty of a single denominator for the given ratio. The sum of the result of this method
        /// for multiple denominators is the total penalty for the given ratio.
        /// </summary>
        /// <param name="ratio">The ratio of current and previous <see cref="osu.Game.Rulesets.Difficulty.Preprocessing.DifficultyHitObject.DeltaTime" /></param>
        /// <param name="denominator">The denominator term to calculate the penalty for. A denominator of 3 means 1/3, 2/3, 3/3, 4/3 etc will be penalized</param>
        /// <param name="power">Power to raise to, higher power will result in a narrower penalization range.</param>
        /// <param name="multiplier">Multiplier for the denominator term.</param>
        private double termPenalty(double ratio, int denominator, double power, double multiplier)
        {
            return -multiplier * Math.Pow(Math.Cos(denominator * Math.PI * ratio), power);
        }
    }
}
