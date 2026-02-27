using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageGesturePage : UserControl
    {
        private static readonly Color[] PageColors =
        {
            Color.Parse("#E3F2FD"), Color.Parse("#F3E5F5"), Color.Parse("#E8F5E9"),
            Color.Parse("#FFF3E0"), Color.Parse("#FCE4EC"),
        };

        public NavigationPageGesturePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(BuildPage("Page 1", 0), null);
            await DemoNav.PushAsync(BuildPage("Page 2", 1), null);
            await DemoNav.PushAsync(BuildPage("Page 3", 2), null);
            UpdateStatus();
        }

        private void OnGestureEnabledChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoNav == null)
                return;
            DemoNav.IsGestureEnabled = GestureCheck.IsChecked == true;
        }

        private async void OnPushPages(object? sender, RoutedEventArgs e)
        {
            var depth = DemoNav.StackDepth;
            await DemoNav.PushAsync(BuildPage($"Page {depth + 1}", depth), null);
            UpdateStatus();
        }

        private async void OnPop(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PopAsync();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Depth: {DemoNav.StackDepth}";
        }

        private static ContentPage BuildPage(string title, int colorIndex) =>
            new ContentPage
            {
                Header = title,
                Background = new SolidColorBrush(PageColors[colorIndex % PageColors.Length]),
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontSize = 20,
                            FontWeight = FontWeight.SemiBold,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = "← Drag from the left edge to go back",
                            FontSize = 13,
                            Opacity = 0.7,
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
    }
}
