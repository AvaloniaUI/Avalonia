using System;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// An opaque document position produced by an <see cref="ITextNavigation"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pointer carries no index: a document need not have a single linear index space, and in a
    /// structured document several positions can share one offset (two empty paragraphs, say). Pointers
    /// from the same navigation are ordered by document position. <see cref="IEquatable{T}.Equals(T)"/>
    /// and a <see cref="IComparable{T}.CompareTo(T)"/> result of zero always agree, and neither looks at
    /// gravity. A platform API that speaks integer offsets converts through an
    /// <see cref="ILinearTextScope"/>.
    /// </para>
    /// <para>
    /// A pointer is a snapshot of a position in the document version it was produced for. It does not
    /// follow later edits: after a change the navigation still accepts it, resolving it to the nearest
    /// valid position in the current document, but that position need not hold the same content. A
    /// consumer that holds a position across edits (a composition, an automation text range, a platform
    /// marked range) holds an <see cref="ITrackedTextRange"/> instead.
    /// </para>
    /// <para>
    /// Implementations are UI-thread-only.
    /// </para>
    /// </remarks>
    [Unstable]
    public interface ITextPointer : IComparable<ITextPointer>, IEquatable<ITextPointer>
    {
        /// <summary>
        /// Gets the pointer's logical gravity: the side of the position it stays with when content is
        /// inserted exactly there. A forward pointer stays with the following content and so ends up
        /// after the insertion; a backward pointer stays with the preceding content and ends up before
        /// it. Also breaks the tie at a unit boundary in <see cref="ITextNavigation.GetRangeEnclosing"/>.
        /// </summary>
        LogicalDirection Gravity { get; }
    }
}
