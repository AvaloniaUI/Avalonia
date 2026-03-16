using System;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// Represents a document position in UTF-16 code units.
    /// </summary>
    /// <remarks>
    /// Implementations are UI-thread-only.
    /// </remarks>
    [Unstable]
    public interface ITextPointer : IComparable<ITextPointer>
    {
        /// <summary>
        /// Gets the zero-based UTF-16 offset from the start of the document.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Gets the pointer's logical gravity for insertion behavior.
        /// </summary>
        LogicalDirection LogicalDirection { get; }
    }
}
