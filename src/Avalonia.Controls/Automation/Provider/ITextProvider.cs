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
        /// Gets a text range that encloses the main text of a document.
        /// </summary>
        ITextRangeProvider DocumentRange { get; }

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
        /// Retrieves a collection of text ranges that represents the currently selected text in a
        /// text-based control.
        /// </summary>
        IReadOnlyList<ITextRangeProvider> GetSelection();

        /// <summary>
        /// Retrieves a collection of disjoint text ranges from a text-based control where each
        /// text range represents a contiguous span of visible text.
        /// </summary>
        IReadOnlyList<ITextRangeProvider> GetVisibleRanges();

        /// <summary>
        /// Retrieves a text range enclosing a child element such as an image, hyperlink, or other
        /// embedded object.
        /// </summary>
        /// <param name="childElement">The child element.</param>
        ITextRangeProvider RangeFromChild(AutomationPeer childElement);

        /// <summary>
        /// Returns the degenerate (empty) text range nearest to the specified coordinates.
        /// </summary>
        /// <param name="p">The point in top-level coordinates.</param>
        ITextRangeProvider RangeFromPoint(Point p);
    }
}
