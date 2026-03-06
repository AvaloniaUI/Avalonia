using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageAttachedMethodsPage : UserControl
    {
        private int _pageCount;

        public NavigationPageAttachedMethodsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(new ContentPage
            {
                Header = "Root Page",
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Root Page",
                            FontSize = 24,
                            FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = "Configure per-page settings on the right,\nthen press \"Push Page with Above Settings\"\nto apply them to a new page.",
                            FontSize = 13,
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            Opacity = 0.65,
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            }, null);
        }

        private async void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCount++;

            var page = new ContentPage
            {
                Background = NavigationDemoHelper.GetPageBrush(_pageCount - 1),
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"Page {_pageCount}",
                            FontSize = 26,
                            FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = BuildSummary(),
                            FontSize = 12,
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            Opacity = 0.6,
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            NavigationPage.SetHasNavigationBar(page, HasNavBarCheck.IsChecked == true);
            NavigationPage.SetHasBackButton(page, HasBackButtonCheck.IsChecked == true);

            if (BackButtonCombo.SelectedIndex > 0)
            {
                object? content = BackButtonCombo.SelectedIndex switch
                {
                    1 => (object)"← Back",
                    2 => "Cancel",
                    _ => new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 4,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            new TextBlock { Text = "✓", FontSize = 14, VerticalAlignment = VerticalAlignment.Center },
                            new TextBlock { Text = "Done", VerticalAlignment = VerticalAlignment.Center },
                        }
                    }
                };
                NavigationPage.SetBackButtonContent(page, content);
            }

            if (HeaderCombo.SelectedIndex == 3)
            {
                NavigationPage.SetHasNavigationBar(page, false);
            }
            else
            {
                object? titleView = HeaderCombo.SelectedIndex switch
                {
                    1 => (object)new StackPanel
                    {
                        Spacing = 0,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            new TextBlock { Text = $"Page {_pageCount}", FontSize = 14, FontWeight = FontWeight.SemiBold },
                            new TextBlock { Text = "Custom subtitle", FontSize = 10, Opacity = 0.5 },
                        }
                    },
                    2 => new TextBox
                    {
                        PlaceholderText = "Search…",
                        Width = 200,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 13,
                    },
                    _ => (object?)$"Page {_pageCount}"
                };
                NavigationPage.SetHeader(page, titleView);
            }

            await DemoNav.PushAsync(page);
        }

        private async void OnPop(object? sender, RoutedEventArgs e) => await DemoNav.PopAsync();

        private async void OnPopToRoot(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PopToRootAsync();
            _pageCount = 0;
        }

        private string BuildSummary()
        {
            var navBar    = HasNavBarCheck.IsChecked == true ? "shown" : "hidden";
            var backBtn   = HasBackButtonCheck.IsChecked == true ? "shown" : "hidden";
            var backLabel = BackButtonCombo.SelectedIndex switch
            {
                1 => "← Back", 2 => "Cancel", 3 => "✓ Done", _ => "default icon"
            };
            var headerLabel = HeaderCombo.SelectedIndex switch
            {
                1 => "Title+subtitle", 2 => "Search box", 3 => "hidden", _ => "string"
            };
            return $"NavBar: {navBar}\nBack button: {backBtn}\nBack content: {backLabel}\nHeader: {headerLabel}";
        }
    }
}
