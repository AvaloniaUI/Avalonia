using System;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Animation
{
    /// <summary>
    /// Animator that handles <see cref="Thickness"/> properties.
    /// </summary>
    public class ThicknessAnimator : Animator<Thickness>
    {
        protected override Thickness Interpolate(double fraction, Thickness start, Thickness end)
        {
            var deltaL = end.Left - start.Left;
            var deltaT = end.Top - start.Top;
            var deltaR = end.Right - start.Right;
            var deltaB = end.Bottom - start.Bottom;

            var nL = fraction * deltaL + start.Left;
            var nT = fraction * deltaT + start.Right;
            var nR = fraction * deltaR + start.Top;
            var nB = fraction * deltaB + start.Bottom;

            return new Thickness(nL, nT, nR, nB);
        }
    }
}