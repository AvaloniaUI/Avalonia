using Avalonia.Controls;

namespace ImpellerSandbox;

internal class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Impeller Sandbox";
        Width = 900;
        Height = 1000;
        Content = new SandboxView();
    }
}
