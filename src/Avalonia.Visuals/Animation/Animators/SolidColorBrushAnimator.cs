using System;
using System.Reactive.Disposables;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="SolidColorBrush"/>. 
    /// </summary>
    public class SolidColorBrushAnimator : Animator<SolidColorBrush>
    {
        ColorAnimator _colorAnimator;

        void InitializeColorAnimator()
        {
            _colorAnimator = new ColorAnimator();

            foreach (AnimatorKeyFrame keyframe in this)
            {
                _colorAnimator.Add(keyframe);
            }

            _colorAnimator.Property = SolidColorBrush.ColorProperty;
        }

        public override IDisposable Apply(Animation animation, Animatable control, IClock clock, IObservable<bool> match, Action onComplete)
        {
            foreach (var keyframe in this)
            {
                if (keyframe.Value as ISolidColorBrush == null)
                    return Disposable.Empty;

                // Preprocess keyframe values to Color if the xaml parser converts them to ISCB.
                if (keyframe.Value.GetType() == typeof(ImmutableSolidColorBrush))
                {
                    keyframe.Value = ((ImmutableSolidColorBrush)keyframe.Value).Color;
                }
            }

            // Add SCB if the target prop is empty.
            if (control.GetValue(Property) == null)
                control.SetValue(Property, new SolidColorBrush(Colors.Transparent));

            var targetVal = control.GetValue(Property);

            // Continue if target prop is not empty & is a SolidColorBrush derivative. 
            if (typeof(ISolidColorBrush).IsAssignableFrom(targetVal.GetType()))
            {
                if (_colorAnimator == null)
                    InitializeColorAnimator();

                SolidColorBrush finalTarget;

                // If it's ISCB, change it back to SCB.
                if (targetVal.GetType() == typeof(ImmutableSolidColorBrush))
                {
                    var col = (ImmutableSolidColorBrush)targetVal;
                    targetVal = new SolidColorBrush(col.Color);
                    control.SetValue(Property, targetVal);
                }

                finalTarget = targetVal as SolidColorBrush;

                return _colorAnimator.Apply(animation, finalTarget, clock ?? control.Clock, match, onComplete);
            }

            return Disposable.Empty;
        }

        public override SolidColorBrush Interpolate(double p, SolidColorBrush o, SolidColorBrush n) => null;
    }
}
