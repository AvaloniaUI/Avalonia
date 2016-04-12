using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Perspex.Media;
using Perspex.Platform;
using SkiaSharp;

namespace Perspex.Skia
{
    unsafe class FormattedTextImpl : IFormattedTextImpl
    {
		public SKPaint Paint { get; private set; }

        //private readonly NativeFormattedText* _shared;
        private readonly string _text;

		public FormattedTextImpl(string text)
		{
			_text = text;
			Paint = new SKPaint();

			//Length = length;
			Paint.TextEncoding = SKTextEncoding.Utf16;
			Paint.IsStroke = false;
			Paint.IsAntialias = true;
			LineOffset = 0;

			//Shared.WidthConstraint = -1.0;
			//Data = new pchar[length + 1];
			//memcpy(Data, data, length * 2);
			//Data[length] = 0;

			//Replace 0 characters with zero-width spaces (200B)
			_text = _text.Replace((char)0, (char) 0x200B);

			//Rebuild();
		}

		public static FormattedTextImpl Create(string text, string fontFamilyName, double fontSize, FontStyle fontStyle,
            TextAlignment textAlignment, FontWeight fontWeight)
        {
			var typeface = TypefaceCache.GetTypeface(fontFamilyName, fontStyle, fontWeight);

			FormattedTextImpl instance = new FormattedTextImpl(text);
			instance.Paint.Typeface = typeface;
			instance.Paint.TextSize = (float) fontSize;

			//rv->Paint.setTextAlign(align); //TODO: Manually align
			instance.Rebuild();

			//NativeFormattedText* pShared;
			//fixed (void* ptext = text)
			//{
			//    IntPtr handle = MethodTable.Instance.CreateFormattedText(ptext, text.Length,
			//        TypefaceCache.GetTypeface(fontFamilyName, fontStyle, fontWeight),
			//        (float) fontSize, textAlignment, &pShared);

			//    return new FormattedTextImpl(handle, pShared, text);
			//}

			return instance;
		}

		readonly List<FormattedTextLine> _lines = new List<FormattedTextLine>();
        readonly List<Rect> _rects = new List<Rect>();

		List<PerspexFormattedTextLine> _skiaLines;
		SKRect[] _skiaRects;

		Size _size;

		const float MAX_LINE_WIDTH = 10000;
		float LineOffset;
		float WidthConstraint = -1;

		struct PerspexFormattedTextLine
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
                        IsTrailing = (point.X - rc.X) > rc.Width/2
                    };

                }
            }
            bool end = point.X > _size.Width || point.Y > _size.Height;
            return new TextHitTestResult() {IsTrailing = end, TextPosition = end ? _text.Length - 1 : 0};
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
        }

		void Rebuild()
        {
			var length = _text.Length;

			_lines.Clear();

			_skiaRects = new SKRect[length];
			_skiaLines = new List<PerspexFormattedTextLine>();

			int curOff = 0;
			int curY = 0;

			// TODO: cannot find Font Metrics in SkiaSharp!
			//SKPaint.FontMetrics metrics;
			float lineHeight = Paint.TextSize;  // Paint.getFontMetrics(&metrics);
			LineOffset = 0; // -metrics.fTop;

			string subString;

			byte[] bytes;
			GCHandle pinnedArray;
			IntPtr pointer;

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

					// This method is not linking into SkiaSharp so we must use the RAW buffer version
					//measured = (int)Paint.BreakText(subString, constraint, out lineWidth) / 2;
					bytes = Encoding.ASCII.GetBytes(subString);
					pinnedArray = GCHandle.Alloc(bytes, GCHandleType.Pinned);
					pointer = pinnedArray.AddrOfPinnedObject();
					measured = (int)Paint.BreakText(pointer, (IntPtr)bytes.Length, constraint, out lineWidth);

					// some weird unicode byte issue again
					if(subString.Length % 2 == 1)
					{
						measured -= 1;
					}

					pinnedArray.Free();

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
						//Perform scan for the last space and end the line there
						for (int si = curOff + measured - 1; si > curOff; si--)
						{
							if (_text[si] == ' ')
							{
								measured = si - curOff;
								extraSkip = 1;
								//Rects[si] = SkRect();
								break;
							}
						}
					}


				}

				PerspexFormattedTextLine line = new PerspexFormattedTextLine();
				line.Start = curOff;
				line.Length = measured;
				line.Width = lineWidth;
				line.Height = lineHeight;
				line.Top = curY;

				if (line.Width < 0)
					line.Width = _skiaRects[line.Start + line.Length - 1].Right;

				//Build character rects
				for (int i = line.Start; i < line.Start + line.Length; i++)
				{
					float prevRight = 0;
					if (i != line.Start)
						prevRight = _skiaRects[i - 1].Right;

					subString = _text.Substring(line.Start, i - line.Start + 1);

					// Unfortunately this version appears to be incorrect and skipping
					// even characters. Some issue with Unicode?
					//float w = Paint.MeasureText(subString);
					bytes = Encoding.ASCII.GetBytes(subString);
					pinnedArray = GCHandle.Alloc(bytes, GCHandleType.Pinned);
					pointer = pinnedArray.AddrOfPinnedObject();
					float w = Paint.MeasureText(pointer, (IntPtr)bytes.Length);
					pinnedArray.Free();

					SKRect rc;
					rc.Left = prevRight;
					rc.Right = w;
					rc.Top = line.Top;
					rc.Bottom = line.Top + line.Height;
					_skiaRects[i] = rc;
				}

				subString = _text.Substring(line.Start, line.Length);

				// Unfortunately this version appears to be incorrect and skipping
				// even characters. Some issue with Unicode?
				//line.Width = Paint.MeasureText(subString);
				bytes = Encoding.ASCII.GetBytes(subString);
				pinnedArray = GCHandle.Alloc(bytes, GCHandleType.Pinned);
				pointer = pinnedArray.AddrOfPinnedObject();
				line.Width = Paint.MeasureText(pointer, (IntPtr)bytes.Length);
				pinnedArray.Free();

				_skiaLines.Add(line);

				curY += (int)lineHeight;
				curOff += measured + extraSkip;
			}

			//Shared.Lines = Lines.data();
			//Shared.CharRects = Rects.data();

			// Now convert to Perspex data formats
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
				_rects.Add(_skiaRects[c].ToPerspexRect());

			if (_skiaLines.Count == 0)
				_size = new Size();
			else
			{
				var lastLine = _skiaLines[_skiaLines.Count - 1];
				_size = new Size(maxX, lastLine.Top + lastLine.Height);
			}
		}

		internal void Draw(SKCanvas canvas, /*PerspexBrush* foreground, */ SKPoint origin)
		{
			SKPaint paint = Paint;
			//ConfigurePaint(paint, ctx, foreground);

			/*
			//Debugging code for character positions
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

			for (int c = 0; c < _skiaLines.Count; c++)
			{
				PerspexFormattedTextLine line = _skiaLines[c];
				var subString = _text.Substring(line.Start, line.Length);
				canvas.DrawText(subString, origin.X, origin.Y + line.Top + LineOffset, paint);
			}
		}

		Size _constraint = new Size(double.PositiveInfinity, double.PositiveInfinity);

        public Size Constraint
        {
            get { return _constraint; }
            set
            {
                if(_constraint == value)
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
