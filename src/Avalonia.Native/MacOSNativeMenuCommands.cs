using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    internal class MacOSNativeMenuCommands : INativeApplicationCommands
    {
        private readonly IAvnApplicationCommands _commands;

        public MacOSNativeMenuCommands(IAvnApplicationCommands commands)
        {
            _commands = commands;
        }

        public void ShowApp()
        {
            _commands.UnhideApp();
        }

        public void HideApp()
        {
            _commands.HideApp();
        }

        public void ShowAll()
        {
            _commands.ShowAll();
        }

        public void HideOthers()
        {
            _commands.HideOthers();
        }

        public static readonly AttachedProperty<bool> IsServicesSubmenuProperty =
            AvaloniaProperty.RegisterAttached<MacOSNativeMenuCommands, NativeMenu, bool>("IsServicesSubmenu", false);
    }
}
