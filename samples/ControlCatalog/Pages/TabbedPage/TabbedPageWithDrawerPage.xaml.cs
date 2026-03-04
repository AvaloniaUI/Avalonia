using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageWithDrawerPage : UserControl
    {
        public TabbedPageWithDrawerPage()
        {
            InitializeComponent();
            Loaded += (_, _) => ShowSection("Home");
        }

        private void OnSectionSelected(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string section)
            {
                ShowSection(section);
                DemoDrawer.IsOpen = false;
            }
        }

        private void ShowSection(string section)
        {
            SectionHost.Content = section switch
            {
                "Home" => CreateHomeTabbed(),
                _      => CreatePlainPage(section)
            };
        }

        private static Control CreatePlainPage(string section)
        {
            var (subtitle, icon) = section switch
            {
                "Explore"   => ("Discover new content.", "📍"),
                "Favorites" => ("Items you've saved.", "❤"),
                _           => (string.Empty, string.Empty)
            };

            return new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = section,
                        FontSize = 22,
                        FontWeight = FontWeight.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = subtitle,
                        FontSize = 13,
                        Opacity = 0.7,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        MaxWidth = 300
                    }
                }
            };
        }

        private static TabbedPage CreateHomeTabbed() => new()
        {
            TabPlacement = TabPlacement.Bottom,
            Pages = new[]
            {
                new ContentPage
                {
                    Header  = "Featured",
                    Content = new TextBlock
                    {
                        Text = "Featured content",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment   = VerticalAlignment.Center,
                        FontSize = 18,
                        Opacity  = 0.7
                    }
                },
                new ContentPage
                {
                    Header  = "Recent",
                    Content = new TextBlock
                    {
                        Text = "Recent activity",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment   = VerticalAlignment.Center,
                        FontSize = 18,
                        Opacity  = 0.7
                    }
                },
                new ContentPage
                {
                    Header  = "Popular",
                    Content = new TextBlock
                    {
                        Text = "Popular right now",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment   = VerticalAlignment.Center,
                        FontSize = 18,
                        Opacity  = 0.7
                    }
                }
            }
        };
    }
}
