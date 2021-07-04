using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="SolidColorBrush"/> values. 
    /// </summary>
    public class IGradientBrushAnimator : Animator<IGradientBrush>
    {
        private static readonly RelativePointAnimator s_relativePointAnimator = new RelativePointAnimator();
        private static readonly DoubleAnimator s_doubleAnimator = new DoubleAnimator();

        public override IGradientBrush Interpolate(double progress, IGradientBrush oldValue, IGradientBrush newValue)
        {
            if (oldValue is null || newValue is null
                || oldValue.GradientStops.Count != oldValue.GradientStops.Count)
            {
                return progress >= 1 ? newValue : oldValue;
            }

            switch (oldValue)
            {
                case IRadialGradientBrush oldRadial when newValue is IRadialGradientBrush newRadial:
                    return new ImmutableRadialGradientBrush(
                        InterpolateStops(progress, oldValue.GradientStops, newValue.GradientStops),
                        s_doubleAnimator.Interpolate(progress, oldValue.Opacity, newValue.Opacity),
                        oldValue.SpreadMethod,
                        s_relativePointAnimator.Interpolate(progress, oldRadial.Center, newRadial.Center),
                        s_relativePointAnimator.Interpolate(progress, oldRadial.GradientOrigin, newRadial.GradientOrigin),
                        s_doubleAnimator.Interpolate(progress, oldRadial.Radius, newRadial.Radius));

                case IConicGradientBrush oldConic when newValue is IConicGradientBrush newConic:
                    return new ImmutableConicGradientBrush(
                        InterpolateStops(progress, oldValue.GradientStops, newValue.GradientStops),
                        s_doubleAnimator.Interpolate(progress, oldValue.Opacity, newValue.Opacity),
                        oldValue.SpreadMethod,
                        s_relativePointAnimator.Interpolate(progress, oldConic.Center, newConic.Center),
                        s_doubleAnimator.Interpolate(progress, oldConic.Angle, newConic.Angle));

                case ILinearGradientBrush oldLinear when newValue is ILinearGradientBrush newLinear:
                    return new ImmutableLinearGradientBrush(
                        InterpolateStops(progress, oldValue.GradientStops, newValue.GradientStops),
                        s_doubleAnimator.Interpolate(progress, oldValue.Opacity, newValue.Opacity),
                        oldValue.SpreadMethod,
                        s_relativePointAnimator.Interpolate(progress, oldLinear.StartPoint, newLinear.StartPoint),
                        s_relativePointAnimator.Interpolate(progress, oldLinear.EndPoint, newLinear.EndPoint));

                default:
                    return progress >= 1 ? newValue : oldValue;
            }
        }

        public override IDisposable BindAnimation(Animatable control, IObservable<IGradientBrush> instance)
        {
            return control.Bind((AvaloniaProperty<IBrush>)Property, instance, BindingPriority.Animation);
        }

        private IReadOnlyList<ImmutableGradientStop> InterpolateStops(double progress, IReadOnlyList<IGradientStop> oldValue, IReadOnlyList<IGradientStop> newValue)
        {
            // pool
            return oldValue
                .Zip(newValue, (f, s) => new ImmutableGradientStop(
                    s_doubleAnimator.Interpolate(progress, f.Offset, s.Offset),
                    ColorAnimator.InterpolateCore(progress, f.Color, s.Color)))
                .ToArray();
        }
    }
}
