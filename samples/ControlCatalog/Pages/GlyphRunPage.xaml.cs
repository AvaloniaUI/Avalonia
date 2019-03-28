using System;
using System.Text;

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

            const double FontRenderingEmSize = 36;

            var scale = FontRenderingEmSize / glyphTypeface.DesignEmHeight;

            var bytes = Encoding.UTF32.GetBytes("ABCDEFGHIJKL");

            var codePoints = new int[bytes.Length / 4];

            Buffer.BlockCopy(bytes, 0, codePoints, 0, bytes.Length);

            var glyphs = glyphTypeface.GetGlyphs(codePoints);

            var baselineOrigin = new Point(0, -glyphTypeface.Ascent * scale);

            var glyphRun = new GlyphRun(glyphTypeface, FontRenderingEmSize, baselineOrigin, glyphs);

            drawingContext.DrawGlyphRun(Brushes.Black, glyphRun);
        }
    }
}
