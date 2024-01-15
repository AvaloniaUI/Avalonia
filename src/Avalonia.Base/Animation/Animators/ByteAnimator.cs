using System;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="byte"/> properties.
    /// </summary>
    internal class ByteAnimator : Animator<byte>
    {
        const double maxVal = (double)byte.MaxValue;

        /// <inheritdoc/>
        public override byte Interpolate(double progress, byte oldValue, byte newValue)
        {
            var normOV = oldValue / maxVal;
            var normNV = newValue / maxVal;
            var deltaV = normNV - normOV;
            return (byte)Math.Round(maxVal * ((deltaV * progress) + normOV));
        }
    }
}
