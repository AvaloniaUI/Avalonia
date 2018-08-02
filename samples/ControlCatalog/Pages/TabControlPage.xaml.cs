using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using ControlCatalog.Pages;

namespace ControlCatalog.Pages
{
    public class TabControlPage : UserControl
    {
        public TabControlPage()
        {
            this.InitializeComponent();                      
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
