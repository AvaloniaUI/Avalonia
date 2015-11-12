using Android.Graphics;
using Android.Text;
using Perspex.Android.Platform.CanvasPlatform;
using Perspex.Android.Platform.Specific;
using Perspex.Media;
using Perspex.Platform;
using System;
using System.Collections.Generic;
using AAllignment = Android.Text.Layout.Alignment;
using ARect = Android.Graphics.Rect;
using ATextPaint = Android.Text.TextPaint;

namespace Perspex.Android.CanvasRendering
{
    public class FormattedTextImpl : IFormattedTextImpl
    {
        public FormattedTextImpl(
            string text,
            string fontFamily,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight)
        {
            String = text;
            TextPaint = new ATextPaint { TextAlign = textAlignment.ToAndroidGraphics() };
            var typefaceStyle = fontStyle.ToAndroidGraphicsTypefaceStyle(fontWeight);
            var typeface = fontStyle.ToAndroidGraphicsTypeface(fontWeight);
            TextPaint.SetTypeface(typeface);
            TextPaint.TextSize = PointUnitService.Instance.PerspexToNativeFontSize(fontSize);

            Aligment = textAlignment.ToAndroidGraphicsLayoutAligment();

            var rect = new ARect();
            TextPaint.GetTextBounds(String, 0, String.Length, rect);
            //or use measure text to give us other opinion for the text ???
            //double tw = TextFormatting.MeasureText(String, 0, String.Length);

            //give few pixels in advance just in case
            _initialSize = PointUnitService.Instance.NativeToPerspex(new Size(rect.Right, rect.Bottom));
            Constraint = _initialSize;
        }

        private AAllignment Aligment;

        public string String { get; }

        public ATextPaint TextPaint { get; }

        private Size _initialSize;

        public Size Constraint { get; set; }

        public void Dispose()
        {
        }

        private StaticLayout _layout = null;
        private double _layoutBuildWidth = 0;

        private Dictionary<string, KeyValuePair<object, object>> _localCachedValues = new Dictionary<string, KeyValuePair<object, object>>();

        private T GetLocalCachedValue<T>(string name, object forKey, Func<T> getFunc)
        {
            KeyValuePair<object, object> kvp = default(KeyValuePair<object, object>);

            bool create = true;
            if (_localCachedValues.ContainsKey(name))
            {
                kvp = _localCachedValues[name];
                create = kvp.Key != forKey;
            }

            if (create)
            {
                kvp = new KeyValuePair<object, object>(forKey, getFunc());
                _localCachedValues[name] = kvp;
            }

            return (T)kvp.Value;
        }

        public StaticLayout TextLayout
        {
            get
            {
                if (_layout == null || Math.Abs(_layoutBuildWidth - Constraint.Width) > 1)
                {
                    var newWidth = (Constraint.Width > int.MaxValue ? _initialSize.Width : Constraint.Width) + 10;

                    double widthDiff = _layoutBuildWidth - newWidth;

                    if (_layout == null || widthDiff < 0)
                    {
                        _layoutBuildWidth = newWidth;
                        _layout = new StaticLayout(String, TextPaint, PointUnitService.Instance.PerspexToNativeXInt(_layoutBuildWidth), Aligment, 1.0f, 0.0f, false);
                        _localCachedValues.Clear();
                    }
                }

                return _layout;
            }
        }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            return GetLocalCachedValue("GetLines", "GetLines", () =>
            {
                List<FormattedTextLine> result = new List<FormattedTextLine>();
                for (int i = 0; i < TextLayout.LineCount; i++)
                {
                    int start = TextLayout.GetLineStart(i);
                    int end = TextLayout.GetLineEnd(i);
                    //string lineContent = String.Substring(start, end - start);
                    result.Add(new FormattedTextLine(end - start, PointUnitService.Instance.NativeToPerspexY(TextLayout.GetLineTop(i) - TextLayout.GetLineBottom(i))));
                }
                return result;
            });
        }

        public TextHitTestResult HitTestPoint(Point ppoint)
        {
            return GetLocalCachedValue("HitTestPoint", ppoint, () =>
            {
                var point = PointUnitService.Instance.PerspexToNative(ppoint);

                bool inside = point.X >= 0 && point.X <= TextLayout.Width && point.Y >= 0 && point.Y <= TextLayout.Height;
                int index = -1;

                if (inside)
                {
                    int line = this.TextLayout.GetLineForVertical(Convert.ToInt32(point.Y));
                    index = this.TextLayout.GetOffsetForHorizontal(line, (float)point.X);
                }

                return new TextHitTestResult { IsInside = inside, IsTrailing = false, TextPosition = index };
            });
        }

        public Rect HitTestTextPosition(int index)
        {
            return
                GetLocalCachedValue("HitTestTextPosition", index, () =>
                {
                    int line = TextLayout.GetLineForOffset(index);
                    int top = TextLayout.GetLineTop(line);
                    int bottom = TextLayout.GetLineBottom(line);
                    double left = TextLayout.GetPrimaryHorizontal(index);

                    double right = String.Length > index + 1 ? TextLayout.GetPrimaryHorizontal(index + 1) : left;

                    return PointUnitService.Instance.NativeToPerspex(new Rect(left, top, right - left, bottom - top));
                });
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            int startLine = TextLayout.GetLineForOffset(index);
            int endIndex = index + length;
            int endLine = TextLayout.GetLineForOffset(endIndex);

            for (int i = startLine; i <= endLine; i++)
            {
                int top = TextLayout.GetLineTop(i);
                int bottom = TextLayout.GetLineBottom(i);

                double left = 0, right = 0;

                if (startLine == i)
                {
                    left = TextLayout.GetPrimaryHorizontal(index);
                }
                else
                {
                    left = TextLayout.GetLineLeft(i);
                }

                if (endLine == i)
                {
                    right = TextLayout.GetPrimaryHorizontal(endIndex);
                }
                else
                {
                    right = TextLayout.GetLineRight(i);
                }

                yield return PointUnitService.Instance.NativeToPerspex(new Rect(left, top, right - left, bottom - top));
            }
        }

        public Size Measure()
        {
            return PointUnitService.Instance.NativeToPerspex(new Size(this.TextLayout.Width, this.TextLayout.Height));
        }

        public void SetForegroundBrush(Brush brush, int startIndex, int length)
        {
            var scb = brush as SolidColorBrush;
            if (scb != null)
            {
                TextPaint.Color = scb.Color.ToAndroidGraphics();
            }
        }
    }
}