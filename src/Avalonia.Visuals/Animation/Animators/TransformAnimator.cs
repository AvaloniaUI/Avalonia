using System;
using Avalonia.Logging;
using Avalonia.Media;

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
                    return _doubleAnimator.Apply(animation, ctrl.RenderTransform, clock ?? control.Clock, obsMatch, onComplete);
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

                Logger.Warning(
                    LogArea.Animations,
                    control,
                    $"Cannot find the appropriate transform: \"{Property.OwnerType}\" in {control}.");
            }
            else
            {
                Logger.Error(
                    LogArea.Animations,
                    control,
                    $"Cannot apply animation: Target property owner {Property.OwnerType} is not a Transform object.");
            }
            return null;
        }

        /// <inheritdocs/> 
        public override double Interpolate(double p, double o, double n) => 0;
    }
}
