// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using HarfBuzzSharp;
using SkiaSharp;

namespace Avalonia.Skia.Text
{
    public class SKTextFormat
    {
        public SKTextFormat(SKTypeface typeface, Script script, float fontSize)
        {
            Typeface = typeface;
            Script = script;
            FontSize = fontSize;
        }

        /// <summary>
        ///     Gets the typeface.
        /// </summary>
        /// <value>
        /// The typeface.
        /// </value>
        public SKTypeface Typeface { get; }

        /// <summary>
        ///     Gets the script.
        /// </summary>
        /// <value>
        /// The script of the format..
        /// </value>
        public Script Script { get; }

        /// <summary>
        ///     Gets the font size.
        /// </summary>
        /// <value>
        /// The size of the font.
        /// </value>
        public float FontSize { get; }

        public override string ToString()
        {
            return $"{Typeface.FamilyName} : {Script} : {FontSize}";
        }
    }
}
