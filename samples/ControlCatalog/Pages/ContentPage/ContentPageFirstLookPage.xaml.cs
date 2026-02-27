using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class ContentPageFirstLookPage : UserControl
    {
        private int _pageCount;

        public ContentPageFirstLookPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            DemoNav.Push(MakePage("Root Page", "ContentPage inside a NavigationPage.\nUse the options to navigate."));
            UpdateStatus();
        }

        private void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            DemoNav.Push(MakePage($"Page {_pageCount}", $"ContentPage #{_pageCount}.\nNavigate back using the back button."));
            UpdateStatus();
        }

        private async void OnPop(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PopAsync();
            UpdateStatus();
        }

        private async void OnPopToRoot(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PopToRootAsync();
            _pageCount = 0;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Depth: {DemoNav.StackDepth} | Current: {DemoNav.CurrentPage?.Header}";
        }

        private static ContentPage MakePage(string header, string body) =>
            new ContentPage
            {
                Header = header,
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = header,
                            FontSize = 20,
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
                            MaxWidth = 260
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
    }
}
