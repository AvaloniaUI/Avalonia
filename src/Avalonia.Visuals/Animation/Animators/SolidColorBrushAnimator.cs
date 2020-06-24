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
        private ColorAnimator _colorAnimator;

        private void InitializeColorAnimator()
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

            SolidColorBrush finalTarget;
            var targetVal = control.GetValue(Property);
            if (targetVal is null)
            {
                finalTarget = new SolidColorBrush(Colors.Transparent);
                control.SetValue(Property, finalTarget);
            }
            else if (targetVal is ImmutableSolidColorBrush immutableSolidColorBrush)
            {
                finalTarget = new SolidColorBrush(immutableSolidColorBrush.Color);
                control.SetValue(Property, finalTarget);
            }
            else if (!(targetVal is ISolidColorBrush))
            {
                return Disposable.Empty;
            }
            else
            {
                finalTarget = targetVal as SolidColorBrush;
            }

            if (_colorAnimator == null)
                InitializeColorAnimator();

            return _colorAnimator.Apply(animation, finalTarget, clock ?? control.Clock, match, onComplete);
        }

        public override SolidColorBrush Interpolate(double p, SolidColorBrush o, SolidColorBrush n) => null;
    }
}
