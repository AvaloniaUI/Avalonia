using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Input;

namespace Avalonia
{
    public static class DevToolsExtensions
    {
        public static void AttachDevTools(this TopLevel root)
        {
            DevTools.Attach(root, new KeyGesture(Key.F12));
        }

        public static void AttachDevTools(this TopLevel root, KeyGesture gesture)
        {
            DevTools.Attach(root, gesture);
        }
    }
}
