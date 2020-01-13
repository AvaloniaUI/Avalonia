// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Media.Text
{
    public readonly struct TextStyle
    {
        public TextStyle(Typeface typeface, double fontRenderingEmSize, IBrush foreground) 
            : this(new TextFormat(typeface, fontRenderingEmSize), foreground)
        {
        }

        public TextStyle(TextFormat textFormat, IBrush foreground)
        {
            TextFormat = textFormat;
            Foreground = foreground;
        }

        /// <summary>
        ///     The text format.
        /// </summary>
        public TextFormat TextFormat { get; }

        /// <summary>
        ///     The foreground.
        /// </summary>
        public IBrush Foreground { get; }
    }
}
