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

        private static readonly GlyphTypeface s_glyphTypeface = new GlyphTypeface(new Typeface("Arial"));

        public override void Render(DrawingContext drawingContext)
        {
            const double RenderingEmSize = 1.0d;

            var scale = (12.0 * RenderingEmSize) / s_glyphTypeface.DesignEmHeight;

            var glyphs = s_glyphTypeface.GetGlyphs("ABCDEFGHIJKL");

            var glyphAdvances = s_glyphTypeface.GetGlyphAdvances(glyphs);

            var glyphAdvancesScaled = new double[glyphAdvances.Length];

            for (var i = 0; i < glyphAdvances.Length; i++)
            {
                glyphAdvancesScaled[i] = glyphAdvances[i] * scale;
            }

            var baselineOrigin = new Point(0, -s_glyphTypeface.Ascent * scale);

            var glyphRun = new GlyphRun(s_glyphTypeface, RenderingEmSize, baselineOrigin, glyphs.ToArray(), glyphAdvancesScaled);

            drawingContext.DrawGlyphRun(Brushes.Black, glyphRun);
        }
    }
}
