// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using HarfBuzzSharp;

namespace Avalonia.Skia
{
    internal static class UnicodeUtility
    {
        /// <summary>
        ///     Determines whether [c] is a break char.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>
        ///     <c>true</c> if [is break character] [the specified c]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBreakChar(int c)
        {
            switch (c)
            {
                case '\u000A':
                case '\u000B':
                case '\u000C':
                case '\u000D':
                case '\u0085':
                case '\u2028':
                case '\u2029':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Determines whether [c] is a white space char.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>
        ///     <c>true</c> if [is whitespace character] [the specified c]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsWhiteSpace(int c)
        {
            switch (UnicodeFunctions.Default.GetGeneralCategory((uint)c))
            {
                case UnicodeGeneralCategory.Control:
                case UnicodeGeneralCategory.NonSpacingMark:
                case UnicodeGeneralCategory.Format:
                case UnicodeGeneralCategory.SpaceSeparator:
                case UnicodeGeneralCategory.SpacingMark:
                    return true;
            }

            return false;
        }
    }
}
