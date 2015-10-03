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
using ATextAlign = Android.Graphics.Paint.Align;
using AAllignment = Android.Text.Layout.Alignment;
using Android.Text;

namespace Perspex.Android.Rendering
{
    public class FormattedTextImpl : IFormattedTextImpl
    {
        private Size _constraint;

        public string String { get; private set; }
        public ATextPaint TextFormatting { get; private set; }
       

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

        public Size Constraint
        {
            get { return _constraint; }
            set { _constraint = value; }
        }

        public void Dispose()
        {
        }

        public IEnumerable<FormattedTextLine> GetLines()
        {
//            throw new NotImplementedException();
			var textLines = String.Split(new[] {System.Environment.NewLine},StringSplitOptions.None);

			var bound = new ARect();
			TextFormatting.GetTextBounds(String, 0, String.Length, bound);

			foreach (var line in textLines) {
				yield return new FormattedTextLine (line.Length, bound.Height ());
			}
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

            StaticLayout mTextLayout = new StaticLayout(String, TextFormatting, (int)Constraint.Width,
				alignment, 1.0f, 0.0f, false);

			var width = mTextLayout.Width;
			var height = mTextLayout.Height;

			return new Size(width, height );
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