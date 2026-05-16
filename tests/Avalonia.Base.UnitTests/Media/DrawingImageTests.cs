using System;
using Avalonia.Collections;
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
        public void Changing_Drawing_Inside_DrawingGroup_Raises_DrawingImage_Invalidated()
        {
            var child = CreateGeometryDrawing();
            var group = new DrawingGroup();
            group.Children.Add(child);

            AssertInvalidated(group, () => child.Brush = Brushes.Red);
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
        public void ImageDrawing_Bubbles_Inner_DrawingImage_Invalidated()
        {
            var innerDrawing = CreateGeometryDrawing();
            var innerImage = new DrawingImage(innerDrawing);
            var drawing = new ImageDrawing
            {
                ImageSource = innerImage,
                Rect = new Rect(0, 0, 10, 10)
            };

            AssertInvalidated(drawing, () => innerDrawing.Brush = Brushes.Red);
        }

        [Fact]
        public void Resetting_DrawingGroup_Children_Collection_Rebuilds_Subscriptions()
        {
            var oldChild = CreateGeometryDrawing();
            var children = new DrawingCollection { oldChild };
            children.ResetBehavior = ResetBehavior.Reset;

            var group = new DrawingGroup { Children = children };
            var drawingImage = new DrawingImage(group);

            var newChild = CreateGeometryDrawing();
            children.Clear();
            children.Add(newChild);

            AssertNotInvalidated(drawingImage, () => oldChild.Brush = Brushes.Red);
            AssertInvalidatedOn(drawingImage, () => newChild.Brush = Brushes.Yellow);
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
        public void Shared_Drawing_Does_Not_Pin_DrawingGroup()
        {
            var child = CreateGeometryDrawing();
            var weakRef = CreateDrawingGroupWeakRef(child);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakRef.IsAlive, "DrawingGroup should have been collected while child survives");
        }

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
        private static WeakReference CreateDrawingGroupWeakRef(Drawing child)
        {
            var group = new DrawingGroup();
            group.Children.Add(child);
            return new WeakReference(group);
        }
    }
}
