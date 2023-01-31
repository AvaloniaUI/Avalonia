using System;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
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

                services.AssertHitTest(10, 10, null, Array.Empty<object>());
                services.AssertHitTest(50, 50, null, services.TopLevel.Content);
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
                
                
                services.AssertHitTest(50, 50, null, Array.Empty<object>());
                services.AssertHitTest(1, 50, null, services.TopLevel.Content);
            }
        }
    }
}
