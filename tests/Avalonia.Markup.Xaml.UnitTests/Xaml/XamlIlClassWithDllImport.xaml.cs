using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class XamlIlClassWithDllImport : UserControl
{
    public XamlIlClassWithDllImport()
        => AvaloniaXamlLoader.Load(this);

    [DllImport("libdoesnotexist")]
    private static extern int Add(int a, int b);
}
