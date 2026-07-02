using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// Provides structured text access and editing operations for IME integrations, layered on the
    /// shared <see cref="ITextNavigation"/> positioning core.
    /// </summary>
    /// <remarks>
    /// Offsets are UTF-16 code units and all members are UI-thread-only. Document anchors, ranges,
    /// <see cref="ITextNavigation.GetText"/> and <see cref="ITextNavigation.GetRangeEnclosing"/> come
    /// from the base navigation contract, and a platform offset <c>n</c> is resolved via
    /// <c>GetPosition(DocumentStart, n)</c> (see the internal <c>TextNavigationExtensions</c> helpers) -
    /// there is deliberately no member that fabricates a pointer from a bare integer. Text mutation is
    /// observed through the base <see cref="ITextNavigation.TextChanged"/> delta;
    /// <see cref="CaretPositionChanged"/> and <see cref="CompositionChanged"/> report caret/selection and
    /// composition movement, which are not text changes.
    /// </remarks>
    [Unstable]
    public interface IStructuredTextInput : ITextNavigation
    {
        ITextPointer CaretPosition { get; }
        ITextRange Selection { get; set; }
        ITextRange? CompositionRange { get; set; }

        void ReplaceText(ITextRange range, string text);
        void SetCompositionText(string? text, int cursorOffset);
        void CommitComposition();

        Rect GetFirstRectForRange(ITextRange range);
        Rect GetCaretRect(ITextPointer position);
        Rect[] GetSelectionRects(ITextRange range);
        ITextPointer? GetClosestPosition(Point point);
        ITextPointer? GetClosestPosition(Point point, ITextRange withinRange);
        ITextRange? GetCharacterRangeAtPoint(Point point);

        event EventHandler? CaretPositionChanged;
        event EventHandler? CompositionChanged;

        /// <summary>
        /// The transient decorations currently painted over the content (composition clause styling and
        /// reconversion highlights). Empty when none. Never part of the document model, undo, or serialization.
        /// </summary>
        IReadOnlyList<TextInputDecoration> InputDecorations { get; }

        /// <summary>
        /// Replaces the current transient decorations. Cleared automatically on
        /// <see cref="CommitComposition"/>. Ranges must be produced by this navigator.
        /// </summary>
        void SetInputDecorations(IReadOnlyList<TextInputDecoration> decorations);

        /// <summary>Raised after <see cref="InputDecorations"/> changes.</summary>
        event EventHandler? InputDecorationsChanged;
    }
}
