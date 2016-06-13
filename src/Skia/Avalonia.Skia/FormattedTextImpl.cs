using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace Avalonia.Skia
{
    unsafe class FormattedTextImpl : IFormattedTextImpl
    {

        public FormattedTextImpl(string text, TextWrapping wrapping = TextWrapping.NoWrap)
        {
            _text = text;
            _wrapping = wrapping;
            _paint = new SKPaint();

            //currently Skia does not measure properly with Utf8 !!!
            //Paint.TextEncoding = SKTextEncoding.Utf8;
            _paint.TextEncoding = SKTextEncoding.Utf16;
            _paint.IsStroke = false;
            _paint.IsAntialias = true;
            LineOffset = 0;

            // Replace 0 characters with zero-width spaces (200B)
            _text = _text.Replace((char)0, (char)0x200B);
        }

        public static FormattedTextImpl Create(string text, string fontFamilyName, double fontSize, FontStyle fontStyle,
            TextAlignment textAlignment, FontWeight fontWeight, TextWrapping wrapping)
        {
            var typeface = TypefaceCache.GetTypeface(fontFamilyName, fontStyle, fontWeight);

            FormattedTextImpl instance = new FormattedTextImpl(text, wrapping);
            instance._paint.Typeface = typeface;
            instance._paint.TextSize = (float)fontSize;
            instance._paint.TextAlign = textAlignment.ToSKTextAlign();
            instance.Rebuild();
            return instance;
        }

        private readonly SKPaint _paint;
        private readonly string _text;
        private readonly TextWrapping _wrapping;

        private readonly List<FormattedTextLine> _lines = new List<FormattedTextLine>();
        private readonly List<Rect> _rects = new List<Rect>();

        private List<AvaloniaFormattedTextLine> _skiaLines;
        private Size _size;

        const float MAX_LINE_WIDTH = 10000;
        private float LineOffset;
        private float LineHeight;

        struct AvaloniaFormattedTextLine
        {
            public float Top;
            public int Start;
            public int Length;
            public int TextLength;
            public float Height;
            public float Width;
        };

        public IEnumerable<FormattedTextLine> GetLines()
        {
            return _lines;
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            float y = (float)point.Y;
            var line = _skiaLines.Find(l => l.Top <= y && (l.Top + l.Height) > y);

            var rects = GetRects();

            if (!line.Equals(default(AvaloniaFormattedTextLine)))
            {
                for (int c = line.Start; c < line.Start + line.TextLength; c++)
                {
                    //TODO: Detect line first
                    var rc = rects[c];
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

                if (point.X >= line.Width)
                {
                    return new TextHitTestResult
                    {
                        IsInside = false,
                        TextPosition = line.Start + line.Length > 0 ? line.Length - 1 : 0,
                        IsTrailing = true
                    };
                }
                else
                {
                    return new TextHitTestResult
                    {
                        IsInside = line.Length > 0,
                        TextPosition = line.Start,
                        IsTrailing = false
                    };
                }
            }
            bool end = point.X > _size.Width || point.Y > _size.Height;
            return new TextHitTestResult() { IsTrailing = end, TextPosition = end ? _text.Length - 1 : 0 };
        }

        public Rect HitTestTextPosition(int index)
        {
            var rects = GetRects();

            if (index < 0 || index > rects.Count)
                return new Rect();

            if (rects.Count == 0)
            {
                //empty text
                return new Rect(0, 0, 1, LineHeight);
            }

            if (index == rects.Count)
            {
                var lr = rects[rects.Count - 1];
                return new Rect(new Point(lr.X + lr.Width, lr.Y), rects[index - 1].Size);
            }

            return rects[index];
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            var rects = GetRects();

            for (var c = 0; c < length; c++)
                yield return rects[c + index];
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
            _rects.Clear();
            _skiaLines = new List<AvaloniaFormattedTextLine>();

            int curOff = 0;
            float curY = 0;

            var metrics = _paint.FontMetrics;
            var mTop = metrics.Top;  // The greatest distance above the baseline for any glyph (will be <= 0).
            var mBottom = metrics.Bottom;  // The greatest distance below the baseline for any glyph (will be >= 0).
            var mLeading = metrics.Leading;  // The recommended distance to add between lines of text (will be >= 0).
            var mDescent = metrics.Descent;
            // This seems like the best measure of full vertical extent
            LineHeight = mBottom - mTop;

            // Rendering is relative to baseline
            LineOffset = -metrics.Top;

            string subString;

            float widthConstraint = (_constraint.Width != double.PositiveInfinity)
                                        ? (float)_constraint.Width
                                        : -1;

            for (int c = 0; curOff < length; c++)
            {
                float lineWidth = -1;
                int measured;
                int trailingnumber = 0;

                subString = _text.Substring(curOff);

                float constraint = -1;

                if (_wrapping == TextWrapping.Wrap)
                {
                    constraint = widthConstraint <= 0 ? MAX_LINE_WIDTH : widthConstraint;
                    if (constraint > MAX_LINE_WIDTH)
                        constraint = MAX_LINE_WIDTH;
                }

                measured = LineBreak(_text, curOff, length, _paint, constraint, out trailingnumber);

                AvaloniaFormattedTextLine line = new AvaloniaFormattedTextLine();
                line.TextLength = measured;

                subString = _text.Substring(line.Start, line.TextLength);
                lineWidth = _paint.MeasureText(subString);

                // lineHeight = hh;
                line.Start = curOff;
                line.Length = measured - trailingnumber;
                line.Width = lineWidth;
                line.Height = LineHeight;
                line.Top = curY;

                _skiaLines.Add(line);

                curY += LineHeight - mDescent;

                // TODO: We may want to consider adding Leading to the vertical line spacing but for now
                // it appears to make no difference. Revisit as part of FormattedText improvements.
                //
                //curY += mLeading;

                curOff += measured;
            }

            // Now convert to Avalonia data formats
            _lines.Clear();
            float maxX = 0;

            for (var c = 0; c < _skiaLines.Count; c++)
            {
                var w = _skiaLines[c].Width;
                if (maxX < w)
                    maxX = w;

                _lines.Add(new FormattedTextLine(_skiaLines[c].Length, _skiaLines[c].Height));
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

            BuildRects();
        }

        private List<Rect> GetRects()
        {
            if (_text.Length > _rects.Count)
            {
                BuildRects();
            }

            return _rects;
        }

        private void BuildRects()
        {
            // Build character rects
            var fm = _paint.FontMetrics;
            for (int li = 0; li < _skiaLines.Count; li++)
            {
                var line = _skiaLines[li];
                float prevRight = 0;
                double nextTop = line.Top + line.Height;

                if (li + 1 < _skiaLines.Count)
                {
                    nextTop = _skiaLines[li + 1].Top;
                }

                for (int i = line.Start; i < line.Start + line.TextLength; i++)
                {
                    float w = _paint.MeasureText(_text[i].ToString());

                    _rects.Add(new Rect(
                        prevRight,
                        line.Top,
                        w,
                        nextTop - line.Top));
                    prevRight += w;
                }
            }
        }

        internal void Draw(SKCanvas canvas, SKPoint origin, DrawingContextImpl.PaintWrapper foreground)
        {
            SKPaint paint = _paint;

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

                    float x = 0;

                    //this is a quick fix so we have skia rendering
                    //properly right and center align
                    //TODO: find a better implementation including 
                    //hittesting and text selection working properly

                    //paint.TextAlign = SKTextAlign.Right;
                    if (paint.TextAlign == SKTextAlign.Left)
                    {
                        x = origin.X;
                    }
                    else
                    {
                        double width = Constraint.Width > 0 && !double.IsPositiveInfinity(Constraint.Width) ?
                                        Constraint.Width :
                                        _size.Width;

                        switch (_paint.TextAlign)
                        {
                            case SKTextAlign.Center: x = origin.X + (float)width / 2; break;
                            case SKTextAlign.Right: x = origin.X + (float)width; break;
                        }
                    }


                    canvas.DrawText(subString, x, origin.Y + line.Top + LineOffset, paint);
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

        private static bool IsBreakChar(char c)
        {
            //white space or zero space whitespace
            return char.IsWhiteSpace(c) || c == '\u200B';
        }

        private static int LineBreak(string textInput, int textIndex, int stop,
                                     SKPaint paint, float maxWidth,
                                     out int trailingCount)
        {
            int lengthBreak;
            if (maxWidth == -1)
            {
                lengthBreak = stop - textIndex;
            }
            else
            {
                float measuredWidth;
                string subText = textInput.Substring(textIndex, stop - textIndex);
                lengthBreak = (int)paint.BreakText(subText, maxWidth, out measuredWidth) / 2;
            }

            //Check for white space or line breakers before the lengthBreak
            int startIndex = textIndex;
            int index = textIndex;
            int word_start = textIndex;
            bool prevBreak = true;

            trailingCount = 0;

            while (index < stop)
            {
                int prevText = index;
                char currChar = textInput[index++];
                bool currBreak = IsBreakChar(currChar);

                if (!currBreak && prevBreak)
                {
                    word_start = prevText;
                }

                prevBreak = currBreak;

                if (index > startIndex + lengthBreak)
                {
                    if (currBreak)
                    {
                        // eat the rest of the whitespace
                        while (index < stop && IsBreakChar(textInput[index]))
                        {
                            index++;
                        }

                        trailingCount = index - prevText;
                    }
                    else
                    {
                        // backup until a whitespace (or 1 char)
                        if (word_start == startIndex)
                        {
                            if (prevText > startIndex)
                            {
                                index = prevText;
                            }
                        }
                        else
                        {
                            index = word_start;
                        }
                    }
                    break;
                }

                if ('\n' == currChar)
                {
                    int ret = index - startIndex;
                    int lineBreakSize = 1;
                    if (index < stop)
                    {
                        currChar = textInput[index++];
                        if ('\r' == currChar)
                        {
                            ret = index - startIndex;
                            ++lineBreakSize;
                        }
                    }

                    trailingCount = lineBreakSize;

                    return ret;
                }

                if ('\r' == currChar)
                {
                    int ret = index - startIndex;
                    int lineBreakSize = 1;
                    if (index < stop)
                    {
                        currChar = textInput[index++];
                        if ('\n' == currChar)
                        {
                            ret = index - startIndex;
                            ++lineBreakSize;
                        }
                    }

                    trailingCount = lineBreakSize;

                    return ret;
                }
            }

            return index - startIndex;
        }
    }
}