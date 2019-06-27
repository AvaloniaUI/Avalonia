using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Headless
{
    public interface IHeadlessWindow
    {
        IRef<IWriteableBitmapImpl> GetLastRenderedFrame();
        void KeyPress(Key key, InputModifiers modifiers);
        void KeyRelease(Key key, InputModifiers modifiers);
        void MouseDown(Point point, int button, InputModifiers modifiers = InputModifiers.None);
        void MouseMove(Point point, InputModifiers modifiers = InputModifiers.None);
        void MouseUp(Point point, int button, InputModifiers modifiers = InputModifiers.None);
    }
}
