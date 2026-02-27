using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageToolbarPage : UserControl
    {
        private static readonly Color[] PageColors =
        {
            Color.Parse("#BBDEFB"), Color.Parse("#C8E6C9"), Color.Parse("#FFE0B2"),
            Color.Parse("#E1BEE7"), Color.Parse("#FFCDD2"), Color.Parse("#B2EBF2"),
        };

        private int _pageCount;
        private int _itemCount;
        private ContentPage? _rootPage;
        private readonly CommandBar _rootCommandBar = new CommandBar();

        public NavigationPageToolbarPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _rootPage = new ContentPage
            {
                Header = "Root Page",
                Background = new SolidColorBrush(PageColors[0]),
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "CommandBar Demo",
                            FontSize = 18,
                            FontWeight = FontWeight.SemiBold,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = "Use the panel to add CommandBar items.\nTop items appear inside the navigation bar.\nBottom items appear as a separate bar.",
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

        private void OnPushWithToolbar(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            var page = MakePage($"Page {_pageCount}", "CommandBar is shown in the nav bar.", _pageCount);

            var bar = BuildPresetCommandBar();
            if (PositionCombo.SelectedIndex == 1)
                NavigationPage.SetBottomCommandBar(page, bar);
            else
                NavigationPage.SetTopCommandBar(page, bar);

            DemoNav.Push(page);
            UpdateStatus();
        }

        private void OnPushWithoutToolbar(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            DemoNav.Push(MakePage($"Page {_pageCount}", "No toolbar on this page.", _pageCount));
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
                    new AppBarButton
                    {
                        Label = "Search",
                        Icon = new PathIcon { Data = (Geometry)this.FindResource("SearchIcon")! }
                    },
                    new AppBarButton
                    {
                        Label = "Share",
                        Icon = new PathIcon { Data = (Geometry)this.FindResource("ShareIcon")! }
                    },
                    new AppBarButton
                    {
                        Label = "Edit",
                        Icon = new PathIcon { Data = (Geometry)this.FindResource("EditIcon")! }
                    },
                },
                SecondaryCommands =
                {
                    new AppBarButton
                    {
                        Label = "Delete",
                        Icon = new PathIcon { Data = (Geometry)this.FindResource("DeleteIcon")! }
                    }
                }
            };

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
