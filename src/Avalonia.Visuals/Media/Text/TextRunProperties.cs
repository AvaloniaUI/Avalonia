// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Utility;

namespace Avalonia.Media.Text
{
    public readonly struct TextRunProperties
    {
        public TextRunProperties(ReadOnlySlice<char> text, Typeface typeface, double fontSize, IBrush foreground) 
            : this(text, new TextFormat(typeface, fontSize), foreground)
        {
        }

        public TextRunProperties(ReadOnlySlice<char> text, TextFormat textFormat, IBrush foreground)
        {
            Text = text;
            TextFormat = textFormat;
            Foreground = foreground;
        }

        /// <summary>
        ///     The text pointer.
        /// </summary>
        public ReadOnlySlice<char> Text { get; }

        /// <summary>
        ///     The text format.
        /// </summary>
        public TextFormat TextFormat { get; }

        /// <summary>
        ///     The foreground.
        /// </summary>
        public IBrush Foreground { get; }


        private TextRunProperties WithText(ReadOnlySlice<char> text)
        {
            return new TextRunProperties(text, TextFormat, Foreground);
        }

        internal SplitResult Split(int length)
        {
            var first = Text.Take(length);
            var second = Text.Skip(length);
            return new SplitResult(WithText(first), WithText(second));
        }

        internal readonly struct SplitResult
        {
            public SplitResult(TextRunProperties first, TextRunProperties second)
            {
                First = first;
                Second = second;
            }

            public TextRunProperties First { get; }
            public TextRunProperties Second { get; }
        }
    }
}
