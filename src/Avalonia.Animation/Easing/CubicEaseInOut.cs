// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases a <see cref="double"/> value 
    /// using a piece-wise cubic equation.
    /// </summary>
    public class CubicEaseInOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;

            if (progress < 0.5d)
            {
                return 4d * p * p * p;
            }
            else
            {
                double f = 2d * (p - 1);
                return 0.5d * f * f * f + 1d;
            }
        }
    }
}
