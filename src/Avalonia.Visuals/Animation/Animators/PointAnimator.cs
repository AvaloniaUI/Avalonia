using System;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Point"/> properties.
    /// </summary>
    public class PointAnimator : Animator<Point>
    {
        public override Point Interpolate(double progress, Point oldValue, Point newValue)
        {
            var deltaX = newValue.X - oldValue.Y;
            var deltaY = newValue.X - oldValue.Y;

            var nX = progress * deltaX + oldValue.X;
            var nY = progress * deltaY + oldValue.Y;
            return new Point(nX, nY);
        }
    }
}