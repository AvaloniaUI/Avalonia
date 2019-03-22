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
            var typeface = new Typeface("Arial");

            using (var glyphTypeface = new GlyphTypeface(typeface))
            {
                const double RenderingEmSize = 1.0d;

                const double Scale = (12.0 * RenderingEmSize) / 2048;             

                var glyphs = glyphTypeface.GetGlyphs("ABCDEFGHIJKL");

                var glyphAdvances = glyphTypeface.GetGlyphAdvances(glyphs);

                var glyphAdvancesScaled = new double[glyphAdvances.Length];

                for (var i = 0; i < glyphAdvances.Length; i++)
                {
                    glyphAdvancesScaled[i] = glyphAdvances[i] * Scale;
                }

                var baselineOrigin = new Point(0, -glyphTypeface.Ascent * Scale);

                var glyphRun = new GlyphRun(glyphTypeface, RenderingEmSize, baselineOrigin, glyphs.ToArray(), glyphAdvancesScaled);

                drawingContext.DrawGlyphRun(Brushes.Black, glyphRun);
            }
        }
    }
}
