using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ControlCatalog.Pages
{
    public class TabStripPage : UserControl
    {
        public TabStripPage()
        {
            InitializeComponent();

            DataContext = new[]
            {
                new TabStripItemViewModel
                {
                    Header = "Item 1",
                },
                new TabStripItemViewModel
                {
                    Header = "Item 2",
                },
                new TabStripItemViewModel
                {
                    Header = "Disabled",
                    IsEnabled = false,
                },
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private class TabStripItemViewModel
        {
            public string Header { get; set; }
            public bool IsEnabled { get; set; } = true;
        }
    }
}
