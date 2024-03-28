using System;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MobileSandbox;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    public void ButtonCommand()
    {
        Console.WriteLine("Button pressed");
    }
}
