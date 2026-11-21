using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// A single contiguous text replacement, raised by <see cref="ITextNavigation.TextChanged"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Inserted"/> is the replacement content in the current (post-change) document; it is
    /// empty for a pure deletion and is read on demand via <see cref="ITextNavigation.GetText"/>, so the
    /// event never eagerly materializes it. The removed content is gone from the document and has no
    /// position left to describe it; a consumer that needs its extent reads
    /// <see cref="ILinearTextScope.TextChanged"/>.
    /// </remarks>
    [Unstable]
    public readonly record struct TextChange(ITextRange Inserted);

    /// <summary>
    /// A single contiguous text replacement in the offsets of one <see cref="ILinearTextScope"/>, raised by
    /// <see cref="ILinearTextScope.TextChanged"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="OldLength"/> code units at <see cref="Start"/> were replaced by <see cref="NewLength"/>.
    /// Maps to TSF <c>OnTextChange(Start, Start + OldLength, Start + NewLength)</c>.
    /// </remarks>
    [Unstable]
    public readonly record struct LinearTextChange(int Start, int OldLength, int NewLength);
}
