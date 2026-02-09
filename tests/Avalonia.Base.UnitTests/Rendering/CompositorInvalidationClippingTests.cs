using Avalonia.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;
/// <summary>
/// Test class that verifies how clipping influences rendering in the compositor
/// </summary>
public class CompositorInvalidationClippingTests : CompositorTestsBase
{
    int CountVisuals(Visual visual)
    {
        int count = 1; // Count the current visual
        foreach (var child in visual.VisualChildren) count += CountVisuals(child);
        return count;
    }
    
    [Theory,
        // If canvas itself has no background, the second render won't draw any visuals at all, since
        // root visual's subtree bounds will exactly match the second visual 
        InlineData(false, false,  false, 1, 0),
        InlineData(true, false, false, 1, 0),
        InlineData(false, true, false, 1, 0),
        // If canvas has background, the second render will draw only the canvas visual itself
        InlineData(false, false, true, 5, 4),
        InlineData(true, false,   true,5, 4),
        InlineData(false, true,  true, 5, 4),
    ]
    public void Do_Not_Re_Render_Unaffected_Visual_Trees(bool clipToBounds, bool clipGeometry,
        bool canvasHasContent,
        int expectedVisitedVisualsCount, int expectedRenderedVisualsCount)
    {
        using (var s = new CompositorCanvas())
        {
            // #1 visual is top level
            // #2 is ContentPresenter
            // #3 visual is s.Canvas
            
            //# 4 visual is border1
            s.Canvas.Children.Add(new Border()
            {
                [Canvas.LeftProperty] = 0, [Canvas.TopProperty] = 0,
                Width = 20, Height = 10,
                Background = Brushes.Red,
                ClipToBounds = clipToBounds,
                Clip = clipGeometry ? new RectangleGeometry(new Rect(new Size(20, 10))) : null
            });
            
            //# 5 visual is border2
            s.Canvas.Children.Add(new Border()
            {
                [Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50,
                Width = 20, Height = 10,
                Background = Brushes.Red,
                ClipToBounds = clipToBounds,
                Clip = clipGeometry ? new RectangleGeometry(new Rect(new Size(20, 10))) : null
            });
            if (canvasHasContent)
                s.Canvas.Background = Brushes.Green;
            s.RunJobs();
            s.Events.Reset();
            if (CountVisuals(s.TopLevel) != 5)
                Assert.Fail("Layout part of the test is broken, expected 5 visuals in the tree");
            
            //invalidate border1
            s.Canvas.Children[0].IsVisible = false;
            s.RunJobs();
            
            s.AssertRenderedVisuals(expectedVisitedVisualsCount, expectedRenderedVisualsCount);
        }
    }
}
