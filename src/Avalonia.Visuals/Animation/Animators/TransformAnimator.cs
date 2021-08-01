using System;
using System.Reactive.Disposables;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Transformation;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Transform"/> properties.
    /// </summary>
    public class TransformAnimator : Animator<double>
    {
        DoubleAnimator _doubleAnimator;

        /// <inheritdoc/>
        public override IDisposable Apply(Animation animation, Animatable control, IClock clock, IObservable<bool> obsMatch, Action onComplete)
        {
            var ctrl = (Visual)control;

            // Check if the Target Property is Transform derived.
            if (typeof(Transform).IsAssignableFrom(Property.OwnerType))
            {
                if (ctrl.RenderTransform is TransformOperations)
                {
                    // HACK: This animator cannot reasonably animate CSS transforms at the moment.
                    return Disposable.Empty;
                }

                if (ctrl.RenderTransform == null)
                {
                    var normalTransform = new TransformGroup();

                    // Add the transforms according to MS Expression Blend's 
                    // default RenderTransform order.

                    normalTransform.Children.Add(new ScaleTransform());
                    normalTransform.Children.Add(new SkewTransform());
                    normalTransform.Children.Add(new RotateTransform());
                    normalTransform.Children.Add(new TranslateTransform());

                    ctrl.RenderTransform = normalTransform;
                }

                var renderTransformType = ctrl.RenderTransform.GetType();

                if (_doubleAnimator == null)
                {
                    _doubleAnimator = new DoubleAnimator();

                    foreach (AnimatorKeyFrame keyframe in this)
                    {
                        _doubleAnimator.Add(keyframe);
                    }

                    _doubleAnimator.Property = Property;
                }

                // It's a transform object so let's target that.
                if (renderTransformType == Property.OwnerType)
                {
                    return _doubleAnimator.Apply(animation, (Transform) ctrl.RenderTransform, clock ?? control.Clock, obsMatch, onComplete);
                }
                // It's a TransformGroup and try finding the target there.
                else if (renderTransformType == typeof(TransformGroup))
                {
                    foreach (Transform transform in ((TransformGroup)ctrl.RenderTransform).Children)
                    {
                        if (transform.GetType() == Property.OwnerType)
                        {
                            return _doubleAnimator.Apply(animation, transform, clock ?? control.Clock, obsMatch, onComplete);
                        }
                    }
                }

                Logger.TryGet(LogEventLevel.Warning, LogArea.Animations)?.Log(
                    control,
                    $"Cannot find the appropriate transform: \"{Property.OwnerType}\" in {control}.");
            }
            else
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Animations)?.Log(
                    control,
                    $"Cannot apply animation: Target property owner {Property.OwnerType} is not a Transform object.");
            }
            return null;
        }

        internal ImmutableTransform InterpolateMatrix(double progress, ITransform oldValue, ITransform newValue)
        {    
            var from = oldValue.Value;
            var to = newValue.Value;
            var matrix = new Matrix(
                ((to.M11 - from.M11) * progress) + from.M11,
                ((to.M12 - from.M12) * progress) + from.M12,
                ((to.M21 - from.M21) * progress) + from.M21,
                ((to.M22 - from.M22) * progress) + from.M22,
                ((to.M31 - from.M31) * progress) + from.M31,
                ((to.M32 - from.M32) * progress) + from.M32);

            return new ImmutableTransform(matrix);
        }

        /// <inheritdocs/> 
        public override double Interpolate(double p, double o, double n) => 0;
    }
}
