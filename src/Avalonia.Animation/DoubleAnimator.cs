// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Animation
{
    /// <summary>
    /// Animator that handles <see cref="double"/> properties.
    /// </summary>
    public class DoubleAnimator : Animator<double>
    {
        /// <inheritdocs/>
        protected override double Interpolate(double fraction, double start, double end)
        {
            return start + (fraction) * (end - start);
        }
    }
}
