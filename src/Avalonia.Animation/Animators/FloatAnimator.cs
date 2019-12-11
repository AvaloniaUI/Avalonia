// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="float"/> properties.
    /// </summary>
    public class FloatAnimator : Animator<float>
    {
        /// <inheritdocs/>
        public override float Interpolate(double progress, float oldValue, float newValue)
        {
            return (float)(((newValue - oldValue) * progress) + oldValue);
        }
    }
}
