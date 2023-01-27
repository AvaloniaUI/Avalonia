using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;

#nullable enable

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Describes the type of text selection supported by an element.
    /// </summary>
    public enum SupportedTextSelection
    {
        /// <summary>
        /// The element does not support text selection.
        /// </summary>
        None,

        /// <summary>
        /// The element supports a single, continuous text selection.
        /// </summary>
        Single,

        /// <summary>
        /// The element supports multiple, disjoint text selections.
        /// </summary>
        Multiple,
    }

    /// <summary>
    /// Exposes methods and properties to support access by a UI Automation client to controls
    /// that holds editable text.
    /// </summary>
    public interface ITextProvider
    {
        /// <summary>
        /// Gets the current position of the caret, or -1 if there is no caret.
        /// </summary>
        int CaretIndex { get; }

        /// <summary>
        /// Gets a text range that encloses the main text of the document.
        /// </summary>
        TextRange DocumentRange { get; }

        /// <summary>
        /// Gets a value that indicates whether the text in the control is read-only.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets the total number of lines in the main text of the document.
        /// </summary>
        int LineCount { get; }

        /// <summary>
        /// Gets the placeholder text.
        /// </summary>
        string? PlaceholderText { get; }

        /// <summary>
        /// Gets a value that specifies the type of text selection that is supported by the control.
        /// </summary>
        SupportedTextSelection SupportedTextSelection { get; }

        /// <summary>
        /// Occurs when the control's selection changes.
        /// </summary>
        event EventHandler? SelectedRangesChanged;

        /// <summary>
        /// Occurs when the control's text changes.
        /// </summary>
        event EventHandler? TextChanged;

        /// <summary>
        /// Retrieves a collection of bounding rectangles for each fully or partially visible line
        /// of text in a text range.
        /// </summary>
        /// <param name="range">The text range.</param>
        /// <returns>A collection of <see cref="Rect"/>s in top-level coordinates.</returns>
        IReadOnlyList<Rect> GetBounds(TextRange range);

        /// <summary>
        /// Retrieves the line number for the line that contains the specified character index.
        /// </summary>
        /// <param name="index">The character index.</param>
        /// <returns>The line number, or -1 if the character index is invalid.</returns>
        int GetLineForIndex(int index);

        /// <summary>
        /// Retrieves a text range that encloses the specified line.
        /// </summary>
        /// <param name="lineIndex">The index of the line.</param>
        TextRange GetLineRange(int lineIndex);

        /// <summary>
        /// Retrieves a collection of text ranges that represents the currently selected text in a
        /// text-based control.
        /// </summary>
        /// <remarks>
        /// If the control contains a text insertion point but no text is selected, the result
        /// should contain a degenerate (empty) text range at the position of the text insertion
        /// point.
        /// </remarks>
        IReadOnlyList<TextRange> GetSelection();

        /// <summary>
        /// Retrieves the text for a specified range.
        /// </summary>
        /// <param name="range">The text range.</param>
        /// <returns>The text.</returns>
        string GetText(TextRange range);

        /// <summary>
        /// Returns the degenerate (empty) text range nearest to the specified coordinates.
        /// </summary>
        /// <param name="p">The point in top-level coordinates.</param>
        TextRange RangeFromPoint(Point p);

        /// <summary>
        /// Scrolls the specified range of text into view.
        /// </summary>
        /// <param name="range">The text range.</param>
        void ScrollIntoView(TextRange range);

        /// <summary>
        /// Selects the specified range of text, replacing any previous selection.
        /// </summary>
        /// <param name="range"></param>
        void Select(TextRange range);
    }
}
