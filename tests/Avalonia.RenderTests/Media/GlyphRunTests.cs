using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
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
                Width = 80,
                Height = 90,
                Child = new GlyphRunGeometryControl()
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

                var glyphIndices = new[] { glyphTypeface.GetGlyph('A') };

                var characters = new[] { 'A' };

                var glyphRun = new GlyphRun(glyphTypeface, 100, characters, glyphIndices);

                _geometry = glyphRun.BuildGeometry();
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                return _geometry.Bounds.Size;
            }

            public override void Render(DrawingContext context)
            {
                context.DrawGeometry(Brushes.Green, null, _geometry);
            }
        }
    }
}
