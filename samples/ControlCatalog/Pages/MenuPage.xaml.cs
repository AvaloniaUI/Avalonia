using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class MenuPage : UserControl
    {
        public MenuPage()
        {
            this.InitializeComponent();
            DataContext = new[]
            {
                new MenuItemViewModel
                {
                    Header = "_File",
                    Items = new[]
                    {
                        new MenuItemViewModel { Header = "_Open..." },
                        new MenuItemViewModel { Header = "Save" },
                        new MenuItemViewModel { Header = "-" },
                        new MenuItemViewModel
                        {
                            Header = "Recent",
                            Items = new[]
                            {
                                new MenuItemViewModel { Header = "File1.txt" },
                                new MenuItemViewModel { Header = "File2.txt" },
                            }
                        },
                    }
                },
                new MenuItemViewModel
                {
                    Header = "_Edit",
                    Items = new[]
                    {
                        new MenuItemViewModel { Header = "_Copy" },
                        new MenuItemViewModel { Header = "_Paste" },
                    }
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public class MenuItemViewModel
    {
        public string Header { get; set; }
        public IList<MenuItemViewModel> Items { get; set; }
    }
}
