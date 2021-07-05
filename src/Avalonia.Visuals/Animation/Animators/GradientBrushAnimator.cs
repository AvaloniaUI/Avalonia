using System;
using System.Collections.Generic;

using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;

#nullable enable

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="SolidColorBrush"/> values. 
    /// </summary>
    public class IGradientBrushAnimator : Animator<IGradientBrush?>
    {
        private static readonly RelativePointAnimator s_relativePointAnimator = new RelativePointAnimator();
        private static readonly DoubleAnimator s_doubleAnimator = new DoubleAnimator();

        public override IGradientBrush? Interpolate(double progress, IGradientBrush? oldValue, IGradientBrush? newValue)
        {
            if (oldValue is null || newValue is null
                || oldValue.GradientStops.Count != newValue.GradientStops.Count)
            {
                return progress >= 0.5 ? newValue : oldValue;
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
                    return progress >= 0.5 ? newValue : oldValue;
            }
        }

        public override IDisposable BindAnimation(Animatable control, IObservable<IGradientBrush?> instance)
        {
            return control.Bind((AvaloniaProperty<IBrush?>)Property, instance, BindingPriority.Animation);
        }

        private IReadOnlyList<ImmutableGradientStop> InterpolateStops(double progress, IReadOnlyList<IGradientStop> oldValue, IReadOnlyList<IGradientStop> newValue)
        {
            var stops = new ImmutableGradientStop[oldValue.Count];
            for (int index = 0; index < oldValue.Count; index++)
            {
                stops[index] = new ImmutableGradientStop(
                    s_doubleAnimator.Interpolate(progress, oldValue[index].Offset, newValue[index].Offset),
                    ColorAnimator.InterpolateCore(progress, oldValue[index].Color, newValue[index].Color));
            }
            return stops;
        }

        internal static IGradientBrush ConvertSolidColorBrushToGradient(IGradientBrush gradientBrush, ISolidColorBrush solidColorBrush)
        {
            switch (gradientBrush)
            {
                case IRadialGradientBrush oldRadial:
                    return new ImmutableRadialGradientBrush(
                        CreateStopsFromSolidColorBrush(solidColorBrush, oldRadial), solidColorBrush.Opacity,
                        oldRadial.SpreadMethod, oldRadial.Center, oldRadial.GradientOrigin, oldRadial.Radius);

                case IConicGradientBrush oldConic:
                    return new ImmutableConicGradientBrush(
                        CreateStopsFromSolidColorBrush(solidColorBrush, oldConic), solidColorBrush.Opacity,
                        oldConic.SpreadMethod, oldConic.Center, oldConic.Angle);

                case ILinearGradientBrush oldLinear:
                    return new ImmutableLinearGradientBrush(
                        CreateStopsFromSolidColorBrush(solidColorBrush, oldLinear), solidColorBrush.Opacity,
                        oldLinear.SpreadMethod, oldLinear.StartPoint, oldLinear.EndPoint);

                default:
                    throw new NotSupportedException($"Gradient of type {gradientBrush?.GetType()} is not supported");
            }

            static IReadOnlyList<ImmutableGradientStop> CreateStopsFromSolidColorBrush(ISolidColorBrush solidColorBrush, IGradientBrush baseGradient)
            {
                var stops = new ImmutableGradientStop[baseGradient.GradientStops.Count];
                for (int index = 0; index < baseGradient.GradientStops.Count; index++)
                {
                    stops[index] = new ImmutableGradientStop(baseGradient.GradientStops[index].Offset, solidColorBrush.Color);
                }
                return stops;
            }
        }
    }
}
