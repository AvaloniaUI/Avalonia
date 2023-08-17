using Avalonia.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;
/// <summary>
/// Test class that verifies how clipping influences rendering in the compositor
/// </summary>
public class CompositorInvalidationClippingTests : CompositorTestsBase
{
    [Fact]
    // Test case: When the ClipToBounds is false, all visuals should be rendered
    public void Siblings_Should_Be_Rendered_On_Invalidate_Without_ClipToBounds()
    {
        AssertRenderedVisuals(clipToBounds: false, clipGeometry: false, expectedRenderedVisualsCount: 4);
    }

    [Fact]
    // Test case: When the ClipToBounds is true, only visuals within the clipped boundary should be rendered
    public void Siblings_Should_Not_Be_Rendered_On_Invalidate_With_ClipToBounds()
    {
        AssertRenderedVisuals(clipToBounds: true, clipGeometry: false, expectedRenderedVisualsCount: 3);
    }

    [Fact]
    // Test case: When the Clip is used, only visuals within the clip geometry should be rendered
    public void Siblings_Should_Not_Be_Rendered_On_Invalidate_With_Clip()
    {
        AssertRenderedVisuals(clipToBounds: false, clipGeometry: true, expectedRenderedVisualsCount: 3);
    }

    private void AssertRenderedVisuals(bool clipToBounds, bool clipGeometry, int expectedRenderedVisualsCount)
    {
        using (var s = new CompositorCanvas())
        {
            //#1 visual is top level
            //#2 visual is s.Canvas
            
            //#3 visual is border1
            s.Canvas.Children.Add(new Border()
            {
                [Canvas.LeftProperty] = 0, [Canvas.TopProperty] = 0,
                Width = 20, Height = 10,
                Background = Brushes.Red,
                ClipToBounds = clipToBounds,
                Clip = clipGeometry ? new RectangleGeometry(new Rect(new Size(20, 10))) : null
            });
            
            //#4 visual is border2
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
            
            //invalidate border1
            s.Canvas.Children[0].IsVisible = false;
            s.RunJobs();
            
            s.AssertRenderedVisuals(expectedRenderedVisualsCount);
        }
    }
}
