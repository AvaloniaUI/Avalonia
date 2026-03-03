using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageFirstLookPage : UserControl
    {
        private int _pageCount;

        public NavigationPageFirstLookPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(NavigationDemoHelper.MakePage("Home", "Welcome!\nUse the buttons to push and pop pages.", 0), null);
            UpdateStatus();
        }

        private void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            var page = NavigationDemoHelper.MakePage($"Page {_pageCount}", $"This is page {_pageCount}.", _pageCount);
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

    }
}
