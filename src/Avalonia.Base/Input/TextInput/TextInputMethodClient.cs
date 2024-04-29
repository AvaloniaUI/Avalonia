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
        /// The visual that's showing the text
        /// </summary>
        public abstract Visual TextViewVisual { get; }

        /// <summary>
        /// Indicates if TextViewVisual is capable of displaying non-committed input on the cursor position
        /// </summary>
        public abstract bool SupportsPreedit { get; }

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
        /// Sets the non-committed input string
        /// </summary>
        public virtual void SetPreeditText(string? preeditText) { }

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
        
        protected virtual void RequestReset()
        {
            ResetRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public record struct TextSelection(int Start, int End);
}
