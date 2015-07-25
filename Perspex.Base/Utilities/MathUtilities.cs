// -----------------------------------------------------------------------
// <copyright file="TypeUtilities.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides math utilities not provided in System.Math.
    /// </summary>
    public static class MathUtilities
    {
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
    }
}
