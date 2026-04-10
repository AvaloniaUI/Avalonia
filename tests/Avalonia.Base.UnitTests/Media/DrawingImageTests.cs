using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class DrawingImageTests
    {
        [Fact]
        public void Changing_GeometryDrawing_Brush_Raises_DrawingImage_Invalidated()
        {
            var drawing = CreateGeometryDrawing();
            AssertInvalidated(drawing, () => drawing.Brush = Brushes.Red);
        }

        [Fact]
        public void Changing_GlyphRunDrawing_Foreground_Raises_DrawingImage_Invalidated()
        {
            var drawing = new GlyphRunDrawing { Foreground = Brushes.Black };
            AssertInvalidated(drawing, () => drawing.Foreground = Brushes.Red);
        }

        [Fact]
        public void Changing_ImageDrawing_Rect_Raises_DrawingImage_Invalidated()
        {
            var drawing = new ImageDrawing { Rect = new Rect(0, 0, 10, 10) };
            AssertInvalidated(drawing, () => drawing.Rect = new Rect(0, 0, 20, 20));
        }

        [Fact]
        public void Changing_DrawingGroup_Opacity_Raises_DrawingImage_Invalidated()
        {
            var group = new DrawingGroup { Opacity = 1.0 };
            group.Children.Add(CreateGeometryDrawing());
            AssertInvalidated(group, () => group.Opacity = 0.5);
        }

        [Fact]
        public void Changing_Drawing_Inside_DrawingGroup_Raises_DrawingImage_Invalidated()
        {
            var child = CreateGeometryDrawing();
            var group = new DrawingGroup();
            group.Children.Add(child);
            AssertInvalidated(group, () => child.Brush = Brushes.Red);
        }

        [Fact]
        public void Changing_Deeply_Nested_Drawing_Raises_DrawingImage_Invalidated()
        {
            var leaf = CreateGeometryDrawing();
            var innerGroup = new DrawingGroup();
            innerGroup.Children.Add(leaf);
            var outerGroup = new DrawingGroup();
            outerGroup.Children.Add(innerGroup);
            AssertInvalidated(outerGroup, () => leaf.Brush = Brushes.Red);
        }

        [Fact]
        public void Adding_Child_To_DrawingGroup_Raises_DrawingImage_Invalidated()
        {
            var group = new DrawingGroup();
            AssertInvalidated(group, () => group.Children.Add(CreateGeometryDrawing()));
        }

        [Fact]
        public void Removing_Child_From_DrawingGroup_Raises_DrawingImage_Invalidated()
        {
            var child = CreateGeometryDrawing();
            var group = new DrawingGroup();
            group.Children.Add(child);
            AssertInvalidated(group, () => group.Children.Remove(child));
        }

        [Fact]
        public void Clearing_DrawingGroup_Children_Raises_DrawingImage_Invalidated()
        {
            var group = new DrawingGroup();
            group.Children.Add(CreateGeometryDrawing());
            AssertInvalidated(group, () => group.Children.Clear());
        }

        [Fact]
        public void Replacing_Drawing_Unsubscribes_Previous()
        {
            var oldDrawing = CreateGeometryDrawing();
            var drawingImage = new DrawingImage(oldDrawing);

            drawingImage.Drawing = CreateGeometryDrawing();

            AssertNotInvalidated(drawingImage, () => oldDrawing.Brush = Brushes.Red);
        }

        [Fact]
        public void Replacing_DrawingGroup_Children_Collection_Rewires_Subscriptions()
        {
            var oldChild = CreateGeometryDrawing();
            var group = new DrawingGroup();
            group.Children.Add(oldChild);
            var drawingImage = new DrawingImage(group);

            var newChild = CreateGeometryDrawing();
            group.Children = new DrawingCollection { newChild };

            AssertNotInvalidated(drawingImage, () => oldChild.Brush = Brushes.Red);
            AssertInvalidatedOn(drawingImage, () => newChild.Brush = Brushes.Yellow);
        }

        [Fact]
        public void Replacing_Child_In_DrawingGroup_Rewires_Subscriptions()
        {
            var oldChild = CreateGeometryDrawing();
            var group = new DrawingGroup();
            group.Children.Add(oldChild);
            var drawingImage = new DrawingImage(group);

            var newChild = CreateGeometryDrawing();
            group.Children.Remove(oldChild);
            group.Children.Add(newChild);

            AssertNotInvalidated(drawingImage, () => oldChild.Brush = Brushes.Red);
            AssertInvalidatedOn(drawingImage, () => newChild.Brush = Brushes.Yellow);
        }

        [Fact]
        public void ImageDrawing_Bubbles_Inner_DrawingImage_Invalidated()
        {
            var innerGeometry = CreateGeometryDrawing();
            var innerDrawingImage = new DrawingImage(innerGeometry);
            var imageDrawing = new ImageDrawing
            {
                ImageSource = innerDrawingImage,
                Rect = new Rect(0, 0, 10, 10)
            };
            AssertInvalidated(imageDrawing, () => innerGeometry.Brush = Brushes.Red);
        }

        [Fact]
        public void Changing_Viewbox_Still_Raises_Invalidated()
        {
            var drawingImage = new DrawingImage(CreateGeometryDrawing());

            AssertInvalidatedOn(drawingImage, () => drawingImage.Viewbox = new Rect(0, 0, 5, 5));
        }

        [Fact]
        public void Shared_Drawing_Does_Not_Pin_DrawingImage()
        {
            var drawing = CreateGeometryDrawing();
            var weakRef = CreateDrawingImageWeakRef(drawing);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakRef.IsAlive, "DrawingImage should have been collected");
        }

        [Fact]
        public void Shared_DrawingCollection_Does_Not_Pin_DrawingGroup()
        {
            var collection = new DrawingCollection { CreateGeometryDrawing() };
            var weakRef = CreateDrawingGroupWeakRef(collection);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakRef.IsAlive, "DrawingGroup should have been collected");
        }

        [Fact]
        public void Shared_Drawing_Does_Not_Pin_DrawingGroup()
        {
            var child = CreateGeometryDrawing();
            var weakRef = CreateDrawingGroupWithChildWeakRef(child);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakRef.IsAlive, "DrawingGroup should have been collected while child survives");
        }

        // -- Helpers --

        private static GeometryDrawing CreateGeometryDrawing() => new()
        {
            Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
            Brush = Brushes.Blue
        };

        private static void AssertInvalidated(Drawing drawing, Action mutate)
        {
            var drawingImage = new DrawingImage(drawing);
            AssertInvalidatedOn(drawingImage, mutate);
        }

        private static void AssertInvalidatedOn(DrawingImage drawingImage, Action mutate)
        {
            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;
            mutate();
            Assert.True(raised);
        }

        private static void AssertNotInvalidated(DrawingImage drawingImage, Action mutate)
        {
            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;
            mutate();
            Assert.False(raised);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static WeakReference CreateDrawingImageWeakRef(Drawing drawing)
        {
            var drawingImage = new DrawingImage(drawing);
            return new WeakReference(drawingImage);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static WeakReference CreateDrawingGroupWeakRef(DrawingCollection collection)
        {
            var group = new DrawingGroup { Children = collection };
            return new WeakReference(group);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static WeakReference CreateDrawingGroupWithChildWeakRef(Drawing child)
        {
            var group = new DrawingGroup();
            group.Children.Add(child);
            return new WeakReference(group);
        }
    }
}
