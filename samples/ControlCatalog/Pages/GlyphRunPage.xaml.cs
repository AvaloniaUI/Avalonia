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
            var glyphTypeface = Typeface.Default.GlyphTypeface;

            const double RenderingEmSize = 1.0d;

            var scale = (12.0 * RenderingEmSize) / glyphTypeface.DesignEmHeight;

            var glyphs = glyphTypeface.GetGlyphs("ABCDEFGHIJKL");           

            var baselineOrigin = new Point(0, -glyphTypeface.Ascent * scale);

            var glyphRun = new GlyphRun(glyphTypeface, RenderingEmSize, baselineOrigin, glyphs.ToArray());

            drawingContext.DrawGlyphRun(Brushes.Black, glyphRun);
        }
    }
}
