using Avalonia.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class CompositorInvalidationClippingTests : CompositorTestsBase
{
    [Fact]
    public void Siblings_Should_Be_Rendered_On_Invalidate_Without_ClipToBounds()
    {
        AssertRenderedVisuals(clipToBounds: false, clipGeometry: false, expectedRenderedVisualsCount: 4);
    }

    [Fact]
    public void Siblings_Should_Not_Be_Rendered_On_Invalidate_With_ClipToBounds()
    {
        AssertRenderedVisuals(clipToBounds: true, clipGeometry: false, expectedRenderedVisualsCount: 3);
    }

    [Fact]
    public void Siblings_Should_Not_Be_Rendered_On_Invalidate_With_Clip()
    {
        AssertRenderedVisuals(clipToBounds: false, clipGeometry: true, expectedRenderedVisualsCount: 3);
    }

    private void AssertRenderedVisuals(bool clipToBounds, bool clipGeometry, int expectedRenderedVisualsCount)
    {
        using (var s = new CompositorCanvas())
        {
            //#1 visual to render is root
            //#2 visual to render is s.Canvas
            
            //#3 visual to render
            s.Canvas.Children.Add(new Border()
            {
                [Canvas.LeftProperty] = 0, [Canvas.TopProperty] = 0,
                Width = 20, Height = 10,
                Background = Brushes.Red,
                ClipToBounds = clipToBounds,
                Clip = clipGeometry ? new RectangleGeometry(new Rect(new Size(20, 10))) : null
            });
            
            //#4 visual to render
            s.Canvas.Children.Add(new Border()
            {
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50,
                Width = 20, Height = 10,
                Background = Brushes.Red,
                ClipToBounds = clipToBounds,
                Clip = clipGeometry ? new RectangleGeometry(new Rect(new Size(20, 10))) : null
            });
            s.RunJobs();
            s.Events.Reset();
            s.Canvas.Children[0].IsVisible = false;
            s.RunJobs();
            s.AssertRenderedVisuals(expectedRenderedVisualsCount);
        }
    }
}
