using Avalonia.Media;
using Avalonia.Media.Text;
using Avalonia.Media.Text.Unicode;
using Avalonia.Platform;
using Avalonia.Utility;
using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;

namespace Avalonia.Direct2D1.Media
{
    internal class TextFormatterImpl : ITextFormatterImpl
    {
        /// <summary>
        ///     Creates a text style run with unique properties.
        /// </summary>
        /// <param name="text">The text to create text runs from.</param>
        /// <param name="defaultStyle"></param>
        /// <returns>A list of text runs.</returns>
        public TextStyleRun CreateShapableTextStyleRun(ReadOnlySlice<char> text, TextStyle defaultStyle)
        {
            var defaultTypeface = defaultStyle.TextFormat.Typeface;

            var currentTypeface = defaultTypeface;

            if (TryGetRunProperties(text, currentTypeface, defaultTypeface, out var count))
            {
                return new TextStyleRun(new TextPointer(text.Start, count), new TextStyle(currentTypeface,
                    defaultStyle.TextFormat.FontRenderingEmSize,
                    defaultStyle.Foreground));

            }

            var codepoint = CodepointReader.Peek(text, count, out _);

            //ToDo: Fix FontFamily fallback
            currentTypeface =
                FontManager.Current.MatchCharacter(codepoint, defaultTypeface.Weight, defaultTypeface.Style);

            if (currentTypeface != null && TryGetRunProperties(text, currentTypeface, defaultTypeface, out count))
            {
                //Fallback found
                return new TextStyleRun(new TextPointer(text.Start, count), new TextStyle(currentTypeface,
                    defaultStyle.TextFormat.FontRenderingEmSize,
                    defaultStyle.Foreground));

            }

            // no fallback found
            currentTypeface = defaultTypeface;

            var glyphTypeface = (GlyphTypefaceImpl)currentTypeface.GlyphTypeface.PlatformImpl;

            for (; count < text.Length;)
            {
                codepoint = CodepointReader.Peek(text, count, out var charCount);

                if (!UnicodeUtility.IsWhiteSpace(codepoint) && glyphTypeface.Font.TryGetGlyph(codepoint, out _))
                {
                    break;
                }

                count += charCount;
            }

            return new TextStyleRun(new TextPointer(text.Start, count),
                new TextStyle(currentTypeface, defaultStyle.TextFormat.FontRenderingEmSize,
                    defaultStyle.Foreground));
        }

        /// <summary>
        ///     Tries to get run properties.
        /// </summary>
        /// <param name="defaultTypeface"></param>
        /// <param name="text"></param>
        /// <param name="typeface">The typeface that is used to find matching characters.</param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static bool TryGetRunProperties(ReadOnlySlice<char> text, Typeface typeface, Typeface defaultTypeface,
            out int count)
        {
            if (text.Length == 0)
            {
                count = 0;
                return false;
            }

            var isFallback = typeface != defaultTypeface;

            count = 0;
            var script = Script.Common;
            var direction = Direction.Invalid;

            var font = ((GlyphTypefaceImpl)typeface.GlyphTypeface.PlatformImpl).Font;
            var defaultFont = ((GlyphTypefaceImpl)defaultTypeface.GlyphTypeface.PlatformImpl).Font;

            for (; count < text.Length;)
            {
                var codepoint = CodepointReader.Peek(text, count, out var charCount);

                var currentScript = UnicodeFunctions.Default.GetScript((uint)codepoint);

                // ToDo: Implement BiDi algorithm
                if (currentScript.HorizontalDirection != direction)
                {
                    if (direction == Direction.Invalid)
                    {
                        direction = currentScript.HorizontalDirection;
                    }
                    else
                    {
                        if (!UnicodeUtility.IsWhiteSpace(codepoint))
                        {
                            break;
                        }
                    }
                }

                if (currentScript != script)
                {
                    if (currentScript != Script.Inherited && currentScript != Script.Common)
                    {
                        if (script == Script.Inherited || script == Script.Common)
                        {
                            script = currentScript;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (isFallback)
                {
                    if (defaultFont.TryGetGlyph(codepoint, out _))
                    {
                        break;
                    }
                }

                if (!font.TryGetGlyph(codepoint, out _))
                {
                    if (!UnicodeUtility.IsWhiteSpace(codepoint))
                    {
                        break;
                    }
                }

                count += charCount;
            }

            return count > 0;
        }

        public GlyphRun CreateShapedGlyphRun(ReadOnlySlice<char> text, TextFormat textFormat)
        {
            using (var buffer = new Buffer())
            {
                buffer.ContentType = ContentType.Unicode;

                var breakCharPosition = text.Length - 1;

                if (UnicodeUtility.IsBreakChar(text[breakCharPosition]))
                {
                    var breakCharCount = 1;

                    if (text.Length > 1)
                    {
                        if (text[breakCharPosition] == '\r' && text[breakCharPosition - 1] == '\n'
                            || text[breakCharPosition] == '\n' && text[breakCharPosition - 1] == '\r')
                        {
                            breakCharCount = 2;
                        }
                    }

                    if (breakCharPosition != text.Start)
                    {
                        buffer.AddUtf16(text.Buffer.Span, text.Start, text.Length - breakCharCount);
                    }

                    var cluster = buffer.GlyphInfos.Length > 0 ?
                        buffer.GlyphInfos[buffer.Length - 1].Cluster + 1 :
                        (uint)text.Start;

                    switch (breakCharCount)
                    {
                        case 1:
                            buffer.Add('\u200C', cluster);
                            break;
                        case 2:
                            buffer.Add('\u200C', cluster);
                            buffer.Add('\u200D', cluster);
                            break;
                    }
                }
                else
                {
                    buffer.AddUtf16(text.Buffer.Span, text.Start, text.Length);
                }

                buffer.GuessSegmentProperties();

                var glyphTypeface = textFormat.Typeface.GlyphTypeface;

                var font = ((GlyphTypefaceImpl)glyphTypeface.PlatformImpl).Font;

                font.Shape(buffer);

                font.GetScale(out var scaleX, out _);

                var textScale = textFormat.FontRenderingEmSize / scaleX;

                var len = buffer.Length;

                var info = buffer.GetGlyphInfoSpan();

                var pos = buffer.GetGlyphPositionSpan();

                var glyphIndices = new ushort[len];

                var clusters = new ushort[len];

                var glyphAdvances = new double[len];

                var glyphOffsets = new Vector[len];

                for (var i = 0; i < len; i++)
                {
                    glyphIndices[i] = (ushort)info[i].Codepoint;

                    clusters[i] = (ushort)info[i].Cluster;

                    var advanceX = pos[i].XAdvance * textScale;
                    // Depends on direction of layout
                    //var advanceY = pos[i].YAdvance * textScale;

                    glyphAdvances[i] = advanceX;

                    var offsetX = pos[i].XOffset * textScale;
                    var offsetY = pos[i].YOffset * textScale;

                    glyphOffsets[i] = new Vector(offsetX, offsetY);
                }

                return new GlyphRun(glyphTypeface, textFormat.FontRenderingEmSize,
                    new ReadOnlySlice<ushort>(glyphIndices),
                    new ReadOnlySlice<double>(glyphAdvances),
                    new ReadOnlySlice<Vector>(glyphOffsets),
                    text,
                    new ReadOnlySlice<ushort>(clusters));
            }
        }
    }
}
