// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases a <see cref="double"/> value 
    /// using a piecewise exponential function.
    /// </summary>
    public class ExponentialEaseInOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;

            if (p < 0.5d)
            {
                return 0.5d * Math.Pow(2d, 20d * p - 10d);
            }
            else
            {
                return -0.5d * Math.Pow(2d, -20d * p + 10d) + 1d;
            }
        }
    }
}
