using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class ContentPageFirstLookPage : UserControl
    {
        private static readonly Color[] PageColors =
        [
            Color.FromRgb(0xE3, 0xF2, 0xFD), // blue
            Color.FromRgb(0xF3, 0xE5, 0xF5), // purple
            Color.FromRgb(0xE8, 0xF5, 0xE9), // green
            Color.FromRgb(0xFF, 0xF8, 0xE1), // amber
            Color.FromRgb(0xFB, 0xE9, 0xE7), // deep orange
        ];

        private int _pageCount;

        public ContentPageFirstLookPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(MakePage("Root Page", "ContentPage inside a NavigationPage.\nUse the options to navigate."));
            UpdateStatus();
        }

        private async void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            await DemoNav.PushAsync(MakePage($"Page {_pageCount}", $"ContentPage #{_pageCount}.\nNavigate back using the back button."));
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

        private ContentPage MakePage(string header, string body) =>
            new ContentPage
            {
                Header = header,
                Background = new SolidColorBrush(PageColors[_pageCount % PageColors.Length]),
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
