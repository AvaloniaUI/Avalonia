using System;
using Avalonia.Media;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class MediaInvalidationTests
    {
        [Fact]
        public void NestedMediaChanges_InvalidateParentVisual()
        {
            var root = new TestRoot();
            var rendererMock = Mock.Get(root.Renderer);

            var rectGeometry = new RectangleGeometry();
            var brush = new LinearGradientBrush
            {
                GradientStops = new()
                {
                    new(),
                    new(),
                }
            };
            var dashStyle = new DashStyle() { Dashes = new() };

            var image = root.Child = new Image
            {
                Source = new DrawingImage
                {
                    Drawing = new DrawingGroup
                    {
                        Children = new()
                        {
                            new GeometryDrawing
                            {
                                Brush = brush,
                                Geometry = rectGeometry,
                                Pen = new Pen
                                {
                                    Brush = Brushes.Black,
                                    DashStyle = dashStyle,
                                },
                            }
                        }
                    }
                }
            };

            VerifyInvalidation(() => rectGeometry.Rect = new(0, 0, 20, 20), nameof(RectangleGeometry.RectProperty));

            VerifyInvalidation(() => brush.GradientStops.Add(new()), nameof(GradientBrush.GradientStopsProperty));

            VerifyInvalidation(() => dashStyle.Dashes.Add(0.5), nameof(DashStyle.DashesProperty));

            void VerifyInvalidation(Action action, string description)
            {
                rendererMock.Reset();

                action();

                rendererMock.Verify(r => r.AddDirty(image), Times.Once, $"A change to {description} did not trigger invalidation.");
            }
        }

        [Fact]
        public void MultipleMediaParents_EachParentInvalidatedExactlyOnce()
        {
            var root = new TestRoot();
            var rendererMock = Mock.Get(root.Renderer);

            var pen = new Pen();

            var drawingImage = new DrawingImage
            {
                Drawing = new DrawingGroup
                {
                    Children = new()
                    {
                        new GeometryDrawing { Pen = pen },
                        new GeometryDrawing { Pen = pen },
                    }
                }
            };

            var panel = new StackPanel();

            root.Child = panel;

            for (var i = 0; i < 5; i++)
            {
                panel.Children.Add(new Image { Source = drawingImage });
            }

            rendererMock.Reset();

            pen.DashStyle = DashStyle.Dash;

            foreach(var image in panel.Children)
            {
                rendererMock.Verify(r => r.AddDirty(image), Times.Once);
            }
        }
    }
}
