using Avalonia.Platform;

namespace Avalonia.Controls
{
    public class Screens
    {
        private IWindowImpl _windowImpl;

        public Screen[] All => _windowImpl.Screen.AllScreens;
        public Screen Primary => _windowImpl.Screen.PrimaryScreen;

        public Screens(IWindowImpl windowImpl)
        {
            _windowImpl = windowImpl;
        }
    }
}