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

            var bytes = Encoding.UTF32.GetBytes("ABCDEFGHIJKL");

            var codePoints = new int[bytes.Length / 4];

            Buffer.BlockCopy(bytes, 0, codePoints, 0, bytes.Length);

            var glyphs = glyphTypeface.GetGlyphs(codePoints);

            var baselineOrigin = new Point(Bounds.X, Bounds.Y);

            for (var i = 12; i < 32; i++)
            {
                var glyphRun = new GlyphRun(glyphTypeface, i, baselineOrigin, glyphs);

                drawingContext.DrawGlyphRun(Brushes.Black, glyphRun);

                baselineOrigin += new Point(0, glyphRun.Size.Height);
            }
        }
    }
}
