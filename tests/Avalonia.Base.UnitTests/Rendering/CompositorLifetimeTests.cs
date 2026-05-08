using Avalonia.Rendering.Composition;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class CompositorLifetimeTests : CompositorTestsBase
{
    [Fact]
    public void InvalidateVisual_Does_Not_Update_RenderingTarget_When_Rendering_Stopped()
    {
        using var services = new CompositorTestServices(new Size(200, 200));

        var presentationSource = services.TopLevel.GetPresentationSource();
        Assert.NotNull(presentationSource);

        var compositionTarget = ((CompositingRenderer)presentationSource.Renderer).CompositionTarget;
        Assert.True(compositionTarget.IsEnabled);
        Assert.Equal(new Size(200, 200), compositionTarget.Size);

        // Stop rendering and invalidate a visual: this should not result in an update
        services.TopLevel.StopRendering();
        ((CompositorTestServices.TopLevelImpl)services.TopLevel.PlatformImpl!).ClientSize = new Size(300, 300);
        services.TopLevel.InvalidateVisual();
        services.RunJobs();

        Assert.Equal(new Size(200, 200), compositionTarget.Size);

        // Check that restarting rendering re-queues the pending invalidation
        services.TopLevel.StartRendering();
        services.RunJobs();

        Assert.Equal(new Size(300, 300), compositionTarget.Size);
    }
}
