using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="ITransform"/> properties such as
    /// <see cref="Visual.RenderTransformProperty"/> by interpolating between
    /// <see cref="TransformOperations"/> values.
    /// </summary>
    internal class ITransformAnimator : Animator<ITransform?>
    {
        public override ITransform? Interpolate(double progress, ITransform? oldValue, ITransform? newValue)
        {
            var from = EnsureOperations(oldValue);
            var to = EnsureOperations(newValue);
            return TransformOperations.Interpolate(from, to, progress);
        }

        private static TransformOperations EnsureOperations(ITransform? value)
            => value as TransformOperations ?? TransformOperations.Identity;
    }
}
