using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using Android.Text;
using Perspex.Media;
using Perspex.Platform;
using ATextPaint = Android.Text.TextPaint;
using ARect = Android.Graphics.Rect;
using AString = Java.Lang.String;
using ATextAlign = Android.Graphics.Paint.Align;
using AAllignment = Android.Text.Layout.Alignment;

namespace Perspex.Android.Rendering
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
            TextFormatting = new ATextPaint {TextAlign = textAlignment.ToAndroidGraphics()};
            var style = fontStyle.ToAndroidGraphics();
            if (fontWeight >= FontWeight.Bold)
                style = style == TypefaceStyle.Italic ? TypefaceStyle.BoldItalic : TypefaceStyle.Bold;
            // Override Typespace with default for testing
            TextFormatting.SetTypeface(Typeface.Default);
            TextFormatting.TextSize = (float) fontSize;
            Constraint = Measure();
        }

        public string String { get; }
        public ATextPaint TextFormatting { get; }

        public Size Constraint { get; set; }

        public void Dispose()
        {
        }

        public IEnumerable<FormattedTextLine> GetLines()
        {
//            throw new NotImplementedException();
            var textLines = String.Split(new[] {Environment.NewLine}, StringSplitOptions.None);

            var bound = new ARect();
            TextFormatting.GetTextBounds(String, 0, String.Length, bound);

            return textLines.Select(line => new FormattedTextLine(line.Length, bound.Height()));
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
            var alignment = AAllignment.AlignNormal;

            if (TextFormatting.TextAlign == ATextAlign.Center)
                alignment = AAllignment.AlignCenter;

            var rect = new ARect();
            TextFormatting.GetTextBounds(String, 0, String.Length, rect);

            Constraint = new Size(rect.Width(), rect.Height());

            var mTextLayout = new StaticLayout(String, TextFormatting, (int) Constraint.Width,
                alignment, 1.0f, 0.0f, false);

            var width = mTextLayout.Width;
            var height = mTextLayout.Height;

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