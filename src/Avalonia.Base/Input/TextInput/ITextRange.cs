using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// Represents a range between two text pointers.
    /// </summary>
    /// <remarks>
    /// Implementations are UI-thread-only.
    /// </remarks>
    [Unstable]
    public interface ITextRange
    {
        /// <summary>
        /// Gets the start pointer.
        /// </summary>
        ITextPointer Start { get; }

        /// <summary>
        /// Gets the end pointer.
        /// </summary>
        ITextPointer End { get; }

        /// <summary>
        /// Gets whether the range is empty.
        /// </summary>
        bool IsEmpty { get; }
    }
}
