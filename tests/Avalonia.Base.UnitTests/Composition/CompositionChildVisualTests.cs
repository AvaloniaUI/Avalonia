using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

public class CompositionChildVisualTests : ScopedTestBase
{
    private readonly CompositorTestServices _services = new();

    public override void Dispose()
    {
        _services.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Clears its child visual when it leaves the tree, the usual counterpart to
    /// setting one on attach.
    /// </summary>
    private sealed class ClearingHost : Control
    {
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            ElementComposition.SetElementChildVisual(this, null);
        }
    }

    [Fact]
    public void Child_Visual_Cleared_During_Detach_Can_Reattach()
    {
        var child = _services.Compositor.CreateContainerVisual();
        var host = new ClearingHost { Width = 100, Height = 100 };
        _services.TopLevel.Content = host;
        _services.RunJobs();
        ElementComposition.SetElementChildVisual(host, child);
        _services.RunJobs();

        _services.TopLevel.Content = null;
        _services.RunJobs();
        Assert.Null(child.Parent);

        _services.TopLevel.Content = host;
        _services.RunJobs();
        ElementComposition.SetElementChildVisual(host, child);
        _services.RunJobs();

        var elementVisual = Assert.IsAssignableFrom<CompositionContainerVisual>(
            ElementComposition.GetElementVisual(host));
        Assert.Contains(child, elementVisual.Children);
    }
}
