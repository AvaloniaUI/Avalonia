// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases in a <see cref="double"/> value 
    /// using the shifted fourth quadrant of
    /// the unit circle.
    /// </summary>
    public class CircularEaseIn : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double p)
        {
            return 1d - Math.Sqrt(1d - p * p);
        }
    }
}
