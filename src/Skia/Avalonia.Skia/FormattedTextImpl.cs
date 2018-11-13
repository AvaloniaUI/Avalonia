// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;

using Avalonia.Skia.Text;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia formatted text implementation.
    /// </summary>
    public class FormattedTextImpl : IFormattedTextImpl
    {
        private readonly List<FormattedTextLine> _lines = new List<FormattedTextLine>();

        public FormattedTextImpl(
            string text,
            Typeface typeface,
            TextAlignment textAlignment,
            TextWrapping textWrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            Constraint = constraint;

            Text = text ?? string.Empty;

            // Replace 0 characters with zero-width spaces (200B)
            Text = Text.Replace((char)0, (char)0x200B);

            var skiaTypeface = TypefaceCache.Default;

            if (typeface.FontFamily.Key != null)
            {
                var typefaces = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(typeface.FontFamily);
                skiaTypeface = typefaces.GetTypeFace(typeface);
            }
            else
            {
                if (typeface.FontFamily.FamilyNames.HasFallbacks)
                {
                    foreach (var familyName in typeface.FontFamily.FamilyNames)
                    {
                        skiaTypeface = TypefaceCache.GetTypeface(
                            familyName,
                            typeface.Style,
                            typeface.Weight);
                        if (skiaTypeface != TypefaceCache.Default)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    skiaTypeface = TypefaceCache.GetTypeface(
                        typeface.FontFamily.Name,
                        typeface.Style,
                        typeface.Weight);
                }
            }

            TextLayout = new SKTextLayout(text, skiaTypeface, (float)typeface.FontSize, textAlignment, textWrapping, constraint);

            foreach (var textLine in TextLayout.TextLines)
            {
                _lines.Add(new FormattedTextLine(textLine.Length, textLine.LineMetrics.Size.Height));
            }

            if (spans != null)
            {
                foreach (var span in spans)
                {
                    TextLayout.ApplyTextSpan(span);
                }
            }

            Size = TextLayout.Size;
        }

        public string Text { get; }

        public Size Constraint { get; }

        public Size Size { get; }

        public SKTextLayout TextLayout { get; }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            return _lines;
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            return TextLayout.HitTestPoint(point);
        }

        public Rect HitTestTextPosition(int index)
        {
            return TextLayout.HitTestTextPosition(index);
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            return TextLayout.HitTestTextRange(index, length);
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
