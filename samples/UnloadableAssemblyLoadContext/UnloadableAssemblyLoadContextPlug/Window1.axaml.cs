using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaPlug;

namespace UnloadableAssemblyLoadContextPlug;

public partial class Window1 : Window
{
    public Window1()
    {
        InitializeComponent();
       DataContext=new Window1ViewModel();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
      
        
    }
}
