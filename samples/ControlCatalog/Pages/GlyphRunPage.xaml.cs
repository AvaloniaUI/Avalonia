using System;
using System.Linq;
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

            var bytes = Encoding.UTF32.GetBytes("1234567890");

            var codePoints = new int[bytes.Length / 4];

            Buffer.BlockCopy(bytes, 0, codePoints, 0, bytes.Length);

            var glyphs = glyphTypeface.GetGlyphs(codePoints);

            var baselineOrigin = new Point(15, 15);

            for (var i = 12; i < 32; i++)
            {
                var spacing = i * 0.3;

                var scale = (float)i / glyphTypeface.DesignEmHeight;

                baselineOrigin += new Point(0, -glyphTypeface.Ascent * scale);

                var offsets = new Vector[10];

                var offsetY = 0.0d;

                for (var j = 0; j < 5; j++)
                {
                    offsets[j] = new Vector(0, offsetY++ * i * 0.06);
                }

                var maxY = offsetY;

                for (var j = 5; j < 10; j++)
                {
                    offsets[j] = new Vector(0, offsetY-- * i * 0.06);
                }

                var glyphRun = new GlyphRun(glyphTypeface, i, baselineOrigin, glyphs, null, offsets);

                using (drawingContext.PushTransformContainer())
                {
                    drawingContext.DrawRectangle(new Pen(Brushes.Orange), glyphRun.Bounds);
                }

                using (drawingContext.PushTransformContainer())
                {
                    drawingContext.DrawLine(new Pen(Brushes.Blue), baselineOrigin, baselineOrigin + new Point(glyphRun.Bounds.Width, 0));
                }

                using (drawingContext.PushTransformContainer())
                {
                    drawingContext.DrawGlyphRun(Brushes.Black, glyphRun);
                }

                baselineOrigin += new Point(0, maxY + spacing);
            }
        }
    }
}
