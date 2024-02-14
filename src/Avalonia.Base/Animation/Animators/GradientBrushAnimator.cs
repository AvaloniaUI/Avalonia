using System;
using System.Collections.Generic;

using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Transformation;

#nullable enable

namespace Avalonia.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="SolidColorBrush"/> values. 
    /// </summary>
    internal class GradientBrushAnimator : Animator<IGradientBrush?>
    {
        private static readonly RelativePointAnimator s_relativePointAnimator = new RelativePointAnimator();
        private static readonly RelativeScalarAnimator s_relativeScalarAnimator = new RelativeScalarAnimator();
        private static readonly DoubleAnimator s_doubleAnimator = new DoubleAnimator();

        public override IGradientBrush? Interpolate(double progress, IGradientBrush? oldValue, IGradientBrush? newValue)
        {
            if (oldValue is null || newValue is null)
            {
                return progress >= 0.5 ? newValue : oldValue;
            }

            switch (oldValue)
            {
                case IRadialGradientBrush oldRadial when newValue is IRadialGradientBrush newRadial:
                    return new ImmutableRadialGradientBrush(
                        InterpolateStops(progress, oldValue.GradientStops, newValue.GradientStops),
                        s_doubleAnimator.Interpolate(progress, oldValue.Opacity, newValue.Opacity),
                        InterpolateTransform(progress, oldValue.Transform, newValue.Transform),
                        s_relativePointAnimator.Interpolate(progress, oldValue.TransformOrigin, newValue.TransformOrigin),
                        oldValue.SpreadMethod,
                        s_relativePointAnimator.Interpolate(progress, oldRadial.Center, newRadial.Center),
                        s_relativePointAnimator.Interpolate(progress, oldRadial.GradientOrigin, newRadial.GradientOrigin),
                        s_relativeScalarAnimator.Interpolate(progress, oldRadial.RadiusX, newRadial.RadiusX),
                        s_relativeScalarAnimator.Interpolate(progress, oldRadial.RadiusY, newRadial.RadiusY)
                        );

                case IConicGradientBrush oldConic when newValue is IConicGradientBrush newConic:
                    return new ImmutableConicGradientBrush(
                        InterpolateStops(progress, oldValue.GradientStops, newValue.GradientStops),
                        s_doubleAnimator.Interpolate(progress, oldValue.Opacity, newValue.Opacity),
                        InterpolateTransform(progress, oldValue.Transform, newValue.Transform),
                        s_relativePointAnimator.Interpolate(progress, oldValue.TransformOrigin, newValue.TransformOrigin),
                        oldValue.SpreadMethod,
                        s_relativePointAnimator.Interpolate(progress, oldConic.Center, newConic.Center),
                        s_doubleAnimator.Interpolate(progress, oldConic.Angle, newConic.Angle));

                case ILinearGradientBrush oldLinear when newValue is ILinearGradientBrush newLinear:
                    return new ImmutableLinearGradientBrush(
                        InterpolateStops(progress, oldValue.GradientStops, newValue.GradientStops),
                        s_doubleAnimator.Interpolate(progress, oldValue.Opacity, newValue.Opacity),
                        InterpolateTransform(progress, oldValue.Transform, newValue.Transform),
                        s_relativePointAnimator.Interpolate(progress, oldValue.TransformOrigin, newValue.TransformOrigin),
                        oldValue.SpreadMethod,
                        s_relativePointAnimator.Interpolate(progress, oldLinear.StartPoint, newLinear.StartPoint),
                        s_relativePointAnimator.Interpolate(progress, oldLinear.EndPoint, newLinear.EndPoint));

                default:
                    return progress >= 0.5 ? newValue : oldValue;
            }
        }

        public override IDisposable BindAnimation(Animatable control, IObservable<IGradientBrush?> instance)
        {
            if (Property is null)
            {
                throw new InvalidOperationException("Animator has no property specified.");
            }

            return control.Bind((AvaloniaProperty<IBrush?>)Property, instance, BindingPriority.Animation);
        }

        private static ImmutableTransform? InterpolateTransform(double progress,
            ITransform? oldTransform, ITransform? newTransform)
        {
            if (oldTransform is TransformOperations oldTransformOperations
                && newTransform is TransformOperations newTransformOperations)
            {

                return new ImmutableTransform(TransformOperations
                    .Interpolate(oldTransformOperations, newTransformOperations, progress).Value);
            }

            if (oldTransform is not null)
            {
                return new ImmutableTransform(oldTransform.Value);
            }
            
            return null;
        }
    
        private static IReadOnlyList<ImmutableGradientStop> InterpolateStops(double progress, IReadOnlyList<IGradientStop> oldValue, IReadOnlyList<IGradientStop> newValue)
        {
            var resultCount = Math.Max(oldValue.Count, newValue.Count);
            var stops = new ImmutableGradientStop[resultCount];

            for (int index = 0, oldIndex = 0, newIndex = 0; index < resultCount; index++)
            {
                stops[index] = new ImmutableGradientStop(
                    s_doubleAnimator.Interpolate(progress, oldValue[oldIndex].Offset, newValue[newIndex].Offset),
                    ColorAnimator.InterpolateCore(progress, oldValue[oldIndex].Color, newValue[newIndex].Color));

                if (oldIndex < oldValue.Count - 1)
                {
                    oldIndex++;
                }

                if (newIndex < newValue.Count - 1)
                {
                    newIndex++;
                }
            }
            
            return stops;
        }

        internal static IGradientBrush ConvertSolidColorBrushToGradient(IGradientBrush gradientBrush, ISolidColorBrush solidColorBrush)
        {
            switch (gradientBrush)
            {
                case IRadialGradientBrush oldRadial:
                    return new ImmutableRadialGradientBrush(
                        CreateStopsFromSolidColorBrush(solidColorBrush, oldRadial.GradientStops), solidColorBrush.Opacity,
                        oldRadial.Transform is { } ? new ImmutableTransform(oldRadial.Transform.Value) : null,
                        oldRadial.TransformOrigin,
                        oldRadial.SpreadMethod, oldRadial.Center, oldRadial.GradientOrigin, oldRadial.Radius);

                case IConicGradientBrush oldConic:
                    return new ImmutableConicGradientBrush(
                        CreateStopsFromSolidColorBrush(solidColorBrush, oldConic.GradientStops), solidColorBrush.Opacity,
                        oldConic.Transform is { } ? new ImmutableTransform(oldConic.Transform.Value) : null,
                        oldConic.TransformOrigin,
                        oldConic.SpreadMethod, oldConic.Center, oldConic.Angle);

                case ILinearGradientBrush oldLinear:
                    return new ImmutableLinearGradientBrush(
                        CreateStopsFromSolidColorBrush(solidColorBrush, oldLinear.GradientStops), solidColorBrush.Opacity,
                        oldLinear.Transform is { } ? new ImmutableTransform(oldLinear.Transform.Value) : null,
                        oldLinear.TransformOrigin,
                        oldLinear.SpreadMethod, oldLinear.StartPoint, oldLinear.EndPoint);

                default:
                    throw new NotSupportedException($"Gradient of type {gradientBrush?.GetType()} is not supported");
            }

            static IReadOnlyList<ImmutableGradientStop> CreateStopsFromSolidColorBrush(ISolidColorBrush solidColorBrush, IReadOnlyList<IGradientStop> baseStops)
            {
                var stops = new ImmutableGradientStop[baseStops.Count];
                for (int index = 0; index < baseStops.Count; index++)
                {
                    stops[index] = new ImmutableGradientStop(baseStops[index].Offset, solidColorBrush.Color);
                }
                return stops;
            }
        }
    }
}
