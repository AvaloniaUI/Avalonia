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

        public override IDisposable Apply(Animation animation, Animatable control, IClock clock, IObservable<bool> match, Action onComplete)
        {
            var ctrl = (Visual)control;

            foreach (var keyframe in this)
            {
                // Return if the keyframe value is not a SolidColorBrush
                if (keyframe.Value as ISolidColorBrush == null)
                {
                    return Disposable.Empty;
                }

                // Preprocess values to Color if the xaml parser converts them to ISCB
                if (keyframe.Value.GetType() == typeof(ImmutableSolidColorBrush))
                {
                    keyframe.Value = ((ImmutableSolidColorBrush)keyframe.Value).Color;
                }
            }

            // Make sure that the target property has SCB instead of the immutable one nor null.

            var targetVal = control.GetValue(Property);

            SolidColorBrush targetSCB = null;

            if (targetVal == null)
                targetSCB = new SolidColorBrush(Colors.Transparent);
            else if (typeof(ISolidColorBrush).IsAssignableFrom(targetVal.GetType()))
                targetSCB = new SolidColorBrush(((ISolidColorBrush)targetVal).Color);
            else
                return Disposable.Empty;

            control.SetValue(Property, targetSCB);

            if (colorAnimator == null)
            {
                colorAnimator = new ColorAnimator();

                foreach (AnimatorKeyFrame keyframe in this)
                {
                    colorAnimator.Add(keyframe);
                }

                colorAnimator.Property = SolidColorBrush.ColorProperty;
            }

            return colorAnimator.Apply(animation, targetSCB, clock ?? control.Clock, match, onComplete);
        }

        public override SolidColorBrush Interpolate(double p, SolidColorBrush o, SolidColorBrush n) => null;
    }
}
