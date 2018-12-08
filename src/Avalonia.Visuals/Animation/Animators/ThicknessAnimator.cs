using System;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Thickness"/> properties.
    /// </summary>
    public class ThicknessAnimator : Animator<Thickness>
    {
        public override Thickness Interpolate(double progress, Thickness oldValue, Thickness newValue)
        {
            var deltaL = newValue.Left - oldValue.Left;
            var deltaT = newValue.Top - oldValue.Top;
            var deltaR = newValue.Right - oldValue.Right;
            var deltaB = newValue.Bottom - oldValue.Bottom;

            var nL = progress * deltaL + oldValue.Left;
            var nT = progress * deltaT + oldValue.Right;
            var nR = progress * deltaR + oldValue.Top;
            var nB = progress * deltaB + oldValue.Bottom;

            return new Thickness(nL, nT, nR, nB);
        }
    }
}