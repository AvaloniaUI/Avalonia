// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using SkiaSharp;
using Buffer = HarfBuzzSharp.Buffer;

namespace Avalonia.Skia.Text
{
    internal class TextRunsBuilder
    {
        /// <summary>
        ///     Builds a list of text runs.
        /// </summary>
        /// <param name="text">The text to build text runs from.</param>
        /// <param name="defaultTypeface">The default typeface to match against.</param>
        /// <param name="textPointer">The position within the text to build the runs from.</param>
        /// <returns>A list of text runs.</returns>
        public static List<TextRunProperties> Build(ReadOnlySpan<char> text, SKTypeface defaultTypeface, SKTextPointer textPointer = default)
        {
            var textRuns = new List<TextRunProperties>();
            var textPosition = textPointer.StartingIndex;
            var bufferPosition = textPointer.StartingIndex;

            using (var buffer = new Buffer())
            {
                buffer.AddUtf16(text.Slice(textPointer.StartingIndex, textPointer.Length));

                while (bufferPosition < buffer.Length)
                {
                    var currentTypeface = defaultTypeface;

                    var count = CountSupportedCharacters(currentTypeface, defaultTypeface, buffer, bufferPosition, out var charCount);

                    if (count == 0)
                    {
                        var codepoint = (int)buffer.GlyphInfos[bufferPosition].Codepoint;

                        currentTypeface = SKFontManager.Default.MatchCharacter(codepoint);

                        if (currentTypeface != null)
                        {
                            count = CountSupportedCharacters(currentTypeface, defaultTypeface, buffer, bufferPosition, out charCount);
                        }
                        else
                        {
                            // no fallback found
                            currentTypeface = defaultTypeface;

                            var loader = TableLoader.Get(currentTypeface);

                            for (var i = textPosition; i < buffer.GlyphInfos.Length; i++)
                            {
                                var glyphInfo = buffer.GlyphInfos[i];

                                if (loader.Font.GetGlyph(glyphInfo.Codepoint) != 0)
                                {
                                    break;
                                }

                                count++;
                                charCount += glyphInfo.Codepoint > ushort.MaxValue ? 2 : 1;
                            }
                        }

                        // an error has occurred probably corrupted text
                        if (count == 0)
                        {
                            break;
                        }
                    }

                    textRuns.Add(new TextRunProperties(new SKTextPointer(textPosition, charCount), currentTypeface));

                    bufferPosition += count;
                    textPosition += charCount;
                }
            }

            return textRuns;
        }

        /// <summary>
        ///     Counts the number of characters that can be mapped to glyphs./>
        /// </summary>
        /// <param name="defaultTypeface"></param>
        /// <param name="typeface">The typeface that is used to find matching characters.</param>
        /// <param name="buffer">The buffer to count on.</param>
        /// <param name="startingIndex">The starting index within the buffer.</param>
        /// <param name="charCount">Count of matching characters.</param>
        /// <returns>Count of matching codepoints.</returns>
        private static int CountSupportedCharacters(SKTypeface typeface, SKTypeface defaultTypeface, Buffer buffer, int startingIndex, out int charCount)
        {
            var isFallback = typeface != defaultTypeface;

            charCount = 0;
            var count = 0;

            var font = TableLoader.Get(typeface).Font;
            var defaultFont = TableLoader.Get(defaultTypeface).Font;

            for (var i = startingIndex; i < buffer.Length; i++)
            {
                var glyphInfo = buffer.GlyphInfos[i];

                if (isFallback)
                {
                    if (defaultFont.GetGlyph(glyphInfo.Codepoint) != 0)
                    {
                        break;
                    }
                }

                if (font.GetGlyph(glyphInfo.Codepoint) == 0)
                {
                    if (UnicodeUtility.IsZeroSpace(glyphInfo.Codepoint))
                    {
                        count++;
                        charCount++;
                        continue;
                    }

                    if (UnicodeUtility.IsBreakChar(glyphInfo.Codepoint))
                    {
                        count++;
                        charCount++;

                        if (count < buffer.Length)
                        {
                            switch (glyphInfo.Codepoint)
                            {
                                case '\r' when buffer.GlyphInfos[count].Codepoint == '\n':
                                case '\n' when buffer.GlyphInfos[count].Codepoint == '\r':
                                    count++;
                                    charCount++;
                                    break;
                            }
                        }
                    }

                    break;
                }

                count++;
                charCount += glyphInfo.Codepoint > ushort.MaxValue ? 2 : 1;
            }

            return count;
        }
    }
}
