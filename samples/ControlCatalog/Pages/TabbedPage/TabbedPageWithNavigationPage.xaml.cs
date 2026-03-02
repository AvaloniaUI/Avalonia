using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageWithNavigationPage : UserControl
    {
        public TabbedPageWithNavigationPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await BrowseNav.PushAsync(CreateListPage("Browse", "Items", BrowseNav), null);
            await SearchNav.PushAsync(CreateListPage("Search", "Results", SearchNav), null);
            await AccountNav.PushAsync(CreateListPage("Account", "Options", AccountNav), null);
        }

        private static ContentPage CreateListPage(string tabName, string listTitle, NavigationPage nav)
        {
            var list = new ListBox
            {
                Margin = new Avalonia.Thickness(8),
                Items =
                {
                    $"{listTitle} item 1",
                    $"{listTitle} item 2",
                    $"{listTitle} item 3",
                    $"{listTitle} item 4",
                    $"{listTitle} item 5"
                }
            };

            list.SelectionChanged += async (_, args) =>
            {
                if (args.AddedItems.Count == 0) return;
                var item = args.AddedItems[0]?.ToString() ?? string.Empty;
                list.SelectedItem = null;

                var detail = new ContentPage
                {
                    Header = item,
                    Content = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Spacing = 8,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = item,
                                FontSize = 20,
                                FontWeight = FontWeight.SemiBold,
                                HorizontalAlignment = HorizontalAlignment.Center
                            },
                            new TextBlock
                            {
                                Text = $"Detail view for \"{item}\" in the {tabName} tab.",
                                FontSize = 13,
                                Opacity = 0.7,
                                TextWrapping = TextWrapping.Wrap,
                                TextAlignment = Avalonia.Media.TextAlignment.Center,
                                MaxWidth = 280
                            }
                        }
                    }
                };

                await nav.PushAsync(detail, null);
            };

            var page = new ContentPage
            {
                Header = tabName,
                Content = list,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            NavigationPage.SetHasNavigationBar(page, false);
            return page;
        }

        private void OnPlacementChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoTabs == null) return;
            DemoTabs.TabPlacement = PlacementCombo.SelectedIndex == 0 ? TabPlacement.Top : TabPlacement.Bottom;
        }
    }
}
