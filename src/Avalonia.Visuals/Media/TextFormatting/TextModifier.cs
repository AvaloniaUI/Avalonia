namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Specialized text run used to modify properties of text runs in its scope.
    /// The scope extends to the next matching EndOfSegment text run (matching
    /// because text modifiers may be nested), or to the next EndOfParagraph.
    /// </summary>
    public abstract class TextModifier : TextRun
    {
        /// <summary>
        /// Modifies the properties of a text run.
        /// </summary>
        /// <param name="properties">Properties of a text run or the return value of
        /// ModifyProperties for a nested text modifier.</param>
        /// <returns>Returns the actual text run properties to be used for formatting,
        /// subject to further modification by text modifiers at outer scopes.</returns>
        public abstract TextRunProperties ModifyProperties(TextRunProperties properties);
    }
}
