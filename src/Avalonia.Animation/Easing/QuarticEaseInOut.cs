// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases a <see cref="double"/> value 
    /// using a piece-wise quartic equation.
    /// </summary>
    public class QuarticEaseInOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;

            if (p < 0.5d)
            {
                return 8d * p * p * p * p;
            }
            else
            {
                double f = (p - 1d);
                return -8d * f * f * f * f + 1d;
            }           
        }

    }
}
