// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;
using System.Globalization;
using Perspex;
using Perspex.Media;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using TheArtOfDev.HtmlRenderer.Core.Utils;
using TheArtOfDev.HtmlRenderer.Perspex.Utilities;

namespace TheArtOfDev.HtmlRenderer.Perspex.Adapters
{
    /// <summary>
    /// Adapter for Perspex Graphics.
    /// </summary>
    internal sealed class GraphicsAdapter : RGraphics
    {
        #region Fields and Consts

        /// <summary>
        /// The wrapped Perspex graphics object
        /// </summary>
        private readonly IDrawingContext _g;

        /// <summary>
        /// if to release the graphics object on dispose
        /// </summary>
        private readonly bool _releaseGraphics;

        #endregion


        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="g">the Perspex graphics object to use</param>
        /// <param name="initialClip">the initial clip of the graphics</param>
        /// <param name="releaseGraphics">optional: if to release the graphics object on dispose (default - false)</param>
        public GraphicsAdapter(IDrawingContext g, RRect initialClip, bool releaseGraphics = false)
            : base(PerspexAdapter.Instance, initialClip)
        {
            ArgChecker.AssertArgNotNull(g, "g");

            _g = g;
            _releaseGraphics = releaseGraphics;
        }

        /// <summary>
        /// Init.
        /// </summary>
        public GraphicsAdapter()
            : base(PerspexAdapter.Instance, RRect.Empty)
        {
            _g = null;
            _releaseGraphics = false;
        }
        
        public override void PopClip()
        {
            /*
            _g.Pop();
            _clipStack.Pop();
            */
        }

        public override void PushClip(RRect rect)
        {
            //_clipStack.Push(rect);
            //_g.PushClip(new RectangleGeometry(Utils.Convert(rect)));
        }

        public override void PushClipExclude(RRect rect)
        {
            //var geometry = new CombinedGeometry();
            //geometry.Geometry1 = new RectangleGeometry(Utils.Convert(_clipStack.Peek()));
            //geometry.Geometry2 = new RectangleGeometry(Utils.Convert(rect));
            //geometry.GeometryCombineMode = GeometryCombineMode.Exclude;

            //_clipStack.Push(_clipStack.Peek());
            //_g.PushClip(geometry);
        }

        public override Object SetAntiAliasSmoothingMode()
        {
            return null;
        }

        public override void ReturnPreviousSmoothingMode(Object prevMode)
        { }

        public override RSize MeasureString(string str, RFont font)
        {
            var text = GetText(str, font);
            var measure = text.Measure();
            return new RSize(measure.Width, measure.Height);
            
        }

        FormattedText GetText(string str, RFont font)
        {
            var f = ((FontAdapter)font);
            return new FormattedText(str, f.Name, font.Size, f.FontStyle, TextAlignment.Left, f.Weight);
        }

        public override void MeasureString(string str, RFont font, double maxWidth, out int charFit, out double charFitWidth)
        {
            var text = GetText(str, font);
            charFit = str.Length;
            charFitWidth = text.Measure().Width;
        }
        
        public override void DrawString(string str, RFont font, RColor color, RPoint point, RSize size, bool rtl)
        {
            var text = GetText(str, font);
            text.Constraint = Util.Convert(size);
            _g.DrawText(new SolidColorBrush(Util.Convert(color)), Util.Convert(point), text);

            //var colorConv = ((BrushAdapter)_adapter.GetSolidBrush(color)).Brush;

            //bool glyphRendered = false;
            //GlyphTypeface glyphTypeface = ((FontAdapter)font).GlyphTypeface;
            //if (glyphTypeface != null)
            //{
            //    double width = 0;
            //    ushort[] glyphs = new ushort[str.Length];
            //    double[] widths = new double[str.Length];

            //    int i = 0;
            //    for (; i < str.Length; i++)
            //    {
            //        ushort glyph;
            //        if (!glyphTypeface.CharacterToGlyphMap.TryGetValue(str[i], out glyph))
            //            break;

            //        glyphs[i] = glyph;
            //        width += glyphTypeface.AdvanceWidths[glyph];
            //        widths[i] = 96d / 72d * font.Size * glyphTypeface.AdvanceWidths[glyph];
            //    }

            //    if (i >= str.Length)
            //    {
            //        point.Y += glyphTypeface.Baseline * font.Size * 96d / 72d;
            //        point.X += rtl ? 96d / 72d * font.Size * width : 0;

            //        glyphRendered = true;
            //        var glyphRun = new GlyphRun(glyphTypeface, rtl ? 1 : 0, false, 96d / 72d * font.Size, glyphs, Utils.ConvertRound(point), widths, null, null, null, null, null, null);
            //        _g.DrawGlyphRun(colorConv, glyphRun);
            //    }
            //}

            //if (!glyphRendered)
            //{
            //    var formattedText = new FormattedText(str, CultureInfo.CurrentCulture, rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, ((FontAdapter)font).Font, 96d / 72d * font.Size, colorConv);
            //    point.X += rtl ? formattedText.Width : 0;
            //    _g.DrawText(formattedText, Utils.ConvertRound(point));
            //}
        }

