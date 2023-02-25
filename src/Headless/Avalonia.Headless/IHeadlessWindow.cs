using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Headless
{
    public interface IHeadlessWindow
    {
        IRef<IWriteableBitmapImpl> GetLastRenderedFrame();
        void KeyPress(Key key, RawInputModifiers modifiers);
        void KeyRelease(Key key, RawInputModifiers modifiers);
        void MouseDown(Point point, int button, RawInputModifiers modifiers = RawInputModifiers.None);
        void MouseMove(Point point, RawInputModifiers modifiers = RawInputModifiers.None);
        void MouseUp(Point point, int button, RawInputModifiers modifiers = RawInputModifiers.None);
        void MouseWheel(Point point, Vector delta, RawInputModifiers modifiers = RawInputModifiers.None);
        void DragDrop(Point point, RawDragEventType type, IDataObject data, DragDropEffects effects, RawInputModifiers modifiers);
    }
}
