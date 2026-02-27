using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageFirstLookPage : UserControl
    {
        private static readonly Color[] PageColors =
        {
            Color.Parse("#BBDEFB"), Color.Parse("#C8E6C9"), Color.Parse("#FFE0B2"),
            Color.Parse("#E1BEE7"), Color.Parse("#FFCDD2"), Color.Parse("#B2EBF2"),
        };

        private int _pageCount;

        public NavigationPageFirstLookPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(MakePage("Home", "Welcome!\nUse the buttons to push and pop pages.", 0), null);
            UpdateStatus();
        }

        private void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            var page = MakePage($"Page {_pageCount}", $"This is page {_pageCount}.", _pageCount);
            NavigationPage.SetHasNavigationBar(page, HasNavBarCheck.IsChecked == true);
            NavigationPage.SetHasBackButton(page, HasBackButtonCheck.IsChecked == true);
            DemoNav.Push(page);
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

        private void OnHasNavBarChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoNav == null)
                return;
            if (DemoNav.CurrentPage != null)
                NavigationPage.SetHasNavigationBar(DemoNav.CurrentPage, HasNavBarCheck.IsChecked == true);
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Depth: {DemoNav.StackDepth}";
            HeaderText.Text = $"Current: {DemoNav.CurrentPage?.Header ?? "(none)"}";
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
