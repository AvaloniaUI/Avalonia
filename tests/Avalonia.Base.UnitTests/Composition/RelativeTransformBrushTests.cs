using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Base.UnitTests.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

/// <summary>
/// <see cref="IBrush.RelativeTransform"/> transport: the value set on the client brush must reach
/// the server-side counterpart the backend reads at draw time, for every brush kind, both through
/// the mutable Media brushes and through the composition brushes.
/// </summary>
public class RelativeTransformBrushTests : ScopedTestBase
{
    private static readonly Matrix s_matrix = Matrix.CreateRotation(0.5) * Matrix.CreateTranslation(0.25, 0.5);

    [Fact]
    public void Composition_Solid_Color_Brush_Relative_Transform_Should_Reach_The_Server()
        => AssertCompositionBrushReachesServer(c => c.CreateSolidColorBrush(Colors.Red));

    [Fact]
    public void Composition_Linear_Gradient_Brush_Relative_Transform_Should_Reach_The_Server()
        => AssertCompositionBrushReachesServer(c => c.CreateLinearGradientBrush());

    [Fact]
    public void Composition_Radial_Gradient_Brush_Relative_Transform_Should_Reach_The_Server()
        => AssertCompositionBrushReachesServer(c => c.CreateRadialGradientBrush());

    [Fact]
    public void Composition_Conic_Gradient_Brush_Relative_Transform_Should_Reach_The_Server()
        => AssertCompositionBrushReachesServer(c => c.CreateConicGradientBrush());

    [Fact]
    public void Solid_Color_Brush_Relative_Transform_Should_Reach_The_Server()
        => AssertBrushReachesServer(new SolidColorBrush(Colors.Red));

    [Fact]
    public void Linear_Gradient_Brush_Relative_Transform_Should_Reach_The_Server()
        => AssertBrushReachesServer(new LinearGradientBrush { GradientStops = { new GradientStop(Colors.Red, 0) } });

    [Fact]
    public void Radial_Gradient_Brush_Relative_Transform_Should_Reach_The_Server()
        => AssertBrushReachesServer(new RadialGradientBrush { GradientStops = { new GradientStop(Colors.Red, 0) } });

    [Fact]
    public void Conic_Gradient_Brush_Relative_Transform_Should_Reach_The_Server()
        => AssertBrushReachesServer(new ConicGradientBrush { GradientStops = { new GradientStop(Colors.Red, 0) } });

    [Fact]
    public void Image_Brush_Relative_Transform_Should_Reach_The_Server()
        => AssertBrushReachesServer(new ImageBrush());

    [Fact]
    public void Drawing_Brush_Relative_Transform_Should_Reach_The_Server()
        => AssertBrushReachesServer(new DrawingBrush());

    [Fact]
    public void Changing_Relative_Transform_Raises_Invalidated()
    {
        var target = new SolidColorBrush();

        RenderResourceTestHelper.AssertResourceInvalidation(
            target, () => target.RelativeTransform = new ImmutableTransform(s_matrix));
    }

    [Fact]
    public void Relative_Transform_Is_Referenced_On_The_Compositor()
    {
        using var services = new CompositorTestServices();

        var transform = new RotateTransform(45);
        var brush = new SolidColorBrush(Colors.Red) { RelativeTransform = transform };

        ((ICompositionRenderResource)brush).AddRefOnCompositor(services.Compositor);
        try
        {
            services.RunJobs();
            Assert.NotNull(((ICompositorSerializable)transform).TryGetServer(services.Compositor));
        }
        finally
        {
            ((ICompositionRenderResource)brush).ReleaseOnCompositor(services.Compositor);
        }

        Assert.Null(((ICompositorSerializable)transform).TryGetServer(services.Compositor));
    }

    private static void AssertCompositionBrushReachesServer(Func<Compositor, CompositionBrush> factory)
    {
        using var services = new CompositorTestServices();
        var brush = factory(services.Compositor);

        brush.RelativeTransform = new ImmutableTransform(s_matrix);
        services.RunJobs();

        Assert.Equal(s_matrix, ((IBrush)brush.Server).RelativeTransform!.Value);
    }

    private static void AssertBrushReachesServer(Brush brush)
    {
        using var services = new CompositorTestServices();

        brush.RelativeTransform = new ImmutableTransform(s_matrix);

        var resource = (ICompositionRenderResource<IBrush>)brush;
        ((ICompositionRenderResource)brush).AddRefOnCompositor(services.Compositor);
        try
        {
            services.RunJobs();

            var server = resource.GetForCompositor(services.Compositor);
            Assert.Equal(s_matrix, server.RelativeTransform!.Value);
        }
        finally
        {
            ((ICompositionRenderResource)brush).ReleaseOnCompositor(services.Compositor);
        }
    }
}
