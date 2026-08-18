using Avalonia.Media.TextFormatting;

namespace Avalonia.Media
{
    /// <summary>
    /// Provides a text trimming strategy that collapses overflowing text by replacing path segments with an ellipsis
    /// string.
    /// </summary>
    /// <remarks>Use this class to trim text representing file or URI paths, replacing intermediate segments
    /// with a specified ellipsis when the text exceeds the available width. This approach helps preserve the most
    /// relevant parts of the path, such as the filename or endpoint, while indicating omitted segments. The ellipsis
    /// string can be customized to match application requirements.</remarks>
    public sealed class TextPathSegmentTrimming : TextTrimming
    {
        private readonly string _ellipsis;

        /// <summary>
        /// Initializes a new instance of the TextPathSegmentTrimming class with the specified ellipsis string to
        /// indicate trimmed text.
        /// </summary>
        /// <param name="ellipsis">The string to use as an ellipsis when text is trimmed. This value is displayed at the end of truncated
        /// segments.</param>
        public TextPathSegmentTrimming(string ellipsis)
        {
            _ellipsis = ellipsis;
        }

        public override TextCollapsingProperties CreateCollapsingProperties(TextCollapsingCreateInfo createInfo)
        {
            return new TextPathSegmentEllipsis(_ellipsis, createInfo.Width, createInfo.TextRunProperties, createInfo.FlowDirection);
        }

        public override string ToString()
        {
            return nameof(PathSegmentEllipsis);
        }
    }
}
