using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Skia
{
    unsafe class FormattedTextImpl : PerspexHandleHolder, IFormattedTextImpl
    {
        private readonly NativeFormattedText* _shared;
        private readonly string _text;

        public static FormattedTextImpl Create(string text, string fontFamilyName, double fontSize, FontStyle fontStyle,
            TextAlignment textAlignment, FontWeight fontWeight)
        {
            NativeFormattedText* pShared;
            fixed (void* ptext = text)
            {
                IntPtr handle = MethodTable.Instance.CreateFormattedText(ptext, text.Length,
                    TypefaceCache.GetTypeface(fontFamilyName, fontStyle, fontWeight),
                    (float) fontSize, textAlignment, &pShared);
                return new FormattedTextImpl(handle, pShared, text);
            }
        }
        
        List<FormattedTextLine> _lines = new List<FormattedTextLine>();
        List<Rect> _rects = new List<Rect>();
        Size _size;

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

        public void SetForegroundBrush(Brush brush, int startIndex, int length)
        {
        }


        void Reload()
        {
            _lines.Clear();
            _rects.Clear();
            float maxX = 0;
            for (var c = 0; c < _shared->LineCount; c++)
            {
                var w = _shared->Lines[c].Width;
                if (maxX < w)
                    maxX = w;
                _lines.Add(new FormattedTextLine(_shared->Lines[c].Length, _shared->Lines[c].Height));
            }
            for (var c = 0; c < _text.Length; c++)
                _rects.Add(_shared->Bounds[c].ToRect());
            if (_shared->LineCount == 0)
                _size = new Size();
            else
            {
                var lastLine = _shared->Lines[_shared->LineCount - 1];
                _size = new Size(maxX, lastLine.Top + lastLine.Height);
            }
        }

        void Rebuild()
        {
            MethodTable.Instance.RebuildFormattedText(Handle);
            Reload();
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

                _shared->WidthConstraint = (_constraint.Width != double.PositiveInfinity)
                    ? (float) _constraint.Width
                    : -1;
                Rebuild();
            }
        }

        protected override void Delete(IntPtr handle)
        {
            MethodTable.Instance.DestroyFormattedText(handle);
        }

        public FormattedTextImpl(IntPtr handle, NativeFormattedText* shared, string text) : base(handle)
        {
            _shared = shared;
            _text = text;
            Reload();
        }

        public override string ToString()
        {
            return _text;
        }
    }
}
