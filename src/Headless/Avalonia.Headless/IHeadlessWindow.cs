using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Headless
{
    internal interface IHeadlessWindow
    {
        WriteableBitmap? GetLastRenderedFrame();
        void KeyPress(Key key, RawInputModifiers modifiers);
        void KeyRelease(Key key, RawInputModifiers modifiers);
        void TextInput(string text);
        void MouseDown(Point point, MouseButton button, RawInputModifiers modifiers = RawInputModifiers.None);
        void MouseMove(Point point, RawInputModifiers modifiers = RawInputModifiers.None);
        void MouseUp(Point point, MouseButton button, RawInputModifiers modifiers = RawInputModifiers.None);
        void MouseWheel(Point point, Vector delta, RawInputModifiers modifiers = RawInputModifiers.None);
        void DragDrop(Point point, RawDragEventType type, IDataObject data, DragDropEffects effects, RawInputModifiers modifiers = RawInputModifiers.None);
    }
}
