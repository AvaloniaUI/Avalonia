using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class ContentPageCommandBarPage : UserControl
    {
        private static readonly (string Label, string PathData)[] IconPresets =
        {
            ("Add",      "M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z"),
            ("Save",     "M15,9H5V5H15M12,19A3,3 0 0,1 9,16A3,3 0 0,1 12,13A3,3 0 0,1 15,16A3,3 0 0,1 12,19M17,3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V7L17,3Z"),
            ("Share",    "M18,16.08C17.24,16.08 16.56,16.38 16.04,16.85L8.91,12.7C8.96,12.47 9,12.24 9,12C9,11.76 8.96,11.53 8.91,11.3L15.96,7.19C16.5,7.69 17.21,8 18,8A3,3 0 0,0 21,5A3,3 0 0,0 18,2A3,3 0 0,0 15,5C15,5.24 15.04,5.47 15.09,5.7L8.04,9.81C7.5,9.31 6.79,9 6,9A3,3 0 0,0 3,12A3,3 0 0,0 6,15C6.79,15 7.5,14.69 8.04,14.19L15.16,18.34C15.11,18.55 15.08,18.77 15.08,19C15.08,20.61 16.39,21.91 18,21.91C19.61,21.91 20.92,20.61 20.92,19C20.92,17.39 19.61,16.08 18,16.08Z"),
            ("Favorite", "M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"),
            ("Delete",   "M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z"),
        };

        private int _itemCounter;
        private string _position = "Top";
        private readonly List<ICommandBarElement> _primaryItems = new();
        private readonly List<ICommandBarElement> _secondaryItems = new();

        public ContentPageCommandBarPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            var rootPage = new ContentPage
            {
                Header = "CommandBar Demo",
                Background = new SolidColorBrush(Color.Parse("#E3F2FD")),
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
                            Text = "Add items using the panel on the right,\nthen change the position to Top or Bottom.",
                            FontSize = 14,
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            Opacity = 0.7,
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            await DemoNav.PushAsync(rootPage);
        }

        private void OnPositionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (PositionCombo == null)
                return;
            _position = PositionCombo.SelectedIndex == 1 ? "Bottom" : "Top";
            RebuildCommandBar();
        }

        private void OnAddPrimary(object? sender, RoutedEventArgs e)
        {
            _itemCounter++;
            var btn = new AppBarButton { Label = $"Action {_itemCounter}" };
            if (UseIconCheck.IsChecked == true)
            {
                var preset = IconPresets[(_itemCounter - 1) % IconPresets.Length];
                btn.Label = preset.Label;
                btn.Icon = new PathIcon { Data = StreamGeometry.Parse(preset.PathData) };
                btn.IsCompact = true;
            }
            _primaryItems.Add(btn);
            RebuildCommandBar();
        }

        private void OnAddSecondary(object? sender, RoutedEventArgs e)
        {
            _itemCounter++;
            _secondaryItems.Add(new AppBarButton { Label = $"Item {_itemCounter}" });
            RebuildCommandBar();
        }

        private void OnAddSeparator(object? sender, RoutedEventArgs e)
        {
            _primaryItems.Add(new AppBarSeparator());
            RebuildCommandBar();
        }

        private void OnClearAll(object? sender, RoutedEventArgs e)
        {
            _primaryItems.Clear();
            _secondaryItems.Clear();
            _itemCounter = 0;
            ClearCommandBarFromActivePage();
            StatusText.Text = "No items added";
        }

        private async void OnPush(object? sender, RoutedEventArgs e)
        {
            var next = DemoNav.StackDepth + 1;
            var page = new ContentPage
            {
                Header = $"Page {next}",
                Background = new SolidColorBrush(Color.Parse("#E8F5E9")),
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"Page {next}",
                            FontSize = 28,
                            FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = "New page: no CommandBar set",
                            FontSize = 14,
                            Opacity = 0.6,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            await DemoNav.PushAsync(page);
        }

        private async void OnPop(object? sender, RoutedEventArgs e) => await DemoNav.PopAsync();

        private void ClearCommandBarFromActivePage()
        {
            if (DemoNav.CurrentPage is Page activePage)
            {
                NavigationPage.SetTopCommandBar(activePage, null);
                NavigationPage.SetBottomCommandBar(activePage, null);
            }
        }

        private void RebuildCommandBar()
        {
            if (DemoNav == null || DemoNav.CurrentPage is not Page activePage)
                return;
            if (_primaryItems.Count == 0 && _secondaryItems.Count == 0)
            {
                ClearCommandBarFromActivePage();
                StatusText.Text = "No items added";
                return;
            }

            NavigationPage.SetTopCommandBar(activePage, null);
            NavigationPage.SetBottomCommandBar(activePage, null);

            var commandBar = new CommandBar { IsDynamicOverflowEnabled = true };

            foreach (var item in _primaryItems)
            {
                if (item is AppBarButton btn)
                {
                    PathIcon? icon = null;
                    if (btn.Icon is PathIcon src)
                        icon = new PathIcon { Data = src.Data };
                    commandBar.PrimaryCommands.Add(new AppBarButton
                    {
                        Label = btn.Label,
                        Icon = icon,
                        IsCompact = btn.IsCompact,
                    });
                }
                else if (item is AppBarSeparator)
                    commandBar.PrimaryCommands.Add(new AppBarSeparator());
            }

            foreach (var item in _secondaryItems)
            {
                if (item is AppBarButton btn)
                    commandBar.SecondaryCommands.Add(new AppBarButton { Label = btn.Label });
            }

            if (_position == "Top")
                NavigationPage.SetTopCommandBar(activePage, commandBar);
            else
                NavigationPage.SetBottomCommandBar(activePage, commandBar);

            var primaryCount = _primaryItems.Count(i => i is AppBarButton);
            var secondaryCount = _secondaryItems.Count;
            StatusText.Text = $"{primaryCount} primary, {secondaryCount} secondary ({_position})";
        }
    }
}
