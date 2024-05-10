using System;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases in a <see cref="double"/> value 
    /// using a overshooting cubic function.
    /// </summary>
    public class BackEaseIn : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return progress * (progress * progress - Math.Sin(progress * Math.PI)); 
        }
    }
}
