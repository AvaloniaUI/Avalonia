// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Animation
{
    /// <summary>
    /// Eases a <see cref="double"/> value 
    /// using a piece-wise quartic equation.
    /// </summary>
    public class QuinticEaseInOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;

            if (progress < 0.5d)
            {
                return 16d * p * p * p * p * p;
            }
            else
            {
                double f = ((2d * p) - 2d);
                return 0.5d * f * f * f * f * f + 1d;
            }
        }
    }
}
