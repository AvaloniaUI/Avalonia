using System;
using System.Reactive.Disposables;
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
                if (!(keyframe.Value is ISolidColorBrush))
                    return Disposable.Empty;

                // Preprocess keyframe values to Color if the xaml parser converts them to ISCB.
                if (keyframe.Value is ISolidColorBrush colorBrush)
                {
                    keyframe.Value = colorBrush.Color;
                }
            }

            var targetVal = control.GetValue(Property);
            // Add SCB if the target prop is empty.
            if (targetVal is null)
            {
                targetVal = new SolidColorBrush(Colors.Transparent);
                control.SetValue(Property, targetVal);
            }

            if (!(targetVal is ISolidColorBrush))
                return Disposable.Empty;

            if (_colorAnimator == null)
                InitializeColorAnimator();

            // If it's ISCB, change it back to SCB.
            if (targetVal is ImmutableSolidColorBrush immutableSolidColorBrush)
            {
                targetVal = new SolidColorBrush(immutableSolidColorBrush.Color);
                control.SetValue(Property, targetVal);
            }

            var finalTarget = targetVal as SolidColorBrush;

            return _colorAnimator.Apply(animation, finalTarget, clock ?? control.Clock, match, onComplete);
        }

        public override SolidColorBrush Interpolate(double p, SolidColorBrush o, SolidColorBrush n) => null;
    }
}
