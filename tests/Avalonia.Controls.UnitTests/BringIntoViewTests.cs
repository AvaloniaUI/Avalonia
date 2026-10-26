using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class BringIntoViewTests : ScopedTestBase
{
    [Fact]
    public void BringIntoView_On_Laid_Out_Control_Raises_RequestBringIntoView_Synchronously()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var child = new Border { Width = 50, Height = 50 };
        var root = new TestRoot(child);
        root.LayoutManager.ExecuteInitialLayoutPass();

        var raised = false;
        root.AddHandler(Control.RequestBringIntoViewEvent, (_, _) => raised = true);

        child.BringIntoView();

        Assert.True(raised);
    }

    [Fact]
    public void BringIntoView_Before_Layout_Is_Deferred_Until_End_Of_Layout_Pass()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var child = new Border { Width = 50, Height = 50 };
        var root = new TestRoot(child);

        var raised = 0;
        var targetRect = default(Rect);
        root.AddHandler(Control.RequestBringIntoViewEvent, (_, e) =>
        {
            ++raised;
            targetRect = e.TargetRect;
        });

        child.BringIntoView();

        Assert.Equal(0, raised);

        root.LayoutManager.ExecuteInitialLayoutPass();

        Assert.Equal(1, raised);
        Assert.Equal(new Rect(0, 0, 50, 50), targetRect);
    }

    [Fact]
    public void Deferred_BringIntoView_Is_Abandoned_When_Control_Becomes_Invisible()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var child = new Border { Width = 50, Height = 50 };
        var root = new TestRoot(child);

        var raised = false;
        root.AddHandler(Control.RequestBringIntoViewEvent, (_, _) => raised = true);

        child.BringIntoView();
        child.IsVisible = false;
        root.LayoutManager.ExecuteInitialLayoutPass();

        Assert.False(raised);
    }

    [Fact]
    public void BringIntoView_On_Invisible_Control_Is_Ignored()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        var child = new Border { Width = 50, Height = 50, IsVisible = false };
        var root = new TestRoot(child);
        root.LayoutManager.ExecuteInitialLayoutPass();

        var raised = false;
        root.AddHandler(Control.RequestBringIntoViewEvent, (_, _) => raised = true);

        child.BringIntoView();
        root.LayoutManager.ExecuteLayoutPass();

        Assert.False(raised);
    }
}
