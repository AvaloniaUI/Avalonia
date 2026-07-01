using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// A transient decoration over a range of text, supplied by the input method (composition clause
    /// styling or a reconversion highlight). It is painted over the content and never becomes part of the
    /// document model, undo history, or serialization.
    /// </summary>
    /// <remarks>
    /// <see cref="Kind"/> is the primary signal - the control's theme resolves it to a brush and style. The
    /// explicit <see cref="Foreground"/>, <see cref="Background"/> and <see cref="Underline"/> members are an
    /// escape hatch for platforms that hand raw values (for example an Android background-color span or a
    /// resolved TSF display attribute); when set they override the theme mapping for that member.
    /// <see cref="Range"/> must be produced by the navigator the decoration is applied to.
    /// </remarks>
    [Unstable]
    public readonly record struct TextInputDecoration(
        ITextRange Range,
        TextInputDecorationKind Kind,
        Color? Foreground = null,
        Color? Background = null,
        TextInputUnderline Underline = TextInputUnderline.None);
}
