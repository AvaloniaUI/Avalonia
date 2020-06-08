using System;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Avalonia.Animation.Animators
{
    public class TransformOperationsAnimator : Animator<ITransform>
    {
        public TransformOperationsAnimator()
        {
            Validate = ValidateTransform;
        }

        private void ValidateTransform(AnimatorKeyFrame kf)
        {
            if (!(kf.Value is TransformOperations))
            {
                throw new InvalidOperationException($"All keyframes must be of type {typeof(TransformOperations)}.");
            }
        }

        public override ITransform Interpolate(double progress, ITransform oldValue, ITransform newValue)
        {
            var oldTransform = Cast(oldValue);
            var newTransform = Cast(newValue);

            return TransformOperations.Interpolate(oldTransform, newTransform, progress);
        }

        private static TransformOperations Cast(ITransform value)
        {
            return value as TransformOperations ?? TransformOperations.Identity;
        }
    }
}
