using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// A range that remembers which end moves: a selection extended with Shift+Left keeps its anchor at
    /// the end it started from and its active end at the caret.
    /// </summary>
    /// <param name="Anchor">The fixed end.</param>
    /// <param name="Active">The moving end, where the user acts.</param>
    [Unstable]
    public readonly record struct DirectedTextRange(ITextPointer Anchor, ITextPointer Active)
    {
        /// <summary>The end nearer the document start.</summary>
        public ITextPointer Start => IsReversed ? Active : Anchor;

        /// <summary>The end nearer the document end.</summary>
        public ITextPointer End => IsReversed ? Anchor : Active;

        /// <summary>Whether the active end lies before the anchor.</summary>
        public bool IsReversed => Active.CompareTo(Anchor) < 0;

        /// <summary>Whether the range is collapsed to a single position.</summary>
        public bool IsEmpty => Anchor.Equals(Active);
    }
}
