namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="double"/> properties.
    /// </summary>
    internal class DoubleAnimator : Animator<double>
    {
        /// <inheritdoc/>
        public override double Interpolate(double progress, double oldValue, double newValue)
        {
            return ((newValue - oldValue) * progress) + oldValue;
        }
    }
}
