// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using SkiaSharp;

namespace Avalonia.Skia.Text
{
    public class SKTextFormat
    {
        public SKTextFormat(SKTypeface typeface, float fontSize)
        {
            Typeface = typeface;
            FontSize = fontSize;
        }

        /// <summary>
        /// Gets the typeface.
        /// </summary>
        /// <value>
        /// The typeface.
        /// </value>
        public SKTypeface Typeface { get; }

        /// <summary>
        /// Gets the font size.
        /// </summary>
        /// <value>
        /// The size of the font.
        /// </value>
        public float FontSize { get; }

        public override string ToString()
        {
            return $"{Typeface.FamilyName} : {FontSize}";
        }
    }
}
