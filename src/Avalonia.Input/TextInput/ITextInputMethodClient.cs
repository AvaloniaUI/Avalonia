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
    }
}
