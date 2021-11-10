using Avalonia.Controls.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    internal class AvaloniaNativeApplicationCommands : INativeApplicationCommands
    {
        private readonly IAvnApplicationCommands _commands;

        public AvaloniaNativeApplicationCommands(IAvnApplicationCommands commands)
        {
            _commands = commands;
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
    }
}
