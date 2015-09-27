using Perspex.Platform;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Perspex.Media;
using UIKit;
using Foundation;
using CoreText;
using CoreGraphics;

// May want to consider this newer, simpler API for text rendering:
// http://www.appcoda.com/intro-text-kit-ios-programming-guide/


namespace Perspex.iOS.Rendering
{
    internal class FormattedTextImpl : IFormattedTextImpl
    {
        private Size _constraint;

        public NSMutableAttributedString AttributedString { get; private set; }
        public CTFramesetter Framesetter { get; private set; }

        public FormattedTextImpl(
            string text,
            string fontFamily,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight)
        {
            // TODO: Font Style & Weight are not currently supported here due to CoreText's criptic API
            //

            AttributedString = new NSMutableAttributedString(text);

            CTParagraphStyle alignStyle = new CTParagraphStyle(new CTParagraphStyleSettings
            {
                Alignment = textAlignment.ToCoreGraphics()
            });

            var font = new CTFont(fontFamily, (nfloat)fontSize);

            // Calculate the range of the attributed string
            NSRange stringRange = new NSRange(0, AttributedString.Length);

            // Add style attributes to the attributed string
            AttributedString.AddAttributes(new CTStringAttributes
            {
                ForegroundColorFromContext = true,
                Font = font,
                ParagraphStyle = alignStyle

            }, stringRange);

            // should we cache this here, or generate in the DrawContext?
            Framesetter = new CTFramesetter(AttributedString);
        }

        public Size Constraint
        {
            get { return _constraint; }
            set
            {
                // do we need to adjust the framesetter/layout?
                _constraint = value;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            // not really tested this yet, when would we want this?
            throw new NotImplementedException();

            var path = new CGPath();
            var frame = Framesetter.GetFrame(new NSRange(), path, null);
            var lines = frame.GetLines();
            return lines.Select(l =>
            {
                var bounds = l.GetBounds(CTLineBoundsOptions.UseGlyphPathBounds);
                return new FormattedTextLine((int)l.StringRange.Length, bounds.Height);
            });
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
            NSRange fitRange;   // do we ever care about this (when text overflows bounds)
            var cgSize = Framesetter.SuggestFrameSize(new NSRange(), new CTFrameAttributes(), Constraint.ToCoreGraphics(), out fitRange);
            return cgSize.ToPerspex();
        }

        public void SetForegroundBrush(Brush brush, int startIndex, int length)
        {
            var scb = brush as SolidColorBrush;
            if (scb != null)
            {
                AttributedString.AddAttributes(new CTStringAttributes
                {
                    ForegroundColor = scb.Color.ToCoreGraphics()
                }, new NSRange(startIndex, length));
            }
        }
    }
}
