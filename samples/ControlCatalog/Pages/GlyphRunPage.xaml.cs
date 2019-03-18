using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public class GlyphRunPage : UserControl
    {
        public GlyphRunPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void Render(DrawingContext drawingContext)
        {
            using (var glyphTypeface = new GlyphTypeface(Typeface.Default))
            {
                const double RenderingEmSize = 2.0d;

                var glyphIndices = glyphTypeface.CharactersToGlyphs("Hello World");

                var glyphAdvances = glyphIndices.Select(x => glyphTypeface.GetHorizontalGlyphAdvance(x) * RenderingEmSize)
                    .ToArray();

                var baselineOrigin = new Point(0, (-glyphTypeface.Ascent) * RenderingEmSize);

                var glyphRun = new GlyphRun(glyphTypeface, RenderingEmSize, glyphIndices, baselineOrigin, glyphAdvances, null);

                drawingContext.DrawGlyphRun(Brushes.Black, glyphRun);
            }
        }
    }
}
