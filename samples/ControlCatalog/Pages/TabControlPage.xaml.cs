using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
                    ImageUrl = "resm:ControlCatalog.Assets.delicate-arch-896885_640.jpg",
                },
                new TabItemViewModel
                {
                    Header = "Leaf",
                    Text = "This is the second templated tab page.",
                    ImageUrl = "resm:ControlCatalog.Assets.maple-leaf-888807_640.jpg",
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

        private class TabItemViewModel
        {
            public string Header { get; set; }
            public string Text { get; set; }
            public string ImageUrl { get; set; }
            public bool IsEnabled { get; set; }
        }
    }
}
