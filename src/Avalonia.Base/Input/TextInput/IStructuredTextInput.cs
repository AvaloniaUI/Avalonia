using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// Provides structured text access and editing operations for IME integrations, layered on the
    /// shared <see cref="ITextNavigation"/> positioning core. A client that implements it composes in
    /// its own document: the composition is text in the document, tracked by
    /// <see cref="CompositionRange"/>, not an overlay drawn beside it.
    /// </summary>
    /// <remarks>
    /// All members are UI-thread-only. Positions, ranges, units and linear scopes come from the base
    /// navigation contract; a platform API that speaks offsets converts through
    /// <see cref="ITextNavigation.GetLinearScope"/>. Text mutation is observed through
    /// <see cref="ITextNavigation.TextChanged"/>; <see cref="CaretPositionChanged"/> and
    /// <see cref="CompositionChanged"/> report caret, selection and composition movement, which are not
    /// text changes. The client sees the real text, not the text as drawn.
    /// </remarks>
    [Unstable]
    public interface IStructuredTextInput : ITextNavigation
    {
        /// <summary>The insertion point: where the next typed character goes.</summary>
        ITextPointer CaretPosition { get; }

        /// <summary>
        /// The selection with its direction: the active end is where the caret is. Setting it keeps the
        /// anchor fixed and puts the caret on the active end.
        /// </summary>
        DirectedTextRange Selection { get; set; }

        /// <summary>
        /// The text the input method is composing, or null when no composition is active. The range
        /// follows edits. Setting it over committed text (reconversion) makes that text the composition,
        /// so the next <see cref="SetCompositionText"/> replaces it; setting null ends the composition and
        /// keeps its text.
        /// </summary>
        ITextRange? CompositionRange { get; set; }

        /// <summary>
        /// Whether the document refuses edits. Mutations of a read-only document change nothing.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Replaces <paramref name="range"/> with <paramref name="text"/> as committed input and ends any
        /// composition. The text is subject to the same rules as typed text, such as a length limit.
        /// </summary>
        void ReplaceText(ITextRange range, string text);

        /// <summary>
        /// Sets the composition to <paramref name="text"/>, replacing the current composition, or the
        /// selection when none is active. <paramref name="cursorOffset"/> places the caret inside the new
        /// composition, counted in <see cref="char"/> code units from its start. Null cancels the
        /// composition and restores the text it replaced.
        /// </summary>
        /// <remarks>
        /// The updates of one composition, from its first text to its commit, undo as a single step.
        /// </remarks>
        void SetCompositionText(string? text, int cursorOffset);

        /// <summary>
        /// Ends the composition. A non-null <paramref name="text"/> replaces the composition first, so a
        /// result that differs from the last composition text arrives as one change rather than as a
        /// cancellation followed by an insertion; the caret then follows the committed text.
        /// </summary>
        void CommitComposition(string? text);

        // Geometry is exchanged in top-level coordinates (device-independent pixels
        // relative to the client's TopLevel), matching IAccessibleText: the client
        // transforms from its text visual to the root, and platform adapters only convert
        // between the top level and the screen, so they never need visual-tree access.

        Rect GetFirstRectForRange(ITextRange range);
        Rect GetCaretRect(ITextPointer position);
        Rect[] GetSelectionRects(ITextRange range);
        ITextPointer? GetClosestPosition(Point point);
        ITextPointer? GetClosestPosition(Point point, ITextRange withinRange);
        ITextRange? GetCharacterRangeAtPoint(Point point);

        /// <summary>Raised once the caret or selection has moved and settled, once per action.</summary>
        event EventHandler? CaretPositionChanged;

        /// <summary>Raised after <see cref="CompositionRange"/> starts, ends or moves to other text.</summary>
        event EventHandler? CompositionChanged;

        /// <summary>Raised after <see cref="IsReadOnly"/> changes.</summary>
        event EventHandler? IsReadOnlyChanged;

        /// <summary>
        /// The transient decorations currently painted over the content (composition clause styling and
        /// reconversion highlights). Empty when none. Never part of the document model, undo, or serialization.
        /// </summary>
        IReadOnlyList<TextInputDecoration> InputDecorations { get; }

        /// <summary>
        /// Replaces the current transient decorations. Cleared when the composition ends. Ranges must be
        /// produced by this navigator.
        /// </summary>
        void SetInputDecorations(IReadOnlyList<TextInputDecoration> decorations);

        /// <summary>Raised after <see cref="InputDecorations"/> changes.</summary>
        event EventHandler? InputDecorationsChanged;
    }
}
