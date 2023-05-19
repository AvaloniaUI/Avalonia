using Avalonia.Input.Raw;
using Avalonia.Reactive;

namespace Avalonia.Input.Diagnostics
{
    public static class DebuggingExtensions
    {
        public static AppBuilder EmulateTouchWithMouse(this AppBuilder builder)
        {
            MouseDevice.EmulateTouch = true;
            return builder;
        }
    }
}
