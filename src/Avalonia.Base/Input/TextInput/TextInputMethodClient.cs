using System;

namespace Avalonia.Input.TextInput
{
    public abstract class TextInputMethodClient
    {
        /// <summary>
        /// Fires when the text view visual has changed
        /// </summary>
        public event EventHandler? TextViewVisualChanged;

        /// <summary>
        /// Fires when the cursor rectangle has changed
        /// </summary>
        public event EventHandler? CursorRectangleChanged;

        /// <summary>
        /// Fires when the surrounding text has changed
        /// </summary>
        public event EventHandler? SurroundingTextChanged;

        /// <summary>
        /// Fires when the selection has changed
        /// </summary>
        public event EventHandler? SelectionChanged;
        
        /// <summary>
        /// Fires when client wants to reset IME state
        /// </summary>
        public event EventHandler? ResetRequested;
        
        /// <summary>
        /// Fires when client requests the input panel be opened.
        /// </summary>
        public event EventHandler? InputPaneActivationRequested;

        /// <summary>
        /// The visual that's showing the text
        /// </summary>
        public abstract Visual TextViewVisual { get; }

        /// <summary>
        /// Indicates if the client renders non-committed input through the legacy preedit
        /// overlay (<see cref="SetPreeditText(string?)"/>). Defaults to false: clients that
        /// compose in the document do so through
        /// <c>IStructuredTextInput.SetCompositionText</c> instead, and backends deliver
        /// composition text through <c>DeliverComposition</c>.
        /// </summary>
        public virtual bool SupportsPreedit => false;

        /// <summary>
        /// The text of the active composition as presented to the user, readable by
        /// legacy consumers. A client that composes in the document derives it from the
        /// composition region; null when no composition is active.
        /// </summary>
        public virtual string? PreeditText => null;

        /// <summary>
        /// Indicates if the client renders the composition inside the document through the structured
        /// composition range (<c>IStructuredTextInput.CompositionRange</c>) rather than as a preedit
        /// overlay. Platform IMEs use this to anchor a composition over existing text - e.g. a
        /// reconversion target - instead of routing the replacement through the visible selection.
        /// </summary>
        public virtual bool SupportsInDocumentComposition => false;

        /// <summary>
        /// Indicates if text input client is capable of providing the text around the cursor
        /// </summary>
        public abstract bool SupportsSurroundingText { get; }

        /// <summary>
        /// Returns the text around the cursor, usually the current paragraph
        /// </summary>
        public abstract string SurroundingText { get; }

        /// <summary>
        /// Gets the cursor rectangle relative to the TextViewVisual
        /// </summary>
        public abstract Rect CursorRectangle { get; }

        /// <summary>
        /// Gets or sets the curent selection range within current surrounding text.
        /// </summary>
        public abstract TextSelection Selection { get; set; }

        /// <summary>
        /// Legacy entry point for the non-committed input string. Backends deliver
        /// composition through <c>DeliverComposition</c>, which routes here only for clients
        /// that declare <see cref="SupportsPreedit"/>, so overlay clients are the only ones
        /// that need to override it.
        /// </summary>
        public virtual void SetPreeditText(string? preeditText) { }

        /// <summary>
        /// Execute specific context menu actions
        /// </summary>
        /// <param name="action">The <see cref="ContextMenuAction"/> to perform</param>
        public virtual void ExecuteContextMenuAction(ContextMenuAction action) { }

        /// <summary>
        /// Sets the non-committed input string and cursor offset in that string
        /// </summary>
        public virtual void SetPreeditText(string? preeditText, int? cursorPos)
        {
            SetPreeditText(preeditText);
        }
        
        protected virtual void RaiseTextViewVisualChanged()
        {
            TextViewVisualChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void RaiseCursorRectangleChanged()
        {
            CursorRectangleChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void RaiseSurroundingTextChanged()
        {
            SurroundingTextChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void RaiseSelectionChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void RaiseInputPaneActivationRequested()
        {
            InputPaneActivationRequested?.Invoke(this, EventArgs.Empty);
        }
        
        protected virtual void RequestReset()
        {
            ResetRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public record struct TextSelection(int Start, int End);

    public enum ContextMenuAction
    {
        Copy,
        Cut,
        Paste,
        SelectAll
    }
}
