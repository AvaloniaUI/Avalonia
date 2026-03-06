using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageToolbarPage : UserControl
    {
        private int _pageCount;
        private int _itemCount;
        private ContentPage? _rootPage;
        private readonly CommandBar _rootCommandBar = new CommandBar { IsDynamicOverflowEnabled = true };

        public NavigationPageToolbarPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _rootPage = NavigationDemoHelper.MakePage("CommandBar Demo",
                "Use the panel to add CommandBar items.\nTop items appear inside the navigation bar.\nBottom items appear as a separate bar.", 0);
            ApplyPosition();
            await DemoNav.PushAsync(_rootPage, null);
            UpdateStatus();
        }

        private void OnPositionChanged(object? sender, SelectionChangedEventArgs e) => ApplyPosition();

        private void ApplyPosition()
        {
            if (_rootPage == null)
                return;
            NavigationPage.SetTopCommandBar(_rootPage, null);
            NavigationPage.SetBottomCommandBar(_rootPage, null);
            if (PositionCombo.SelectedIndex == 1)
                NavigationPage.SetBottomCommandBar(_rootPage, _rootCommandBar);
            else
                NavigationPage.SetTopCommandBar(_rootPage, _rootCommandBar);
        }

        private void OnAddPrimary(object? sender, RoutedEventArgs e)
        {
            _itemCount++;
            _rootCommandBar.PrimaryCommands.Add(new AppBarButton
            {
                Label = $"Item {_itemCount}",
                Icon = new PathIcon { Data = (Geometry)this.FindResource("AddIcon")! }
            });
            UpdateStatus();
        }

        private void OnAddSecondary(object? sender, RoutedEventArgs e)
        {
            _itemCount++;
            _rootCommandBar.SecondaryCommands.Add(new AppBarButton
            {
                Label = $"Secondary {_itemCount}"
            });
            UpdateStatus();
        }

        private void OnAddPrimarySeparator(object? sender, RoutedEventArgs e)
        {
            _rootCommandBar.PrimaryCommands.Add(new AppBarSeparator());
            UpdateStatus();
        }

        private void OnAddSecondarySeparator(object? sender, RoutedEventArgs e)
        {
            _rootCommandBar.SecondaryCommands.Add(new AppBarSeparator());
            UpdateStatus();
        }

        private void OnClearAll(object? sender, RoutedEventArgs e)
        {
            _rootCommandBar.PrimaryCommands.Clear();
            _rootCommandBar.SecondaryCommands.Clear();
            _itemCount = 0;
            UpdateStatus();
        }

        private async void OnPushWithToolbar(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            var page = NavigationDemoHelper.MakePage($"Page {_pageCount}", "CommandBar is shown in the nav bar.", _pageCount);

            var bar = BuildPresetCommandBar();
            if (PositionCombo.SelectedIndex == 1)
                NavigationPage.SetBottomCommandBar(page, bar);
            else
                NavigationPage.SetTopCommandBar(page, bar);

            await DemoNav.PushAsync(page);
            UpdateStatus();
        }

        private async void OnPushWithoutToolbar(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            await DemoNav.PushAsync(NavigationDemoHelper.MakePage($"Page {_pageCount}", "No toolbar on this page.", _pageCount));
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

        private CommandBar BuildPresetCommandBar() =>
            new CommandBar
            {
                PrimaryCommands =
                {
                    new AppBarButton { Label = "Search", Icon = new PathIcon { Data = (Geometry)this.FindResource("SearchIcon")! } },
                    new AppBarButton { Label = "Share",  Icon = new PathIcon { Data = (Geometry)this.FindResource("ShareIcon")! } },
                    new AppBarButton { Label = "Edit",   Icon = new PathIcon { Data = (Geometry)this.FindResource("EditIcon")! } },
                },
                SecondaryCommands =
                {
                    new AppBarButton { Label = "Delete", Icon = new PathIcon { Data = (Geometry)this.FindResource("DeleteIcon")! } }
                }
            };
    }
}
