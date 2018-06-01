// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.
using Avalonia.Animation.Utils;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using a simulated bounce function.
    /// </summary>
    public class BounceEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return BounceEaseUtils.Bounce(progress);
        }
    }
}