        public override RBrush GetTextureBrush(RImage image, RRect dstRect, RPoint translateTransformLocation)
        {
            //TODO: Implement texture brush
            return PerspexAdapter.Instance.GetSolidBrush(Util.Convert(Colors.Magenta));

            //var brush = new ImageBrush(((ImageAdapter)image).Image);
            //brush.Stretch = Stretch.None;
            //brush.TileMode = TileMode.Tile;
            //brush.Viewport = Utils.Convert(dstRect);
            //brush.ViewportUnits = BrushMappingMode.Absolute;
            //brush.Transform = new TranslateTransform(translateTransformLocation.X, translateTransformLocation.Y);
            //brush.Freeze();
            //return new BrushAdapter(brush);
        }
        
        public override RGraphicsPath GetGraphicsPath()
        {
            return new GraphicsPathAdapter();
        }

        public override void Dispose()
        {
            //TODO: Do something about Dispose
            //if (_releaseGraphics)
            //    _g.Close();
        }

    
        #region Delegate graphics methods

        public override void DrawLine(RPen pen, double x1, double y1, double x2, double y2)
        {
            x1 = (int)x1;
            x2 = (int)x2;
            y1 = (int)y1;
            y2 = (int)y2;

            var adj = pen.Width;
            if (Math.Abs(x1 - x2) < .1 && Math.Abs(adj % 2 - 1) < .1)
            {
                x1 += .5;
                x2 += .5;
            }
            if (Math.Abs(y1 - y2) < .1 && Math.Abs(adj % 2 - 1) < .1)
            {
                y1 += .5;
                y2 += .5;
            }

            _g.DrawLine(((PenAdapter)pen).CreatePen(), new Point(x1, y1), new Point(x2, y2));
        }
        
        public override void DrawRectangle(RPen pen, double x, double y, double width, double height)
        {
            var adj = pen.Width;
            if (Math.Abs(adj % 2 - 1) < .1)
            {
                x += .5;
                y += .5;
            }
            _g.DrawRectange(((PenAdapter) pen).CreatePen(), new Rect(x, y, width, height));
        }

        public override void DrawRectangle(RBrush brush, double x, double y, double width, double height)
        {
            _g.FillRectange(((BrushAdapter) brush).Brush, new Rect(x, y, width, height));
        }

        public override void DrawImage(RImage image, RRect destRect, RRect srcRect)
        {
            _g.DrawImage(((ImageAdapter) image).Image, 1, Util.Convert(srcRect), Util.Convert(destRect));
        }

        public override void DrawImage(RImage image, RRect destRect)
        {
            _g.DrawImage(((ImageAdapter) image).Image, 1, new Rect(0, 0, image.Width, image.Height),
                Util.Convert(destRect));
        }

        public override void DrawPath(RPen pen, RGraphicsPath path)
        {
            _g.DrawGeometry(null, ((PenAdapter)pen).CreatePen(), ((GraphicsPathAdapter)path).GetClosedGeometry());
        }

        public override void DrawPath(RBrush brush, RGraphicsPath path)
        {
            _g.DrawGeometry(((BrushAdapter)brush).Brush, null, ((GraphicsPathAdapter)path).GetClosedGeometry());
        }

        public override void DrawPolygon(RBrush brush, RPoint[] points)
        {
            if (points != null && points.Length > 0)
            {
                var g = new StreamGeometry();
                using (var context = g.Open())
                {
                    context.BeginFigure(Util.Convert(points[0]), true);
                    for (int i = 1; i < points.Length; i++)
                        context.LineTo(Util.Convert(points[i]));
                }

                _g.DrawGeometry(((BrushAdapter)brush).Brush, null, g);
            }
        }

        #endregion
    }
}