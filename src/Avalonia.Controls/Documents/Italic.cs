using Avalonia.Media;

namespace Avalonia.Controls.Documents
{
    /// <summary>
    /// Italic element - markup helper for indicating italicized content.
    /// Equivalent to a Span with FontStyle property set to FontStyles.Italic.
    /// Can contain other inline elements.
    /// </summary>
    public sealed class Italic : Span
    {
        static Italic()
        {
            FontStyleProperty.OverrideDefaultValue<Italic>(FontStyle.Italic);
        }
    }
}
