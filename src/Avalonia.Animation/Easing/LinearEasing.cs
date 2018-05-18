// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Linearly eases a <see cref="double"/> value.
    /// </summary>
    public class LinearEasing : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return progress;
        }
    }
}
