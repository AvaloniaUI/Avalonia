using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;
using ReactiveUI;

namespace ControlCatalog.Pages
{
    public class MenuPage : UserControl
    {
        public MenuPage()
        {
            this.InitializeComponent();
            DataContext = new MenuPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
