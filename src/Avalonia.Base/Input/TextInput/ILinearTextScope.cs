using System;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// A run of document content seen as a flat sequence of <see cref="char"/> code units, for platform
    /// APIs that address text by integer offsets (AT-SPI Text, TSF ACP, IMM reconversion,
    /// <c>NSRange</c>, Android <c>InputConnection</c>).
    /// </summary>
    /// <remarks>
    /// Offsets are relative to the start of <see cref="Range"/> and only mean something inside this
    /// scope. Content that is not text, such as an embedded object, occupies one U+FFFC (object
    /// replacement character). A position outside <see cref="Range"/> belongs to another scope, which
    /// the navigation returns for it. Implementations are UI-thread-only.
    /// </remarks>
    [Unstable]
    public interface ILinearTextScope
    {
        /// <summary>The document range the scope covers.</summary>
        ITextRange Range { get; }

        /// <summary>The scope's length in <see cref="char"/> code units.</summary>
        int Length { get; }

        /// <summary>
        /// The offset of <paramref name="position"/> from the start of the scope.
        /// </summary>
        /// <exception cref="ArgumentException">The pointer was not produced by the owning navigation.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The position lies outside <see cref="Range"/>.</exception>
        int GetOffset(ITextPointer position);

        /// <summary>
        /// The position at <paramref name="offset"/>, clamped to the scope and snapped in the direction of
        /// <paramref name="gravity"/> to a valid position, so an offset inside a surrogate pair moves
        /// forward for forward gravity and backward for backward gravity. The result carries
        /// <paramref name="gravity"/>.
        /// </summary>
        ITextPointer GetPosition(int offset, LogicalDirection gravity);

        /// <summary>
        /// The range over [<paramref name="start"/>, <paramref name="start"/> + <paramref name="length"/>),
        /// clamped to the scope, with inward-facing endpoint gravity (forward at the start, backward at
        /// the end).
        /// </summary>
        ITextRange GetRange(int start, int length);

        /// <summary>
        /// The <paramref name="length"/> code units at <paramref name="start"/>, clamped to the scope.
        /// The result length always equals the clamped span.
        /// </summary>
        string GetText(int start, int length);

        /// <summary>Raised after a text change inside the scope is applied, in scope offsets.</summary>
        event EventHandler<LinearTextChange>? TextChanged;

        /// <summary>
        /// Whether the scope still describes the document. It stops doing so when an edit changes the
        /// structure it covers (a block merged with its neighbour, split in two, or removed), and from
        /// then on its offsets mean nothing.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Raised once when the scope stops being valid, after the edit that dissolved it is applied and
        /// before <see cref="ITextNavigation.TextChanged"/> reports that edit. A consumer holding the
        /// scope asks <see cref="ITextNavigation.GetLinearScope"/> for the scope at its position again
        /// and resynchronizes from it. <see cref="TextChanged"/> is not raised for that edit.
        /// </summary>
        event EventHandler? Invalidated;
    }
}
