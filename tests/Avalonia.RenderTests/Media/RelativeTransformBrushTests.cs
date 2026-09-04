using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Xunit;

namespace Avalonia.Skia.RenderTests;

/// <summary>
/// <see cref="IRelativeTransformBrush.RelativeTransform"/> applies in the unit
/// space of the painted bounds, before the absolute <see cref="IBrush.Transform"/>,
/// so one brush instance serves consumers of different sizes.
/// </summary>
public class RelativeTransformBrushTests : TestBase
{
    private static readonly ImmutableGradientStop[] s_stops =
    {
        new(0, Colors.Red),
        new(1, Colors.Blue),
    };

    // A rotation about an off-center unit-space point: bounds-dependent once
    // conjugated, and asymmetric enough that even a center-symmetric radial
    // gradient shows it (a rotation about the center would be invisible there).
    private static readonly Matrix s_unitMatrix =
        Matrix.CreateTranslation(-0.3, -0.7)
        * Matrix.CreateRotation(Matrix.ToRadians(30))
        * Matrix.CreateTranslation(0.3, 0.7);

    public RelativeTransformBrushTests()
        : base(@"Media\RelativeTransformBrush")
    {
    }

    private static Canvas Scene(params Control[] children)
    {
        var canvas = new Canvas
        {
            Width = 200,
            Height = 200,
            Background = Brushes.White,
        };

        foreach (var child in children)
            canvas.Children.Add(child);

        return canvas;
    }

    private static Border Filled(IBrush brush, double left, double top, double width, double height) =>
        new()
        {
            Background = brush,
            Width = width,
            Height = height,
            [Canvas.LeftProperty] = left,
            [Canvas.TopProperty] = top,
        };

    [Fact]
    public async Task Linear_Gradient_Rotated_In_Unit_Space()
    {
        var brush = new ImmutableLinearGradientBrush(
            s_stops, 1, null, null, GradientSpreadMethod.Pad, null, null,
            new ImmutableTransform(s_unitMatrix));

        await RenderToFile(Scene(Filled(brush, 40, 70, 120, 60)));
        CompareImages();
    }

    [Fact]
    public async Task Radial_Gradient_Rotated_In_Unit_Space()
    {
        var brush = new ImmutableRadialGradientBrush(
            s_stops, 1, null, null, GradientSpreadMethod.Pad, null, null, null, null,
            new ImmutableTransform(s_unitMatrix));

        await RenderToFile(Scene(Filled(brush, 40, 70, 120, 60)));
        CompareImages();
    }

    [Fact]
    public async Task Shared_Brush_On_Two_Different_Bounds()
    {
        // The gradient must follow each rect's own bounds, so the two fills
        // differ in shape while sharing one brush instance.
        var shared = new ImmutableLinearGradientBrush(
            s_stops, 1, null, null, GradientSpreadMethod.Pad, null, null,
            new ImmutableTransform(s_unitMatrix));

        await RenderToFile(
            Scene(
                Filled(shared, 40, 30, 120, 60),
                Filled(shared, 30, 110, 60, 90)));
        CompareImages();
    }

    [Fact]
    public async Task Relative_Transform_Applies_Before_Absolute()
    {
        // A scale, not a translation: it does not commute with the relative
        // rotation, so applying the two the other way round moves the image far
        // enough to fail this golden. A translation stayed inside the tolerance.
        var brush = new ImmutableLinearGradientBrush(
            s_stops, 1, new ImmutableTransform(Matrix.CreateScale(0.4, 0.4)), null,
            GradientSpreadMethod.Pad, null, null,
            new ImmutableTransform(s_unitMatrix));

        await RenderToFile(Scene(Filled(brush, 40, 70, 120, 60)));
        CompareImages();
    }
}
