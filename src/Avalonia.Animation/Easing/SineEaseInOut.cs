// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases a <see cref="double"/> value 
    /// using a half sine wave function.
    /// </summary>
    public class SineEaseInOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return 0.5d * (1d - Math.Cos(progress * Math.PI));
        }
    }
}
