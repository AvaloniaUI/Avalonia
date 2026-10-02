using Avalonia.Controls;
using Avalonia.Media;
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

    [Fact]
    public void Visuals_Removed_While_Rendering_Stopped_Are_Not_Kept_By_Renderer()
    {
        using var services = new CompositorCanvas();

        var kept = new Border
        {
            Background = Brushes.Red, Width = 20, Height = 10,
            [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50
        };
        services.Canvas.Children.Add(kept);
        services.RunJobs();

        services.TopLevel.StopRendering();

        var removedChild = new Border { Background = Brushes.Blue };
        var removed = new Border
        {
            Child = removedChild, Width = 20, Height = 10,
            [Canvas.LeftProperty] = 100, [Canvas.TopProperty] = 50
        };
        services.Canvas.Children.Add(removed);
        kept.Background = Brushes.Green;
        services.RunJobs();

        Assert.True(services.Renderer.HasPendingUpdateForUnitTests(removed));
        Assert.True(services.Renderer.HasPendingUpdateForUnitTests(removedChild));

        services.Canvas.Children.Remove(removed);
        services.RunJobs();

        // Nothing drains the renderer while it is stopped, so it must not hold on to visuals that left the tree.
        Assert.False(services.Renderer.HasPendingUpdateForUnitTests(removed));
        Assert.False(services.Renderer.HasPendingUpdateForUnitTests(removedChild));

        // Visuals still in the tree stay queued until rendering resumes.
        Assert.True(services.Renderer.HasPendingUpdateForUnitTests(kept));
        Assert.True(services.Renderer.HasPendingUpdateForUnitTests(services.Canvas));

        services.TopLevel.StartRendering();
        services.RunJobs();

        Assert.False(services.Renderer.HasPendingUpdateForUnitTests(kept));
        Assert.False(services.Renderer.HasPendingUpdateForUnitTests(services.Canvas));
        services.AssertHitTest(35, 55, null, kept);
        services.AssertHitTest(105, 55, null);
    }
}
