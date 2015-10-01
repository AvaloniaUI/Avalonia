using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Java.Text;
using Perspex.Media;
using Perspex.Platform;
using ATextPaint = Android.Text.TextPaint;
using ARect = Android.Graphics.Rect;
using AString = Java.Lang.String;

namespace Perspex.Android.Rendering
{
    public class FormattedTextImpl : IFormattedTextImpl
    {
        private Size _constraint;

        public string String { get; private set; }
        public ATextPaint TextFormatting { get; private set; }
        public ARect Bounds { get; private set; }

        public FormattedTextImpl(
            string text,
            string fontFamily,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight)
        {
         
            String = text;
            TextFormatting = new ATextPaint {TextAlign = textAlignment.ToAndroidGraphics()};
            var style = fontStyle.ToAndroidGraphics();
            if (fontWeight >= FontWeight.Bold)
                style = style == TypefaceStyle.Italic ? TypefaceStyle.BoldItalic : TypefaceStyle.Bold;
            TextFormatting.SetTypeface(Typeface.Create(fontFamily, style));
            TextFormatting.TextSize = (float) fontSize;
            TextFormatting.GetTextBounds(String, 0, String.Length, Bounds);
        }

        public Size Constraint
        {
            get { return _constraint; }
            set { _constraint = value; }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            throw new NotImplementedException();
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            throw new NotImplementedException();
        }

        public Rect HitTestTextPosition(int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            throw new NotImplementedException();
        }

        public Size Measure()
        {
            //TODO: Have the slightest feeling this is a disconnect here...
            var metrics = TextFormatting.GetFontMetrics();
            float height = Math.Abs(metrics.Ascent) + Math.Abs(metrics.Descent);
            float width = TextFormatting.MeasureText(String);
            return new Size(width, height);
        }

        public void SetForegroundBrush(Brush brush, int startIndex, int length)
        {
            var scb = brush as SolidColorBrush;
            if (scb != null)
            {
                TextFormatting.Color = scb.Color.ToAndroidGraphics();
            }
        }
    }
}