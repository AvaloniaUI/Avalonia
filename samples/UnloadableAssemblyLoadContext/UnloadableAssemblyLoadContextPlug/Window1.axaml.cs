using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
