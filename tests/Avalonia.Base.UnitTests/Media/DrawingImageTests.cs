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
            var geometryDrawing = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            };
            var drawingImage = new DrawingImage(geometryDrawing);

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            geometryDrawing.Brush = Brushes.Red;

            Assert.True(raised);
        }

        [Fact]
        public void Changing_GeometryDrawing_Pen_Raises_DrawingImage_Invalidated()
        {
            var geometryDrawing = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Pen = new Pen(Brushes.Black)
            };
            var drawingImage = new DrawingImage(geometryDrawing);

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            geometryDrawing.Pen = new Pen(Brushes.Red, 2);

            Assert.True(raised);
        }

        [Fact]
        public void Changing_GlyphRunDrawing_Foreground_Raises_DrawingImage_Invalidated()
        {
            var glyphRunDrawing = new GlyphRunDrawing { Foreground = Brushes.Black };
            var drawingImage = new DrawingImage(glyphRunDrawing);

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            glyphRunDrawing.Foreground = Brushes.Red;

            Assert.True(raised);
        }

        [Fact]
        public void Changing_ImageDrawing_Rect_Raises_DrawingImage_Invalidated()
        {
            var imageDrawing = new ImageDrawing { Rect = new Rect(0, 0, 10, 10) };
            var drawingImage = new DrawingImage(imageDrawing);

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            imageDrawing.Rect = new Rect(0, 0, 20, 20);

            Assert.True(raised);
        }

        [Fact]
        public void Changing_DrawingGroup_Opacity_Raises_DrawingImage_Invalidated()
        {
            var group = new DrawingGroup { Opacity = 1.0 };
            group.Children.Add(new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            });
            var drawingImage = new DrawingImage(group);

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            group.Opacity = 0.5;

            Assert.True(raised);
        }

        [Fact]
        public void Changing_DrawingGroup_Transform_Raises_DrawingImage_Invalidated()
        {
            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            });
            var drawingImage = new DrawingImage(group);

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            group.Transform = new TranslateTransform(5, 5);

            Assert.True(raised);
        }

        [Fact]
        public void Changing_Drawing_Inside_DrawingGroup_Raises_DrawingImage_Invalidated()
        {
            var child = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            };
            var group = new DrawingGroup();
            group.Children.Add(child);
            var drawingImage = new DrawingImage(group);

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            child.Brush = Brushes.Red;

            Assert.True(raised);
        }

        [Fact]
        public void Changing_Deeply_Nested_Drawing_Raises_DrawingImage_Invalidated()
        {
            var leaf = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            };
            var innerGroup = new DrawingGroup();
            innerGroup.Children.Add(leaf);
            var outerGroup = new DrawingGroup();
            outerGroup.Children.Add(innerGroup);
            var drawingImage = new DrawingImage(outerGroup);

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            leaf.Brush = Brushes.Red;

            Assert.True(raised);
        }

        [Fact]
        public void Adding_Child_To_DrawingGroup_Raises_DrawingImage_Invalidated()
        {
            var group = new DrawingGroup();
            var drawingImage = new DrawingImage(group);

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            group.Children.Add(new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            });

            Assert.True(raised);
        }

        [Fact]
        public void Removing_Child_From_DrawingGroup_Raises_DrawingImage_Invalidated()
        {
            var child = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            };
            var group = new DrawingGroup();
            group.Children.Add(child);
            var drawingImage = new DrawingImage(group);

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            group.Children.Remove(child);

            Assert.True(raised);
        }

        [Fact]
        public void Clearing_DrawingGroup_Children_Raises_DrawingImage_Invalidated()
        {
            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            });
            var drawingImage = new DrawingImage(group);

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            group.Children.Clear();

            Assert.True(raised);
        }

        [Fact]
        public void Replacing_Drawing_Unsubscribes_Previous()
        {
            var oldDrawing = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            };
            var drawingImage = new DrawingImage(oldDrawing);

            // Replace with a new drawing
            var newDrawing = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Green
            };
            drawingImage.Drawing = newDrawing;

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            // Mutating the old drawing should NOT raise Invalidated
            oldDrawing.Brush = Brushes.Red;

            Assert.False(raised);
        }

        [Fact]
        public void Replacing_DrawingGroup_Children_Collection_Rewires_Subscriptions()
        {
            var child = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            };
            var group = new DrawingGroup();
            group.Children.Add(child);
            var drawingImage = new DrawingImage(group);

            // Replace the entire Children collection
            var newChildren = new DrawingCollection();
            var newChild = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 5, 5)),
                Brush = Brushes.Green
            };
            newChildren.Add(newChild);
            group.Children = newChildren;

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            // Old child should NOT trigger invalidation
            child.Brush = Brushes.Red;
            Assert.False(raised);

            // New child SHOULD trigger invalidation
            newChild.Brush = Brushes.Yellow;
            Assert.True(raised);
        }

        [Fact]
        public void Replacing_Child_In_DrawingGroup_Rewires_Subscriptions()
        {
            var oldChild = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            };
            var group = new DrawingGroup();
            group.Children.Add(oldChild);
            var drawingImage = new DrawingImage(group);

            // Replace child via remove + add
            var newChild = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 5, 5)),
                Brush = Brushes.Green
            };
            group.Children.Remove(oldChild);
            group.Children.Add(newChild);

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            // Old child should NOT trigger
            oldChild.Brush = Brushes.Red;
            Assert.False(raised);

            // New child SHOULD trigger
            newChild.Brush = Brushes.Yellow;
            Assert.True(raised);
        }

        [Fact]
        public void ImageDrawing_Bubbles_Inner_DrawingImage_Invalidated()
        {
            var innerDrawingImage = new DrawingImage(new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            });
            var imageDrawing = new ImageDrawing
            {
                ImageSource = innerDrawingImage,
                Rect = new Rect(0, 0, 10, 10)
            };
            var outerDrawingImage = new DrawingImage(imageDrawing);

            var raised = false;
            outerDrawingImage.Invalidated += (_, _) => raised = true;

            // Mutating the inner DrawingImage's drawing should bubble up
            ((GeometryDrawing)innerDrawingImage.Drawing!).Brush = Brushes.Red;

            Assert.True(raised);
        }

        [Fact]
        public void Changing_Viewbox_Still_Raises_Invalidated()
        {
            var drawingImage = new DrawingImage(new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            });

            var raised = false;
            drawingImage.Invalidated += (_, _) => raised = true;

            drawingImage.Viewbox = new Rect(0, 0, 5, 5);

            Assert.True(raised);
        }

        [Fact]
        public void Shared_Drawing_Does_Not_Pin_DrawingImage()
        {
            var drawing = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            };

            var weakRef = CreateDrawingImageWeakRef(drawing);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakRef.IsAlive, "DrawingImage should have been collected");
        }

        [Fact]
        public void Shared_DrawingCollection_Does_Not_Pin_DrawingGroup()
        {
            var collection = new DrawingCollection
            {
                new GeometryDrawing
                {
                    Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                    Brush = Brushes.Blue
                }
            };

            var weakRef = CreateDrawingGroupWeakRef(collection);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakRef.IsAlive, "DrawingGroup should have been collected");
        }

        [Fact]
        public void Shared_Drawing_Does_Not_Pin_DrawingGroup()
        {
            var child = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 10, 10)),
                Brush = Brushes.Blue
            };

            var weakRef = CreateDrawingGroupWithChildWeakRef(child);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakRef.IsAlive, "DrawingGroup should have been collected while child survives");
        }

        // Helpers in separate methods to prevent JIT from extending local lifetimes.

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
