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
            // Preprocess keyframe values to Color if the xaml parser converts them to ISCB.
            foreach (var keyframe in this)
            {
                if (keyframe.Value is ISolidColorBrush colorBrush)
                {
                    keyframe.Value = colorBrush.Color;
                }
                else
                {
                    return Disposable.Empty;
                }
            }

            SolidColorBrush finalTarget;
            var targetVal = control.GetValue(Property);
            
            switch (targetVal)
            {
                case null:
                    finalTarget = new SolidColorBrush(Colors.Transparent);
                    break;
                case ImmutableSolidColorBrush immutableSolidColorBrush:
                    finalTarget = new SolidColorBrush(immutableSolidColorBrush.Color);
                    break;
                case SolidColorBrush target:
                    finalTarget = target;
                    break;
                case ISolidColorBrush target:
                    finalTarget = target as SolidColorBrush;
                    break;
                default:
                    return Disposable.Empty;
            }

            if (_colorAnimator == null)
                InitializeColorAnimator();

            return _colorAnimator.Apply(animation, finalTarget, clock ?? control.Clock, match, onComplete);
        }

        public override SolidColorBrush Interpolate(double p, SolidColorBrush o, SolidColorBrush n) => null;
    }
}
