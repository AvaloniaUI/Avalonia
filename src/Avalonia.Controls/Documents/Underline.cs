namespace Avalonia.Controls.Documents
{
    /// <summary>
    /// Underline element - markup helper for indicating superscript content.
    /// Equivalent to a Span with TextDecorations property set to TextDecorations.Underlined.
    /// Can contain other inline elements.
    /// </summary>
    public sealed class Underline : Span
    {
        static Underline()
        {
            TextDecorationsProperty.OverrideDefaultValue<Underline>(Media.TextDecorations.Underline);
        }
    }
}
