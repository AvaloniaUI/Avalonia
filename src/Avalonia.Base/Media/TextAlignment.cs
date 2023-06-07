namespace Avalonia.Media
{
    /// <summary>
    /// Defines how text is aligned.
    /// </summary>
    public enum TextAlignment
    {
        /// <summary>
        /// The text is left-aligned.
        /// </summary>
        Left,

        /// <summary>
        /// The text is centered.
        /// </summary>
        Center,

        /// <summary>
        /// The text is right-aligned.
        /// </summary>
        Right,

        /// <summary>
        /// The beginning of the text is aligned to the edge of the available space.
        /// </summary>
        Start,

        /// <summary>
        /// The end of the text is aligned to the edge of the available space.
        /// </summary>
        End,

        /// <summary>
        /// Text alignment is inferred from the text content.
        /// </summary>
        /// <remarks>
        /// When the TextAlignment property is set to DetectFromContent, alignment is inferred from the text content of the control. For example, English text is left aligned, and Arabic text is right aligned.
        /// </remarks>
        DetectFromContent,

        /// <summary>
        /// Text is justified within the available space.
        /// </summary>
        Justify
    }
}
