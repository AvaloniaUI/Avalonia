using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
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

    private class ClearingHost : Control
    {
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            // Natural cleanup, symmetric with attaching in
            // OnAttachedToVisualTree. Runs before DetachFromCompositor, which
            // used to skip un-parenting the child visual once the property
            // was cleared.
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

    [Fact]
    public void Container_Child_Visual_Gets_Subtree_Bounds()
    {
        // Directly-created visuals carry no Size; with an implicit
        // ClipToBounds default their own clip rect was empty, which nulled
        // the subtree bounds and made the render pass cull the whole
        // child-visual tree — visuals attached via SetElementChildVisual
        // never rendered unless Size was set explicitly.
        var container = _services.Compositor.CreateContainerVisual();
        var solid = _services.Compositor.CreateSolidColorVisual();
        solid.Size = new Vector(60, 60);
        solid.Color = Colors.Lime;
        container.Children.Add(solid);

        var host = new Control { Width = 100, Height = 100 };
        _services.TopLevel.Content = host;
        _services.RunJobs();
        ElementComposition.SetElementChildVisual(host, container);
        _services.RunJobs();

        _services.Compositor.Commit();
        _services.Compositor.Server.Render(false);

        var bounds = (LtrbRect?)typeof(ServerCompositionVisual)
            .GetField("_transformedSubTreeBounds",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(container.Server);

        Assert.NotNull(bounds);
        Assert.Equal(0, bounds!.Value.Left);
        Assert.Equal(60, bounds.Value.Right);
        Assert.Equal(60, bounds.Value.Bottom);
    }
}
