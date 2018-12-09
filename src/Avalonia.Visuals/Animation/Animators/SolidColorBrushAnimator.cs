using System;
using System.Reactive.Disposables;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that interpolates <see cref="Color"/> through 
    /// gamma sRGB color space for better visual result.
    /// </summary>
    public class SolidColorBrushAnimator : Animator<SolidColorBrush>
    {
        ColorAnimator colorAnimator;

        void InitializeColorAnimator()
        {
            colorAnimator = new ColorAnimator();

            foreach (AnimatorKeyFrame keyframe in this)
            {
                colorAnimator.Add(keyframe);
            }
        }

        public override IDisposable Apply(Animation animation, Animatable control, IClock clock, IObservable<bool> match, Action onComplete)
        {
            var ctrl = (Visual)control;

            foreach (var keyframe in this)
            {
                if (keyframe.Value as ISolidColorBrush == null)
                    return Disposable.Empty;
            }

            if (control.GetValue(Property) == null)
                control.SetValue(Property, new SolidColorBrush(Colors.Transparent));

            var targetVal = control.GetValue(Property);

            if (typeof(ISolidColorBrush).IsAssignableFrom(targetVal.GetType()))
            {
                if (colorAnimator == null)
                    InitializeColorAnimator();

                SolidColorBrush finalTarget = new SolidColorBrush();

                if (targetVal.GetType() == typeof(ImmutableSolidColorBrush))
                {
                    var k = (ImmutableSolidColorBrush)targetVal;
                    finalTarget.Color = k.Color;
                }
                else
                {
                    finalTarget = targetVal as SolidColorBrush;
                }

                colorAnimator.Property = SolidColorBrush.ColorProperty;

                return colorAnimator.Apply(animation, finalTarget, clock ?? control.Clock, match, onComplete);
            }

            return Disposable.Empty;
        }

        public override SolidColorBrush Interpolate(double p, SolidColorBrush o, SolidColorBrush n) => null;
    }
}