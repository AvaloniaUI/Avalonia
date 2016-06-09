using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Avalonia.Skia
{
    unsafe class FormattedTextImpl : IFormattedTextImpl
    {
        public SKPaint Paint { get; private set; }

        public FormattedTextImpl(string text)
        {
            _text = text;
            Paint = new SKPaint();

            //currently Skia does not measure properly with Utf8 !!!
            //Paint.TextEncoding = SKTextEncoding.Utf8;
            Paint.TextEncoding = SKTextEncoding.Utf16;
            Paint.IsStroke = false;
            Paint.IsAntialias = true;
            LineOffset = 0;

            // Replace 0 characters with zero-width spaces (200B)
            _text = _text.Replace((char)0, (char)0x200B);
        }

        public static FormattedTextImpl Create(string text, string fontFamilyName, double fontSize, FontStyle fontStyle,
            TextAlignment textAlignment, FontWeight fontWeight)
        {
            var typeface = TypefaceCache.GetTypeface(fontFamilyName, fontStyle, fontWeight);

            FormattedTextImpl instance = new FormattedTextImpl(text);
            instance.Paint.Typeface = typeface;
            instance.Paint.TextSize = (float)fontSize;
            instance.Paint.TextAlign = textAlignment.ToSKTextAlign();
            instance.Rebuild();
            return instance;
        }

        private readonly string _text;

        readonly List<FormattedTextLine> _lines = new List<FormattedTextLine>();
        readonly List<Rect> _rects = new List<Rect>();

        List<AvaloniaFormattedTextLine> _skiaLines;
        SKRect[] _skiaRects;

        Size _size;

        const float MAX_LINE_WIDTH = 10000;
        float LineOffset;
        float WidthConstraint = -1;

        struct AvaloniaFormattedTextLine
        {
            public float Top;
            public int Start;
            public int Length;
            public float Height;
            public float Width;
        };

        public IEnumerable<FormattedTextLine> GetLines()
        {
            return _lines;
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            for (int c = 0; c < _rects.Count; c++)
            {
                //TODO: Detect line first
                var rc = _rects[c];
                if (rc.Contains(point))
                {
                    return new TextHitTestResult
                    {
                        IsInside = true,
                        TextPosition = c,
                        IsTrailing = (point.X - rc.X) > rc.Width / 2
                    };
                }
            }
            bool end = point.X > _size.Width || point.Y > _size.Height;
            return new TextHitTestResult() { IsTrailing = end, TextPosition = end ? _text.Length - 1 : 0 };
        }

        public Rect HitTestTextPosition(int index)
        {
            if (index < 0 || index >= _rects.Count)
                return new Rect();
            return _rects[index];
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            for (var c = 0; c < length; c++)
                yield return _rects[c + index];
        }

        public Size Measure()
        {
            return _size;
        }

        public void SetForegroundBrush(IBrush brush, int startIndex, int length)
        {
            // TODO: we need an implementation here to properly support FormattedText
        }

        void Rebuild()
        {
            var length = _text.Length;

            _lines.Clear();

            _skiaRects = new SKRect[length];
            _skiaLines = new List<AvaloniaFormattedTextLine>();

            int curOff = 0;
            float curY = 0;

            var metrics = Paint.FontMetrics;
            var mTop = metrics.Top;  // The greatest distance above the baseline for any glyph (will be <= 0).
            var mBottom = metrics.Bottom;  // The greatest distance below the baseline for any glyph (will be >= 0).
            var mLeading = metrics.Leading;  // The recommended distance to add between lines of text (will be >= 0).

            // This seems like the best measure of full vertical extent
            float lineHeight = mBottom - mTop;

            // Rendering is relative to baseline
            LineOffset = -metrics.Top;

            string subString;

            for (int c = 0; curOff < length; c++)
            {
                float lineWidth = -1;
                int measured;
                int extraSkip = 0;
                if (WidthConstraint <= 0)
                {
                    measured = length;
                }
                else
                {
                    float constraint = WidthConstraint;
                    if (constraint > MAX_LINE_WIDTH)
                        constraint = MAX_LINE_WIDTH;

                    subString = _text.Substring(curOff);

                    measured = (int)Paint.BreakText(subString, constraint, out lineWidth) / 2;

                    if (measured == 0)
                    {
                        measured = 1;
                        lineWidth = -1;
                    }

                    char nextChar = ' ';
                    if (curOff + measured < length)
                        nextChar = _text[curOff + measured];

                    if (nextChar != ' ')
                    {
                        // Perform scan for the last space and end the line there
                        for (int si = curOff + measured - 1; si > curOff; si--)
                        {
                            if (_text[si] == ' ')
                            {
                                measured = si - curOff;
                                extraSkip = 1;
                                break;
                            }
                        }
                    }
                }

                AvaloniaFormattedTextLine line = new AvaloniaFormattedTextLine();
                line.Start = curOff;
                line.Length = measured;
                line.Width = lineWidth;
                line.Height = lineHeight;
                line.Top = curY;

                if (line.Width < 0)
                    line.Width = _skiaRects[line.Start + line.Length - 1].Right;

                // Build character rects
                for (int i = line.Start; i < line.Start + line.Length; i++)
                {
                    float prevRight = 0;
                    if (i != line.Start)
                        prevRight = _skiaRects[i - 1].Right;

                    subString = _text.Substring(line.Start, i - line.Start + 1);
                    float w = Paint.MeasureText(subString);

                    SKRect rc;
                    rc.Left = prevRight;
                    rc.Right = w;
                    rc.Top = line.Top;
                    rc.Bottom = line.Top + line.Height;
                    _skiaRects[i] = rc;
                }

                subString = _text.Substring(line.Start, line.Length);
                line.Width = Paint.MeasureText(subString);

                _skiaLines.Add(line);

                curY += lineHeight;

                // TODO: We may want to consider adding Leading to the vertical line spacing but for now
                // it appears to make no difference. Revisit as part of FormattedText improvements.
                //
                //curY += mLeading;

                curOff += measured + extraSkip;
            }

            // Now convert to Avalonia data formats
            _lines.Clear();
            _rects.Clear();
            float maxX = 0;

            for (var c = 0; c < _skiaLines.Count; c++)
            {
                var w = _skiaLines[c].Width;
                if (maxX < w)
                    maxX = w;

                _lines.Add(new FormattedTextLine(_skiaLines[c].Length, _skiaLines[c].Height));
            }

            for (var c = 0; c < _text.Length; c++)
            {
                _rects.Add(_skiaRects[c].ToAvaloniaRect());
            }

            if (_skiaLines.Count == 0)
            {
                _size = new Size();
            }
            else
            {
                var lastLine = _skiaLines[_skiaLines.Count - 1];
                _size = new Size(maxX, lastLine.Top + lastLine.Height);
            }
        }

        internal void Draw(SKCanvas canvas, SKPoint origin, DrawingContextImpl.PaintWrapper foreground)
        {
            SKPaint paint = Paint;

            /* TODO: This originated from Native code, it might be useful for debugging character positions as
             * we improve the FormattedText support. Will need to port this to C# obviously. Rmove when
             * not needed anymore.

                SkPaint dpaint;
                ctx->Canvas->save();
                ctx->Canvas->translate(origin.fX, origin.fY);
                for (int c = 0; c < Lines.size(); c++)
                {
                    dpaint.setARGB(255, 0, 0, 0);
                    SkRect rc;
                    rc.fLeft = 0;
                    rc.fTop = Lines[c].Top;
                    rc.fRight = Lines[c].Width;
                    rc.fBottom = rc.fTop + LineOffset;
                    ctx->Canvas->drawRect(rc, dpaint);
                }
                for (int c = 0; c < Length; c++)
                {
                    dpaint.setARGB(255, c % 10 * 125 / 10 + 125, (c * 7) % 10 * 250 / 10, (c * 13) % 10 * 250 / 10);
                    dpaint.setStyle(SkPaint::kFill_Style);
                    ctx->Canvas->drawRect(Rects[c], dpaint);
                }
                ctx->Canvas->restore();
            */

            using (foreground.ApplyTo(paint))
            {
                for (int c = 0; c < _skiaLines.Count; c++)
                {
                    AvaloniaFormattedTextLine line = _skiaLines[c];
                    var subString = _text.Substring(line.Start, line.Length);
                    canvas.DrawText(subString, origin.X, origin.Y + line.Top + LineOffset, paint);
                }
            }
        }

        Size _constraint = new Size(double.PositiveInfinity, double.PositiveInfinity);

        public Size Constraint
        {
            get { return _constraint; }
            set
            {
                if (_constraint == value)
                    return;

                _constraint = value;
                WidthConstraint = (_constraint.Width != double.PositiveInfinity)
                    ? (float)_constraint.Width
                    : -1;

                Rebuild();
            }
        }

        public override string ToString()
        {
            return _text;
        }

        public void Dispose()
        {
        }
    }
}