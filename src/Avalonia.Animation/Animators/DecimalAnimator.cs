// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="decimal"/> properties.
    /// </summary>
    public class DecimalAnimator : Animator<decimal>
    {
        /// <inheritdocs/>
        public override decimal Interpolate(double progress, decimal oldValue, decimal newValue)
        {
            return ((newValue - oldValue) * (decimal)progress) + oldValue;
        }
    }
}
