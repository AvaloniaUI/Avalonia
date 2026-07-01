using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// A single contiguous text replacement, raised by <see cref="ITextNavigation.TextChanged"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Position"/> is the start of the change as a navigator-produced pointer, valid in the
    /// current (post-change) document. <see cref="NewLength"/> UTF-16 code units were inserted there; the
    /// inserted text is the range [<see cref="Position"/>, Position + <see cref="NewLength"/>) and is read
    /// on demand via <see cref="ITextNavigation.GetText"/>, so the event never eagerly materializes it.
    /// <see cref="OldLength"/> code units were removed and are gone from the document. Maps to TSF
    /// <c>OnTextChange(Position.Offset, Position.Offset + OldLength, Position.Offset + NewLength)</c>.
    /// </remarks>
    [Unstable]
    public readonly record struct TextChange(ITextPointer Position, int OldLength, int NewLength);
}
