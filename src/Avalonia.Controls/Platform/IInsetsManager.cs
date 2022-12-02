using System;

namespace Avalonia.Controls.Platform
{
    [Avalonia.Metadata.Unstable]
    public interface IInsetsManager
    {
        SystemBarTheme? SystemBarTheme { get; set; }

        bool? IsSystemBarVisible { get; set; }

        event EventHandler<SafeAreaChangedArgs> SafeAreaChanged;

        bool DisplayEdgeToEdge { get; set; }

        Thickness GetSafeAreaPadding();

        public class SafeAreaChangedArgs : EventArgs
        {
            public SafeAreaChangedArgs(Thickness safeArePadding)
            {
                SafeAreaPadding = safeArePadding;
            }

            public Thickness SafeAreaPadding { get; }
        }
    }

    public enum SystemBarTheme
    {
        Light,
        Dark
    }
}
