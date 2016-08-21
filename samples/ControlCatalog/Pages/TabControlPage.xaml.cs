using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Platform;

namespace ControlCatalog.Pages
{
    public class TabControlPage : UserControl
    {
        public TabControlPage()
        {
            this.InitializeComponent();

            DataContext = new[]
            {
                new TabItemViewModel
                {
                    Header = "Arch",
                    Text = "This is the first templated tab page.",
                    Image = LoadBitmap("resm:ControlCatalog.Assets.delicate-arch-896885_640.jpg?assembly=ControlCatalog"),
                },
                new TabItemViewModel
                {
                    Header = "Leaf",
                    Text = "This is the second templated tab page.",
                    Image = LoadBitmap("resm:ControlCatalog.Assets.maple-leaf-888807_640.jpg?assembly=ControlCatalog"),
                },
                new TabItemViewModel
                {
                    Header = "Disabled",
                    Text = "You should not see this.",
                },
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private IBitmap LoadBitmap(string uri)
        {
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            return new Bitmap(assets.Open(new Uri(uri)));
        }

        private class TabItemViewModel
        {
            public string Header { get; set; }
            public string Text { get; set; }
            public IBitmap Image { get; set; }
            public bool IsEnabled { get; set; }
        }
    }
}
