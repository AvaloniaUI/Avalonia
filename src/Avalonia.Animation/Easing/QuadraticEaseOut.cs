// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using a quadratic function.
    /// </summary>
    public class QuadraticEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return -(progress * (progress - 2d));
        }
    }
}
