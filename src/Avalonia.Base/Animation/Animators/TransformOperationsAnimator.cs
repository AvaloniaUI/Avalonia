using System;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Avalonia.Animation.Animators
{
    internal class TransformOperationsAnimator : Animator<TransformOperations>, IAvaloniaListItemValidator<AnimatorKeyFrame>
    {
        public TransformOperationsAnimator()
        {
            Validator = this;
        }

        public override TransformOperations Interpolate(double progress, TransformOperations oldValue, TransformOperations newValue)
        {
            var oldTransform = EnsureOperations(oldValue);
            var newTransform = EnsureOperations(newValue);

            return TransformOperations.Interpolate(oldTransform, newTransform, progress);
        }

        internal static TransformOperations EnsureOperations(ITransform value)
        {
            return value as TransformOperations ?? TransformOperations.Identity;
        }

        void IAvaloniaListItemValidator<AnimatorKeyFrame>.Validate(AnimatorKeyFrame item)
        {
            if (item.Value is not TransformOperations)
            {
                throw new InvalidOperationException($"{item.DebugDisplay} must have a value of type {typeof(TransformOperations)}.");
            }
        }
    }
}
