namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="float"/> properties.
    /// </summary>
    internal class FloatAnimator : Animator<float>
    {
        /// <inheritdoc/>
        public override float Interpolate(double progress, float oldValue, float newValue)
        {
            return (float)(((newValue - oldValue) * progress) + oldValue);
        }
    }
}
