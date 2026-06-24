using System;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// Layout-free logical navigation over a text document: the shared core beneath IME
    /// (<see cref="IStructuredTextInput"/>) and accessibility consumers. Any backing store
    /// (string, gap buffer, piece table, rope + element tree) can implement it.
    /// </summary>
    /// <remarks>
    /// All members are UI-thread only. Every <see cref="ITextPointer"/> passed in must have been produced
    /// by this same instance; passing a foreign pointer throws <see cref="ArgumentException"/>. There is
    /// deliberately no factory from a raw integer: to turn a platform offset <c>n</c> into a position call
    /// <c>GetPosition(DocumentStart, n)</c>, and to report a position as an integer read
    /// <see cref="ITextPointer.Offset"/>.
    /// </remarks>
    [Unstable]
    public interface ITextNavigation
    {
        /// <summary>The position at the start of the document.</summary>
        ITextPointer DocumentStart { get; }

        /// <summary>The position at the end of the document.</summary>
        ITextPointer DocumentEnd { get; }

        /// <summary>The range covering the whole document.</summary>
        ITextRange DocumentRange { get; }

        /// <summary>A monotonically increasing token bumped once per contiguous text change.</summary>
        long DocumentVersion { get; }

        /// <summary>
        /// The position <paramref name="distance"/> UTF-16 code units from <paramref name="origin"/>
        /// (negative moves toward the document start), clamped to the document and snapped, in the
        /// direction of travel, to a valid position.
        /// </summary>
        ITextPointer GetPosition(ITextPointer origin, int distance);

        /// <summary>
        /// The position <paramref name="count"/> <paramref name="unit"/> boundaries from
        /// <paramref name="origin"/> (negative moves toward the document start), clamped to the document.
        /// </summary>
        /// <remarks>
        /// A caller needing the number of boundaries actually crossed (UIA <c>Move</c>) single-steps with
        /// <c>count = +/-1</c>, counting until a step does not advance (the document edge). That count is
        /// not recoverable from offsets - a code-unit distance is not a boundary count.
        /// </remarks>
        ITextPointer GetPosition(ITextPointer origin, TextUnit unit, int count);

        /// <summary>
        /// The range of the <paramref name="unit"/> containing <paramref name="position"/>. At a unit
        /// boundary the tie is broken by <c>position.Gravity</c> (Forward selects the following unit,
        /// Backward the preceding).
        /// </summary>
        ITextRange GetRangeEnclosing(ITextPointer position, TextUnit unit);

        /// <summary>A normalized range from two positions; the order of the arguments does not matter.</summary>
        ITextRange GetRange(ITextPointer a, ITextPointer b);

        /// <summary>The signed UTF-16 code-unit distance from <paramref name="from"/> to <paramref name="to"/>.</summary>
        int GetOffset(ITextPointer from, ITextPointer to);

        /// <summary>
        /// The text in <paramref name="range"/>. The result length always equals
        /// <c>range.End.Offset - range.Start.Offset</c>; embedded objects contribute their offset footprint.
        /// </summary>
        string GetText(ITextRange range);

        /// <summary>Raised after a contiguous text change is applied. Never raised mid-mutation.</summary>
        event EventHandler<TextChange>? TextChanged;
    }
}
