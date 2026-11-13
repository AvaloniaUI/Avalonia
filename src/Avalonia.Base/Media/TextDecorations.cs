using System;

namespace Avalonia.Media
{
    /// <summary>
    /// Defines a set of commonly used text decorations.
    /// </summary>
    public static class TextDecorations
    {
        // TextDecoration is a thread-affine AvaloniaObject, so each thread gets its own instances.
        [ThreadStatic] private static TextDecorationCollection? t_underline;
        [ThreadStatic] private static TextDecorationCollection? t_strikethrough;
        [ThreadStatic] private static TextDecorationCollection? t_overline;
        [ThreadStatic] private static TextDecorationCollection? t_baseline;

        /// <summary>
        /// Gets a <see cref="TextDecorationCollection"/> containing an underline.
        /// </summary>
        public static TextDecorationCollection Underline
            => t_underline ??= Create(TextDecorationLocation.Underline);

        /// <summary>
        /// Gets a <see cref="TextDecorationCollection"/> containing a strikethrough.
        /// </summary>
        public static TextDecorationCollection Strikethrough
            => t_strikethrough ??= Create(TextDecorationLocation.Strikethrough);

        /// <summary>
        /// Gets a <see cref="TextDecorationCollection"/> containing an overline.
        /// </summary>
        public static TextDecorationCollection Overline
            => t_overline ??= Create(TextDecorationLocation.Overline);

        /// <summary>
        /// Gets a <see cref="TextDecorationCollection"/> containing a baseline.
        /// </summary>
        public static TextDecorationCollection Baseline
            => t_baseline ??= Create(TextDecorationLocation.Baseline);

        private static TextDecorationCollection Create(TextDecorationLocation location)
            => new TextDecorationCollection { new TextDecoration { Location = location } };
    }
}
