using System;
using Avalonia.VisualTree;

namespace Avalonia.Input.TextInput
{
    public interface ITextInputMethodClient
    {
        Rect CursorRectangle { get; }
        event EventHandler CursorRectangleChanged;
        IVisual TextViewVisual { get; }
        event EventHandler TextViewVisualChanged;
        bool SupportsPreedit { get; }
        void SetPreeditText(string text);
        bool SupportsSurroundingText { get; }
        TextInputMethodSurroundingText SurroundingText { get; }
        event EventHandler SurroundingTextChanged;
    }

    public struct TextInputMethodSurroundingText
    {
        public string Text { get; set; }
        public int CursorOffset { get; set; }
        public int AnchorOffset { get; set; }
    }
}
