// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Perspex.Utilities
{
    /// <summary>
    /// Provides math utilities not provided in System.Math.
    /// </summary>
    public static class MathUtilities
    {
        /// <summary>
        /// An approximated value of a machine epsilon for Double.
        /// </summary>
        public const double DoubleEpsilon = 2.2204460492503131e-16;

        /// <summary>
        /// Clamps a value between a minimum and maximum value.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The clamped value.</returns>
        public static double Clamp(double val, double min, double max)
        {
            if (val < min)
            {
                return min;
            }
            else if (val > max)
            {
                return max;
            }
            else
            {
                return val;
            }
        }

        /// <summary>
        /// Check equality between double numbers with epsilon.
        /// </summary>
        /// <param name="a">The first number.</param>
        /// <param name="b">The second number.</param>
        /// <returns>Whether numbers are equal.</returns>
        public static bool Equal(double a, double b)
        {
            return Math.Abs(b - a) <= DoubleEpsilon;
        }
    }
}
