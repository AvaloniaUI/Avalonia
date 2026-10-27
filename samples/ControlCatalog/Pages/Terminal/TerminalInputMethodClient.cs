using Avalonia;
using Avalonia.Input.TextInput;

namespace ControlCatalog.Pages.Terminal
{
    /// <summary>
    /// Lets input methods compose text at the terminal cursor. Committed text arrives as TextInput.
    /// </summary>
    internal sealed class TerminalInputMethodClient : TextInputMethodClient
    {
        private readonly TerminalView _view;

        public TerminalInputMethodClient(TerminalView view) => _view = view;

        public override Visual TextViewVisual => _view;
        public override bool SupportsPreedit => true;
        public override bool SupportsSurroundingText => false;
        public override string SurroundingText => "";
        public override Rect CursorRectangle => _view.CursorRectangle;

        public override TextSelection Selection
        {
            get => default;
            set { }
        }

        public override void SetPreeditText(string? preeditText) => _view.SetPreeditText(preeditText);

        public void UpdateCursorRectangle() => RaiseCursorRectangleChanged();
    }
}
