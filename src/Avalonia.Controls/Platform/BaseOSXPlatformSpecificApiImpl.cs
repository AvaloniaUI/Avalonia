using Avalonia.Controls;

namespace Avalonia.Controls.Platform
{
    public abstract class BaseOSXPlatformSpecificApiImpl : IPlatformSpecificApiImpl
    {
        public abstract void ShowInDock(bool show);
    }
}