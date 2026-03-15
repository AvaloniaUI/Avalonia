using Avalonia.Controls.Primitives;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class VisualLayerManagerTests : ScopedTestBase
    {
        [Fact]
        public void GetAdornerLayer_Returns_Dedicated_AdornerLayer_For_Controls_Inside_OverlayLayer()
        {
            var button = new Button();
            var vlm = new VisualLayerManager { EnableOverlayLayer = true, Child = button };
            var root = new TestRoot { Child = vlm };

            root.Measure(new Size(100, 100));
            root.Arrange(new Rect(0, 0, 100, 100));

            var overlayLayer = vlm.OverlayLayer;
            Assert.NotNull(overlayLayer);

            var overlayChild = new Border();
            overlayLayer.Children.Add(overlayChild);

            // The adorner layer for a control inside the OverlayLayer
            // should be the dedicated one, not the main VLM adorner layer.
            var overlayAdornerLayer = AdornerLayer.GetAdornerLayer(overlayChild);
            Assert.NotNull(overlayAdornerLayer);
            Assert.Same(overlayLayer.AdornerLayer, overlayAdornerLayer);

            // The main VLM adorner layer should be different.
            var mainAdornerLayer = AdornerLayer.GetAdornerLayer(button);
            Assert.NotNull(mainAdornerLayer);
            Assert.NotSame(overlayAdornerLayer, mainAdornerLayer);
        }
    }
}
