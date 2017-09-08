using Avalonia.Input;

namespace Avalonia.Android.Platform.Input
{
    public class AndroidMouseDevice : MouseDevice
    {
        public static AndroidMouseDevice Instance { get; } = new AndroidMouseDevice();

        public AndroidMouseDevice()
        {

        }
    }
}