using System;
using Avalonia.Platform;

namespace Avalonia.Windowing.Bindings
{
    
    public class Monitors : IScreenImpl
    {
        public int ScreenCount => 1;
        public Screen[] AllScreens => new Screen[] { new Screen(Rect.Empty, Rect.Empty, true) };

        public Monitors() 
        {
            
        }
    }
}
