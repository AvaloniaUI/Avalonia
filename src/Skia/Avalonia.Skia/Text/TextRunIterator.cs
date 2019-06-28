// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using HarfBuzzSharp;
using SkiaSharp;
using Buffer = HarfBuzzSharp.Buffer;

namespace Avalonia.Skia.Text
{
    internal static class TextRunIterator
    {
        /// <summary>
        ///     Creates a list of text runs with unique properties.
        /// </summary>
        /// <param name="text">The text to create text runs from.</param>
        /// <param name="defaultTypeface">The default typeface to match against.</param>
        /// <param name="textPointer">The position within the text to build the runs from.</param>
        /// <returns>A list of text runs.</returns>
        public static List<TextRunProperties> Create(ReadOnlySpan<char> text, SKTypeface defaultTypeface,
            SKTextPointer textPointer)
        {
            var textRuns = new List<TextRunProperties>();
            var textPosition = 0;
            var bufferPosition = 0;

            using (var buffer = new Buffer())
            {
                buffer.AddUtf16(text.Slice(textPointer.StartingIndex, textPointer.Length));

                while (bufferPosition < buffer.Length)
                {
                    var currentTypeface = defaultTypeface;

                    if (!TryGetRunProperties(currentTypeface, defaultTypeface, buffer, bufferPosition, out var count,
                        out var charCount, out var script))
                    {
                        var codepoint = (int)buffer.GlyphInfos[bufferPosition].Codepoint;

                        currentTypeface = SKFontManager.Default.MatchCharacter(codepoint);

                        if (currentTypeface == null || !TryGetRunProperties(currentTypeface, defaultTypeface, buffer,
                                bufferPosition, out count,
                                out charCount, out script))
                        {
                            // no fallback found
                            currentTypeface = defaultTypeface;

                            var loader = TableLoader.Get(currentTypeface);

                            for (var i = textPosition; i < buffer.GlyphInfos.Length; i++)
                            {
                                var glyphInfo = buffer.GlyphInfos[i];

                                if (loader.Font.TryGetGlyph(glyphInfo.Codepoint, out _))
                                {
                                    break;
                                }

                                count++;
                                charCount += glyphInfo.Codepoint > ushort.MaxValue ? 2 : 1;
                            }
                        }
                    }

                    textRuns.Add(new TextRunProperties(
                        new SKTextPointer(textPointer.StartingIndex + textPosition, charCount), currentTypeface,
                        script));

                    bufferPosition += count;
                    textPosition += charCount;
                }
            }

            return textRuns;
        }

        /// <summary>
        ///     Tries to get run properties./>
        /// </summary>
        /// <param name="defaultTypeface"></param>
        /// <param name="typeface">The typeface that is used to find matching characters.</param>
        /// <param name="buffer">The buffer to count on.</param>
        /// <param name="startingIndex">The starting index within the buffer.</param>
        /// <param name="count"></param>
        /// <param name="charCount">Count of matching characters.</param>
        /// <param name="script">The run's script.</param>
        /// <returns></returns>
        private static bool TryGetRunProperties(SKTypeface typeface, SKTypeface defaultTypeface, Buffer buffer,
            int startingIndex, out int count, out int charCount, out Script script)
        {
            if (buffer.Length == 0)
            {
                count = 0;
                charCount = 0;
                script = Script.Unknown;
                return false;
            }

            var isFallback = typeface != defaultTypeface;

            count = 0;
            charCount = 0;
            script = Script.Common;
            var currentScript = script;

            var font = TableLoader.Get(typeface).Font;
            var defaultFont = TableLoader.Get(defaultTypeface).Font;

            for (var i = startingIndex; i < buffer.Length; i++)
            {
                var glyphInfo = buffer.GlyphInfos[i];

                script = UnicodeFunctions.Default.GetScript(glyphInfo.Codepoint);

                if (script != currentScript)
                {
                    if (currentScript == Script.Inherited || currentScript == Script.Common)
                    {
                        currentScript = script;
                    }
                    else if (script != Script.Inherited && script != Script.Common)
                    {
                        break;
                    }
                }

                if (isFallback)
                {
                    if (defaultFont.TryGetGlyph(glyphInfo.Codepoint, out _))
                    {
                        break;
                    }
                }

                if (!font.TryGetGlyph(glyphInfo.Codepoint, out _))
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

            return count > 0;
        }
    }
}
