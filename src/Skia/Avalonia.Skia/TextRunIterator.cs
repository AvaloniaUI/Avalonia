// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Text;
using Avalonia.Media.Text.Unicode;
using Avalonia.Utility;
using HarfBuzzSharp;

namespace Avalonia.Skia
{
    internal static class TextRunIterator
    {
        /// <summary>
        ///     Creates a list of text runs with unique properties.
        /// </summary>
        /// <param name="text">The text to create text runs from.</param>
        /// <param name="defaultRunStyle"></param>
        /// <returns>A list of text runs.</returns>
        public static List<TextRunProperties> Create(ReadOnlySlice<char> text, TextRunStyle defaultRunStyle)
        {
            var defaultTypeface = defaultRunStyle.TextFormat.Typeface;
            var textRuns = new List<TextRunProperties>();

            for (; ; )
            {
                var currentTypeface = defaultTypeface;

                if (!TryGetRunProperties(text, currentTypeface, defaultTypeface, out var count))
                {
                    var codepoint = CodepointReader.Peek(text, count, out _);

                    //ToDo: Fix FontFamily fallback
                    currentTypeface =
                        FontManager.Current.MatchCharacter(codepoint, defaultTypeface.Weight, defaultTypeface.Style);

                    if (currentTypeface == null || !TryGetRunProperties(text, currentTypeface, defaultTypeface, out count))
                    {
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
                    }
                }

                textRuns.Add(new TextRunProperties(text.Take(count).GetTextPointer(),
                    new TextRunStyle(currentTypeface, defaultRunStyle.TextFormat.FontRenderingEmSize,
                        defaultRunStyle.Foreground)));

                if (count == text.Length)
                {
                    break;
                }

                text = text.Skip(count);
            }

            return textRuns;
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
    }
}
