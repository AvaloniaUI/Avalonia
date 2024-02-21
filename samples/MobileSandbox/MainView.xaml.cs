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
            var parent = TopLevel.GetTopLevel(this) as Window;

            if(parent != null)
            {
                new MainWindow().Show(parent);

            }

        }

        public void HideCommand()
        {
            Console.WriteLine("Button pressed");
            var parent = TopLevel.GetTopLevel(this) as Window;

           parent?.Hide();

        }
    }
}
