using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests;
#else
namespace Avalonia.Direct2D1.RenderTests.Controls;
#endif

public class AdornerTests : TestBase
{
    public AdornerTests()
        : base(@"Controls\Adorner")
    {
    }

    async Task CheckAdornedContent(Control content, Control adorned, Control adorner, int width = 200, int height = 200,
        [CallerMemberName] string testName = "")
    {
        var tree = new Decorator
        {
            Child = new VisualLayerManager
            {
                Child = content
            },
            Width = width,
            Height = height
        };
        
        var size = new Size(tree.Width, tree.Height);
        tree.Measure(size);
        tree.Arrange(new Rect(size));
        
        adorned.AttachedToVisualTree += delegate
        {
            AdornerLayer.SetAdornedElement(adorner, adorned);
            AdornerLayer.GetAdornerLayer(adorned)!.Children.Add(adorner);
        };
        
        tree.Measure(size);
        tree.Arrange(new Rect(size));
        
        await RenderToFile(tree, testName: testName);
        CompareImages(skipImmediate: true, testName: testName);
    }
    
    [Theory,
        InlineData(true),
        InlineData(false)
    ]
    public async Task Focus_Adorner_Is_Properly_Clipped(bool clip)
    {
        Border adorned;
        var content = new Border
        {
            Background = Brushes.Red,
            Padding = new Thickness(10, 50, 10, 10),
            Child = new Border()
            {
                Background = Brushes.White,
                ClipToBounds = true,
                Padding = new Thickness(0, -30, 0, 0),
                Child = adorned = new Border
                {
                    Background = Brushes.Green,
                    VerticalAlignment = VerticalAlignment.Top,
                    Height = 100,
                    Width = 50
                }
            }
        };
        var adorner = new Border
        {
            BorderThickness = new Thickness(2),
            BorderBrush = Brushes.Black
        };
        if (!clip)
            AdornerLayer.SetIsClipEnabled(adorner, false);
        await CheckAdornedContent(content, adorned, adorner,
            testName: "Focus_Adorner_Is_Properly_Clipped_Clip_" + clip);
    }
    
}