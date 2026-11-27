using System;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// Layout-free logical navigation over a text document: the shared core beneath IME and
    /// accessibility consumers. Any backing store
    /// (string, gap buffer, piece table, rope + element tree) can implement it.
    /// </summary>
    /// <remarks>
    /// All members are UI-thread only. Every <see cref="ITextPointer"/> passed in must have been produced
    /// by this same instance; passing a foreign pointer throws <see cref="ArgumentException"/>. Positions
    /// are opaque and are addressed only by unit boundaries. A consumer that speaks integer offsets asks
    /// <see cref="GetLinearScope"/> for the linear view around a position and converts through it.
    /// </remarks>
    [Unstable]
    public interface ITextNavigation
    {
        /// <summary>The position at the start of the document. Carries forward gravity.</summary>
        ITextPointer DocumentStart { get; }

        /// <summary>The position at the end of the document. Carries backward gravity.</summary>
        ITextPointer DocumentEnd { get; }

        /// <summary>The range covering the whole document.</summary>
        ITextRange DocumentRange { get; }

        /// <summary>A monotonically increasing token bumped once per contiguous text change.</summary>
        long DocumentVersion { get; }

        /// <summary>
        /// The position <paramref name="count"/> <paramref name="unit"/> boundaries from
        /// <paramref name="origin"/> (negative moves toward the document start), clamped to the document.
        /// </summary>
        /// <remarks>
        /// A caller needing the number of boundaries actually crossed (UIA <c>Move</c>) single-steps with
        /// <c>count = +/-1</c>, counting until a step does not advance (the document edge). The result
        /// carries the direction of travel as its gravity; a zero-count move preserves the origin's gravity.
        /// </remarks>
        ITextPointer GetPosition(ITextPointer origin, TextUnit unit, int count);

        /// <summary>
        /// The position <paramref name="position"/> denotes, carrying <paramref name="gravity"/>.
        /// </summary>
        ITextPointer WithGravity(ITextPointer position, LogicalDirection gravity);

        /// <summary>
        /// The range of the <paramref name="unit"/> containing <paramref name="position"/>. At a unit
        /// boundary the tie is broken by <c>position.Gravity</c> (Forward selects the following unit,
        /// Backward the preceding).
        /// </summary>
        ITextRange GetRangeEnclosing(ITextPointer position, TextUnit unit);

        /// <summary>A normalized range from two positions; the order of the arguments does not matter.</summary>
        ITextRange GetRange(ITextPointer a, ITextPointer b);

        /// <summary>
        /// A range over <paramref name="range"/> whose endpoints follow later edits, each by its own
        /// gravity. The caller owns the result and disposes it when the position no longer matters.
        /// </summary>
        /// <remarks>
        /// For a range that should not grow when content is typed at its edges, pass a forward start
        /// and a backward end (as <see cref="ILinearTextScope.GetRange"/> produces).
        /// </remarks>
        ITrackedTextRange TrackRange(ITextRange range);

        /// <summary>The text in <paramref name="range"/>, serialized the way the document serializes its content.</summary>
        string GetText(ITextRange range);

        /// <summary>
        /// The linear view of the content around <paramref name="position"/>, for consumers that address
        /// text by integer offsets. The document decides how far the scope reaches: a block for a
        /// structured document, the whole text for a plain one.
        /// </summary>
        ILinearTextScope GetLinearScope(ITextPointer position);

        /// <summary>Raised after a contiguous text change is applied. Never raised mid-mutation.</summary>
        event EventHandler<TextChange>? TextChanged;
    }
}
