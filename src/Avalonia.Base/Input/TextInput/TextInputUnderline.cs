using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// An underline style an input method may request explicitly via
    /// <see cref="TextInputDecoration.Underline"/>.
    /// </summary>
    [Unstable]
    public enum TextInputUnderline
    {
        /// <summary>No explicit underline; the theme decides from the decoration kind.</summary>
        None,

        /// <summary>A solid single underline.</summary>
        Single,

        /// <summary>A dotted underline.</summary>
        Dotted,

        /// <summary>A dashed underline.</summary>
        Dashed,

        /// <summary>A wavy underline.</summary>
        Wavy,

        /// <summary>A thick solid underline.</summary>
        Thick
    }
}
