using Avalonia.Animation.Animators;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;

/// <summary>
/// <see cref="IBrush.RelativeTransform"/> survives the snapshot every mutable brush takes of itself
/// when it is handed to a consumer that needs an immutable brush.
/// </summary>
public class RelativeTransformBrushTests
{
    private static readonly Matrix s_matrix = Matrix.CreateRotation(0.5) * Matrix.CreateTranslation(0.25, 0.5);

    [Fact]
    public void Solid_Color_Brush_ToImmutable_Carries_Relative_Transform()
        => AssertCarriedToImmutable(new SolidColorBrush(Colors.Red));

    [Fact]
    public void Linear_Gradient_Brush_ToImmutable_Carries_Relative_Transform()
        => AssertCarriedToImmutable(new LinearGradientBrush());

    [Fact]
    public void Radial_Gradient_Brush_ToImmutable_Carries_Relative_Transform()
        => AssertCarriedToImmutable(new RadialGradientBrush());

    [Fact]
    public void Conic_Gradient_Brush_ToImmutable_Carries_Relative_Transform()
        => AssertCarriedToImmutable(new ConicGradientBrush());

    [Fact]
    public void Image_Brush_ToImmutable_Carries_Relative_Transform()
        => AssertCarriedToImmutable(new ImageBrush());

    [Fact]
    public void Scene_Brush_Content_Carries_Relative_Transform()
    {
        using var app = UnitTestApplication.Start(new TestServices(renderInterface: Mock.Of<IPlatformRenderInterface>()));

        var brush = new DrawingBrush(new GeometryDrawing
        {
            Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
            Brush = Brushes.Red,
        })
        {
            RelativeTransform = new ImmutableTransform(s_matrix),
        };

        using var content = ((ISceneBrush)brush).CreateContent()!;

        Assert.Equal(s_matrix, content.RelativeTransform!.Value);
    }

    [Fact]
    public void Immutable_Solid_Color_Brushes_Differing_Only_In_Relative_Transform_Are_Not_Equal()
    {
        var first = new ImmutableSolidColorBrush(Colors.Red);
        var second = new ImmutableSolidColorBrush(Colors.Red, 1, null, new ImmutableTransform(s_matrix));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Animating_A_Gradient_Brush_Keeps_The_Relative_Transform()
    {
        var animator = new GradientBrushAnimator();
        var from = new ImmutableLinearGradientBrush(
            [], 1, null, null, GradientSpreadMethod.Pad, null, null, new ImmutableTransform(s_matrix));
        var to = new ImmutableLinearGradientBrush([]);

        var interpolated = animator.Interpolate(0.5, from, to);

        Assert.Equal(s_matrix, interpolated!.RelativeTransform!.Value);
    }

    private static void AssertCarriedToImmutable(Brush brush)
    {
        brush.RelativeTransform = new ImmutableTransform(s_matrix);

        var immutable = brush.ToImmutable();

        Assert.Equal(s_matrix, immutable.RelativeTransform!.Value);
    }
}
