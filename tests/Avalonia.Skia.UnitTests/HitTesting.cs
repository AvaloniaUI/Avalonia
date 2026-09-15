using System;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class HitTesting
    {
        [Fact]
        public void Hit_Test_Should_Respect_Fill()
        {
            using (AvaloniaLocator.EnterScope())
            {
                SkiaPlatform.Initialize();

                using var services = new CompositorTestServices(new Size(100, 100), 
                    AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>())
                {
                    TopLevel =
                    {
                        Content = new Ellipse
                        {
                            Width = 100,
                            Height = 100,
                            Fill = Brushes.Red,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                };

                services.AssertHitTest(10, 10, null, Array.Empty<Visual>());
                services.AssertHitTest(50, 50, null, (Visual)services.TopLevel.Content);
            }
        }

        [Fact]
        public void Hit_Test_Should_Respect_Stroke()
        {
            using (AvaloniaLocator.EnterScope())
            {
                SkiaPlatform.Initialize();

                using var services = new CompositorTestServices(new Size(100, 100),
                    AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>())
                {
                    TopLevel =
                    {
                        Content = new Ellipse
                        {
                            Width = 100,
                            Height = 100,
                            Stroke = Brushes.Red,
                            StrokeThickness = 5,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                };
                
                
                services.AssertHitTest(50, 50, null, Array.Empty<Visual>());
                services.AssertHitTest(1, 50, null, (Visual)services.TopLevel.Content);
            }
        }

        [Fact]
        public void Geometry_Hit_Test_Should_Detect_Stroke_Only_Shapes()
        {
            using (AvaloniaLocator.EnterScope())
            {
                SkiaPlatform.Initialize();

                Line? line = null;

                using var services = new CompositorTestServices(new Size(100, 100),
                    AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>())
                {
                    TopLevel =
                    {
                        Content = line = new Line
                        {
                            StartPoint = new Point(0, 0),
                            EndPoint = new Point(100,0),
                            Stroke = Brushes.Red,
                            StrokeThickness = 5,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                };


                services.AssertHitTest(new RectangleGeometry(new Rect(25, 25, 10, 10)), null);
                services.AssertHitTest(new RectangleGeometry(new Rect(45, 45, 10, 10)), null, new GeometryHitTestResult(line, IntersectionResult.Intersects));
            }
        }

        [Fact]
        public void Geometry_Hit_Test_Border_With_Background_And_Border()
        {
            using (AvaloniaLocator.EnterScope())
            {
                SkiaPlatform.Initialize();

                Border? border = null;

                using var services = new CompositorTestServices(new Size(400, 400),
                    AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>())
                {
                    TopLevel =
                    {
                        Content = border = new Border
                        {
                            Width = 200,
                            Height = 200,
                            Background = Brushes.Red,
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(10),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                };

                // Geometry fully inside the border -> FullyContains
                services.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 50, 50)), null,
                    new GeometryHitTestResult(border, IntersectionResult.FullyContains));

                // Geometry overlapping the border stroke -> Intersects
                services.AssertHitTest(new RectangleGeometry(new Rect(195, 95, 10, 10)), null,
                    new GeometryHitTestResult(border, IntersectionResult.Intersects));

                // Geometry outside -> no hit
                services.AssertHitTest(new RectangleGeometry(new Rect(310, 310, 10, 10)), null);
            }
        }

        [Fact]
        public void Geometry_Hit_Test_Border_Null_Background_With_BorderBrush()
        {
            using (AvaloniaLocator.EnterScope())
            {
                SkiaPlatform.Initialize();

                Border? border = null;

                using var services = new CompositorTestServices(new Size(400, 400),
                    AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>())
                {
                    TopLevel =
                    {
                        Content = border = new Border
                        {
                            Width = 200,
                            Height = 200,
                            Background = null,
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(10),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                };

                // Geometry fully inside (center) should NOT hit because background is null
                services.AssertHitTest(new RectangleGeometry(new Rect(150, 150, 50, 50)), null);

                // Geometry overlapping the border stroke -> Intersects
                services.AssertHitTest(new RectangleGeometry(new Rect(190, 105, 10, 10)), null,
                    new GeometryHitTestResult(border, IntersectionResult.Intersects));
            }
        }

        [Fact]
        public void Geometry_Hit_Test_Border_Null_Background_And_Null_BorderBrush_NoHit()
        {
            using (AvaloniaLocator.EnterScope())
            {
                SkiaPlatform.Initialize();

                Border? border = null;

                using var services = new CompositorTestServices(new Size(400, 400),
                    AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>())
                {
                    TopLevel =
                    {
                        Content = border = new Border
                        {
                            Width = 200,
                            Height = 200,
                            Background = null,
                            BorderBrush = null,
                            BorderThickness = new Thickness(10),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                };

                // No background and no border brush -> no hit for any geometry
                services.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 50, 50)), null);
                services.AssertHitTest(new RectangleGeometry(new Rect(195, 100, 10, 10)), null);
            }
        }


        [Fact]
        public void Geometry_Hit_Test_Line_Intersects_RectangleGeometry()
        {
            using (AvaloniaLocator.EnterScope())
            {
                SkiaPlatform.Initialize();

                Line? line = null;

                using var services = new CompositorTestServices(new Size(100, 100),
                    AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>())
                {
                    TopLevel =
                    {
                        Content = line = new Line
                        {
                            StartPoint = new Point(0, 0),
                            EndPoint = new Point(100, 100),
                            Stroke = Brushes.Red,
                            StrokeThickness = 5,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                };

                // Rectangle away from the line -> no hit
                services.AssertHitTest(new RectangleGeometry(new Rect(0, 80, 10, 10)), null);

                // Rectangle overlapping the diagonal line -> Intersects
                services.AssertHitTest(new RectangleGeometry(new Rect(45, 45, 10, 10)), null,
                    new GeometryHitTestResult(line, IntersectionResult.Intersects));
            }
        }
    }
}
