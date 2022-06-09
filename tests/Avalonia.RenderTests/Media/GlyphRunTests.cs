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
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 100,
                Child = new GlyphRunGeometryControl
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
                }
            };

            await RenderToFile(target);

            CompareImages();
        }

        public class GlyphRunGeometryControl : Control
        {
            private readonly Geometry _geometry;

            public GlyphRunGeometryControl()
            {
                var glyphTypeface = Typeface.Default.GlyphTypeface;

                var glyphIndices = new[] { glyphTypeface.GetGlyph('A'), glyphTypeface.GetGlyph('B'), glyphTypeface.GetGlyph('C') };

                var characters = new[] { 'A', 'B', 'C' };

                var glyphRun = new GlyphRun(glyphTypeface, 100, characters, glyphIndices);

                _geometry = glyphRun.BuildGeometry();
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                return _geometry.Bounds.Size;
            }

            public override void Render(DrawingContext context)
            {
                var foreground = TextElement.GetForeground(this);

                context.DrawGeometry(foreground, null, _geometry);
            }
        }
    }
}
