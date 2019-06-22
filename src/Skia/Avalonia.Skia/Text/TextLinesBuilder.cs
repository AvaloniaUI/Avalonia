// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Avalonia.Skia.Text
{
    internal static class TextLinesBuilder
    {
        /// <summary>
        ///     Builds a list of text lines.
        /// </summary>
        /// <param name="text">The text to build text lines from.</param>
        /// <returns>A list of build text lines.</returns>
        public static List<SKTextPointer> Build(ReadOnlySpan<char> text)
        {
            var currentPosition = 0;

            var lines = new List<SKTextPointer>();

            while (currentPosition < text.Length)
            {
                var lineBreakPosition = GetLineBreakPosition(text, currentPosition);

                if (lineBreakPosition != -1)
                {
                    var length = lineBreakPosition - currentPosition + 1;

                    lines.Add(new SKTextPointer(currentPosition, length));

                    currentPosition += length;
                }
                else
                {
                    lines.Add(new SKTextPointer(currentPosition, text.Length - currentPosition));
                    break;
                }
            }

            return lines;
        }

        /// <summary>
        ///     Gets the line break position that is indicated by a unicode break char.
        /// </summary>
        /// <returns></returns>
        private static int GetLineBreakPosition(ReadOnlySpan<char> text, int startingIndex)
        {
            for (var index = startingIndex; index < text.Length; index++)
            {
                var c = text[index];

                if (!UnicodeUtility.IsBreakChar(c))
                {
                    continue;
                }

                if (index < text.Length - 1)
                {
                    switch (c)
                    {
                        case '\r' when text[index + 1] == '\n':
                        case '\n' when text[index + 1] == '\r':
                            return ++index;
                    }
                }

                return index;
            }

            return -1;
        }
    }
}
