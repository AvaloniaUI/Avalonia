// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases a <see cref="double"/> value 
    /// using a piecewise unit circle function.
    /// </summary>
    public class CircularEaseInOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;
            if (p < 0.5d)
            {
                return 0.5d * (1d - Math.Sqrt(1d - 4d * (p * p)));
            }
            else
            {
                return 0.5d * (Math.Sqrt(-((2d * p) - 3d) * ((2d * p) - 1d)) + 1d);
            }
        }

    }
}
