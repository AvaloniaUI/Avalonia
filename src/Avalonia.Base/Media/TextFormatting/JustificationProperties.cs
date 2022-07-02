namespace Avalonia.Media.TextFormatting
{
    public abstract class JustificationProperties
    {
        /// <summary>
        /// Gets the width in which the range is justified.
        /// </summary>
        public abstract double Width { get; }

        /// <summary>
        /// Justifies given text line.
        /// </summary>
        /// <param name="textLine">Text line to collapse.</param>
        public abstract void Justify(TextLine textLine);
    }
}
