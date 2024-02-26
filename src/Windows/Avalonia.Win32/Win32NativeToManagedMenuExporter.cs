using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Avalonia.Win32;

internal class Win32NativeToManagedMenuExporter : INativeMenuExporter
{
    private NativeMenu? _nativeMenu;

    public void SetNativeMenu(NativeMenu? nativeMenu)
    {
        _nativeMenu = nativeMenu;
    }

    internal NativeMenu? GetNativeMenu() => _nativeMenu;
}
