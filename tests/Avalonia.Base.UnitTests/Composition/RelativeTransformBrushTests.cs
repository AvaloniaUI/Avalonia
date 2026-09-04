using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

/// <summary>
/// <see cref="IRelativeTransformBrush.RelativeTransform"/> transport: the value
/// set on the client brush must reach the server-side counterpart the backend
/// reads at draw time, for both the mutable Media brush and the composition
/// brush.
/// </summary>
public class RelativeTransformBrushTests : ScopedTestBase
{
    private static readonly Matrix s_matrix = Matrix.CreateRotation(0.5) * Matrix.CreateTranslation(0.25, 0.5);

    [Fact]
    public void Composition_Gradient_Brush_Relative_Transform_Should_Reach_The_Server()
    {
        using var services = new CompositorTestServices();
        var brush = services.Compositor.CreateLinearGradientBrush();

        brush.RelativeTransform = new ImmutableTransform(s_matrix);
        services.RunJobs();

        var server = Assert.IsAssignableFrom<IRelativeTransformBrush>((object)brush.Server);
        Assert.Equal(s_matrix, server.RelativeTransform!.Value);
    }

    [Fact]
    public void Mutable_Gradient_Brush_Relative_Transform_Should_Reach_The_Server()
    {
        using var services = new CompositorTestServices();

        var brush = new LinearGradientBrush
        {
            GradientStops =
            {
                new GradientStop(Colors.Red, 0),
                new GradientStop(Colors.Blue, 1),
            },
            RelativeTransform = new ImmutableTransform(s_matrix),
        };

        var resource = (ICompositionRenderResource<IBrush>)brush;
        ((ICompositionRenderResource)brush).AddRefOnCompositor(services.Compositor);
        try
        {
            services.RunJobs();

            var server = Assert.IsAssignableFrom<IRelativeTransformBrush>(
                (object)resource.GetForCompositor(services.Compositor));
            Assert.Equal(s_matrix, server.RelativeTransform!.Value);
        }
        finally
        {
            ((ICompositionRenderResource)brush).ReleaseOnCompositor(services.Compositor);
        }
    }
}
