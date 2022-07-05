using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class GlyphRunTests : TestBase
    {
        public GlyphRunTests()
            : base(@"Media\GlyphRun")
        {
        }
         
        [Fact]
        public async Task Should_Render_GlyphRun_Geometry()
        {
            var control = new GlyphRunGeometryControl
            {
                [TextElement.ForegroundProperty] = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                    GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        }
                }
            };

            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 190,
                Height = 120,
                Child = control
            };

            await RenderToFile(target);

            CompareImages();
        }

        public class GlyphRunGeometryControl : Control
        {
            public GlyphRunGeometryControl()
            {
                var glyphTypeface = new Typeface(TestFontFamily).GlyphTypeface;

                var glyphIndices = new[] { glyphTypeface.GetGlyph('A'), glyphTypeface.GetGlyph('B'), glyphTypeface.GetGlyph('C') };

                var characters = new[] { 'A', 'B', 'C' };

                var glyphRun = new GlyphRun(glyphTypeface, 100, characters, glyphIndices);

                Geometry = glyphRun.BuildGeometry();
            }

           public Geometry Geometry { get; }

            public override void Render(DrawingContext context)
            {
                var foreground = TextElement.GetForeground(this);

                context.DrawGeometry(foreground, null, Geometry);
            }
        }
    }
}
