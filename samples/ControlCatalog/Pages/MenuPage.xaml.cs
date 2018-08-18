using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class MenuItemViewModel
    {
        public string Header { get; set; }
        
        public IEnumerable<MenuItemViewModel> Children { get; set; }
    }

    public class MenuPage : UserControl
    {
        private static IEnumerable<MenuItemViewModel> GetMenuItems()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return new MenuItemViewModel { Header = $"Submenu{i}" };
            }
        }

        private static IEnumerable<MenuItemViewModel> GetMainMenuItems ()
        {
            for(int  i = 0; i < 10; i++)
            {
                yield return new MenuItemViewModel { Header = $"Test{i}", Children = GetMenuItems() }; 
            }
        }

        public MenuPage()
        {
            this.InitializeComponent();

            Items = GetMainMenuItems();

            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public IEnumerable<MenuItemViewModel> Items { get; }
    }
}
