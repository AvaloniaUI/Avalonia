using Avalonia.Platform;

namespace Avalonia.Controls
{
    public class Screens
    {
        private readonly IScreenImpl _iScreenImpl;
        
        public Screen[] All => _iScreenImpl.AllScreens;
        public Screen Primary => _iScreenImpl.PrimaryScreen;

        public Screens(IScreenImpl iScreenImpl)
        {
            _iScreenImpl = iScreenImpl;
        }
    }
}