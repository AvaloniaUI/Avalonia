using System;
using Avalonia.Data;
using Avalonia.Reactive;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Transform"/> properties.
    /// </summary>
    internal class TransformAnimator : Animator<double>
    {
        private Transform? _targetTransform;
        private AvaloniaProperty? _targetProperty;
        private LightweightSubject<double>? _subject2;
        private DisposeAnimationInstanceSubject<double>? _subject1;

        /// <inheritdoc/>
        public override IDisposable? Apply(Animation animation, Animatable control, IClock? clock, IObservable<bool> match, Action? onComplete)
        {
            var ctrl = (Visual)control;

            if (Property is null)
            {
                throw new InvalidOperationException("Animator has no property specified.");
            }

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
                    normalTransform.Children.Add(new Rotate3DTransform());

                    ctrl.RenderTransform = normalTransform;
                }

                var renderTransformType = ctrl.RenderTransform.GetType();

                _targetProperty = Property;

                // It's a transform object so let's target that.
                if (renderTransformType == Property.OwnerType)
                {
                    _targetTransform = (Transform)ctrl.RenderTransform;
                }

                // It's a TransformGroup and try finding the target there.
                if (renderTransformType == typeof(TransformGroup))
                {
                    foreach (var transform in ((TransformGroup)ctrl.RenderTransform).Children)
                    {
                        if (transform.GetType() == Property.OwnerType)
                        {                    
                            _targetTransform = transform;
                            break;
                        }
                    }
                }

                if (_targetTransform is not null)
                {
                    _subject1 = new DisposeAnimationInstanceSubject<double>(this, animation,
                        control, clock, onComplete);
                    
                    _subject2 = new LightweightSubject<double>();
                    
                    var sideBinding = _targetTransform.Bind((AvaloniaProperty<double>)_targetProperty,
                        _subject2, BindingPriority.Animation);
                    
                    return new CompositeDisposable(match.Subscribe(_subject1), _subject1, sideBinding);
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

        /// <inheritdoc/>  
        public override double Interpolate(double progress, double oldValue, double newValue)
        {
            _subject2?.OnNext(((newValue - oldValue) * progress) + oldValue);
            return default;
        }

    }
}
