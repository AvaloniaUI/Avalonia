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
        
        [Fact]
        public void GetAdornerLayer_Returns_Same_AdornerLayer_For_VisualLayerManager()
        {
            var vlm = new VisualLayerManager();
            var root = new TestRoot { Child = vlm };

            root.Measure(new Size(100, 100));
            root.Arrange(new Rect(0, 0, 100, 100));

            var adornerLayer = vlm.AdornerLayer;
            Assert.NotNull(adornerLayer);
            
            // The adorner layer for a control inside the OverlayLayer
            // should be the dedicated one, not the main VLM adorner layer.
            var target = AdornerLayer.GetAdornerLayer(vlm);
            Assert.NotNull(target);
            Assert.Same(adornerLayer, target);
        }
        
        [Fact]
        public void GetAdornerLayer_Returns_Same_AdornerLayer_For_Child()
        {
            var button = new Button();
            var vlm = new VisualLayerManager() { Child = button };
            var root = new TestRoot { Child = vlm };

            root.Measure(new Size(100, 100));
            root.Arrange(new Rect(0, 0, 100, 100));

            var adornerLayer = vlm.AdornerLayer;
            Assert.NotNull(adornerLayer);
            
            // The adorner layer for a control inside the OverlayLayer
            // should be the dedicated one, not the main VLM adorner layer.
            var target = AdornerLayer.GetAdornerLayer(button);
            Assert.NotNull(target);
            Assert.Same(adornerLayer, target);
        }
        
        [Fact]
        public void GetOverlayLayer_Returns_Same_OverlayLayer_For_VisualLayerManager()
        {
            var vlm = new VisualLayerManager() { EnableOverlayLayer = true };
            var root = new TestRoot { Child = vlm };

            root.Measure(new Size(100, 100));
            root.Arrange(new Rect(0, 0, 100, 100));

            var overlayLayer = vlm.OverlayLayer;
            Assert.NotNull(overlayLayer);
            
            // The adorner layer for a control inside the OverlayLayer
            // should be the dedicated one, not the main VLM adorner layer.
            var target = OverlayLayer.GetOverlayLayer(vlm);
            Assert.NotNull(target);
            Assert.Same(overlayLayer, target);
        }
        
        [Fact]
        public void GetOverlayLayer_Returns_Same_OverlayLayer_For_Child()
        {
            var button = new Button();
            var vlm = new VisualLayerManager() { EnableOverlayLayer = true, Child = button };
            var root = new TestRoot { Child = vlm };

            root.Measure(new Size(100, 100));
            root.Arrange(new Rect(0, 0, 100, 100));

            var overlayLayer = vlm.OverlayLayer;
            Assert.NotNull(overlayLayer);
            
            // The adorner layer for a control inside the OverlayLayer
            // should be the dedicated one, not the main VLM adorner layer.
            var target = OverlayLayer.GetOverlayLayer(button);
            Assert.NotNull(target);
            Assert.Same(overlayLayer, target);
        }
    }
}
