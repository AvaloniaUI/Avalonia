using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageTitlePage : UserControl
    {
        private static readonly Color[] PageColors =
        {
            Color.Parse("#BBDEFB"), Color.Parse("#C8E6C9"), Color.Parse("#FFE0B2"),
            Color.Parse("#E1BEE7"), Color.Parse("#FFCDD2"), Color.Parse("#B2EBF2"),
        };

        private int _pageCount;

        public NavigationPageTitlePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(MakePage("Home", "Choose a header type and tap 'Push'.", 0), null);
            StatusText.Text = "Current: Home";
        }

        private void OnSetStringHeader(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.CurrentPage == null)
                return;
            DemoNav.CurrentPage.Header = DemoNav.CurrentPage.Header as string ?? "Home";
            StatusText.Text = "String Header";
        }

        private void OnSetSearchHeader(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.CurrentPage == null)
                return;
            DemoNav.CurrentPage.Header = new TextBox
            {
                PlaceholderText = "Search...",
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };
            StatusText.Text = "Custom: Search Box";
        }

        private void OnSetSliderHeader(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.CurrentPage == null)
                return;
            DemoNav.CurrentPage.Header = new Slider
            {
                Width = 200,
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };
            StatusText.Text = "Custom: Slider";
        }

        private void OnSetLayoutHeader(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.CurrentPage == null)
                return;
            var currentHeader = DemoNav.CurrentPage.Header as string ?? "Page";
            DemoNav.CurrentPage.Header = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new Border
                    {
                        Width = 24, Height = 24,
                        CornerRadius = new Avalonia.CornerRadius(12),
                        Background = new SolidColorBrush(Color.Parse("#4CAF50")),
                    },
                    new StackPanel
                    {
                        Spacing = 0,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            new TextBlock { Text = currentHeader, FontSize = 14, FontWeight = FontWeight.SemiBold },
                            new TextBlock { Text = "Online", FontSize = 10, Opacity = 0.6 },
                        },
                    },
                },
            };
            StatusText.Text = "Custom: Icon + Subtitle";
        }

        private void OnPushWithStringHeader(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            var page = MakePage($"Page {_pageCount}", "This page uses a string Header.", _pageCount);
            DemoNav.Push(page);
            StatusText.Text = $"Pushed: \"Page {_pageCount}\"";
        }

        private void OnPushWithCustomHeader(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            var progressHeader = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"Step {_pageCount}",
                        FontSize = 16,
                        FontWeight = FontWeight.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    new ProgressBar
                    {
                        Width = 100,
                        Minimum = 0,
                        Maximum = 100,
                        Value = _pageCount * 25,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                },
            };
            var page = new ContentPage
            {
                Header = progressHeader,
                Background = new SolidColorBrush(PageColors[_pageCount % PageColors.Length]),
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"Step {_pageCount}",
                            FontSize = 24,
                            FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = "This page has a custom Header\nwith a progress bar.",
                            FontSize = 14,
                            Opacity = 0.6,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                        },
                    },
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            DemoNav.Push(page);
            StatusText.Text = $"Pushed: Custom Header (Step {_pageCount})";
        }

        private async void OnPop(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PopAsync();
            StatusText.Text = $"Current: {DemoNav.CurrentPage?.Header as string ?? "(custom control)"}";
        }

        private static ContentPage MakePage(string header, string body, int index) =>
            new ContentPage
            {
                Header = header,
                Background = new SolidColorBrush(PageColors[index % PageColors.Length]),
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = header,
                            FontSize = 18,
                            FontWeight = FontWeight.SemiBold,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = body,
                            FontSize = 13,
                            Opacity = 0.7,
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center,
                            MaxWidth = 240
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
    }
}
