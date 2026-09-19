using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// A formatting attribute an <see cref="ITextNavigation"/> can report over a run of text.
    /// </summary>
    /// <remarks>
    /// The vocabulary is the subset shared by the platform accessibility protocols (UIA TextPattern,
    /// AT-SPI). A plain-text control reports a single uniform run spanning the whole document; richer
    /// editors report the run over which each attribute is constant. Values are boxed per the type
    /// noted on each member.
    /// </remarks>
    [Unstable]
    public enum TextAttribute
    {
        /// <summary>Font family name. Value: <see cref="string"/>.</summary>
        FontFamily,

        /// <summary>Font size in device-independent pixels. Value: <see cref="double"/>.</summary>
        FontSize,

        /// <summary>Font weight (100-900). Value: <see cref="Avalonia.Media.FontWeight"/>.</summary>
        FontWeight,

        /// <summary>Font style (normal/italic/oblique). Value: <see cref="Avalonia.Media.FontStyle"/>.</summary>
        FontStyle,

        /// <summary>Foreground colour. Value: <see cref="Avalonia.Media.Color"/>.</summary>
        Foreground,

        /// <summary>Background colour. Value: <see cref="Avalonia.Media.Color"/>.</summary>
        Background,

        /// <summary>Whether the text is read-only. Value: <see cref="bool"/>.</summary>
        IsReadOnly,

        /// <summary>The paragraph/character style, e.g. a heading level. Value: <see cref="TextStyleId"/>.</summary>
        StyleId
    }
}
