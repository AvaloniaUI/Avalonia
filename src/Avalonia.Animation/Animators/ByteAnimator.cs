// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="byte"/> properties.
    /// </summary>
    public class ByteAnimator : Animator<byte>
    {
        const double maxVal = (double)byte.MaxValue;

        /// <inheritdocs/>
        public override byte Interpolate(double progress, byte oldValue, byte newValue)
        {
            var normOV = oldValue / maxVal;
            var normNV = newValue / maxVal;
            var deltaV = normNV - normOV;
            return (byte)Math.Round(maxVal * ((deltaV * progress) + normOV));
        }
    }
}
