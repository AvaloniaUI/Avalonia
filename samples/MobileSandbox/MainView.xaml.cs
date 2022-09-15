using System;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MobileSandbox
{
    public class MainView : UserControl
    {
        public MainView()
        {
            AvaloniaXamlLoader.Load(this);

            DataContext = this;
        }

        public void ButtonCommand()
        {
            Console.WriteLine("Button pressed");
        }
    }
}
