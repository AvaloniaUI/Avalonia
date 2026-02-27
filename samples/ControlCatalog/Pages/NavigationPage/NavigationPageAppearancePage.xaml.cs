using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageAppearancePage : UserControl
    {
        private static readonly Color[] PageColors =
        {
            Color.Parse("#BBDEFB"), Color.Parse("#C8E6C9"), Color.Parse("#FFE0B2"),
            Color.Parse("#E1BEE7"), Color.Parse("#FFCDD2"), Color.Parse("#B2EBF2"),
        };

        private int _pageCount;
        private int _backButtonStyle;

        public NavigationPageAppearancePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(MakePage("Appearance", "Change bar properties using the options panel.", 0), null);
        }

        private void OnHasNavBarChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoNav == null)
                return;
            var show = HasNavBarCheck.IsChecked == true;
            foreach (var p in DemoNav.NavigationStack)
                NavigationPage.SetHasNavigationBar(p, show);
        }

        private void OnHasShadowChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoNav == null)
                return;
            DemoNav.HasShadow = HasShadowCheck.IsChecked == true;
        }

        private void OnBarBgChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoNav == null)
                return;
            DemoNav.BarBackground = BarBgCombo.SelectedIndex switch
            {
                1 => new SolidColorBrush(Colors.DodgerBlue),
                2 => new SolidColorBrush(Colors.DarkSlateGray),
                3 => new SolidColorBrush(Colors.Indigo),
                4 => new SolidColorBrush(Colors.Crimson),
                5 => Brushes.Transparent,
                6 => new SolidColorBrush(Color.FromArgb(80, 20, 20, 20)),
                _ => null
            };
        }

        private void OnBarFgChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoNav == null)
                return;
            DemoNav.BarForeground = BarFgCombo.SelectedIndex switch
            {
                1 => Brushes.White,
                2 => Brushes.Black,
                3 => Brushes.Yellow,
                _ => null
            };
        }

        private void OnBarHeightChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (DemoNav == null)
                return;
            var value = (int)BarHeightSlider.Value;
            DemoNav.BarHeight = value;
            BarHeightLabel.Text = value.ToString();
        }

        private void OnPerPageBarHeightChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoNav?.CurrentPage == null)
                return;
            double? height = PerPageBarHeightCombo.SelectedIndex switch
            {
                1 => 32.0,
                2 => 72.0,
                _ => null
            };
            NavigationPage.SetBarHeight(DemoNav.CurrentPage, height);
        }

        private void OnTitleStyleChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoNav?.CurrentPage == null)
                return;
            DemoNav.CurrentPage.Header = TitleStyleCombo.SelectedIndex switch
            {
                1 => (object)new TextBlock
                {
                    Text = "Big Title",
                    FontSize = 26,
                    FontWeight = FontWeight.Black,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                2 => new StackPanel
                {
                    Spacing = 0,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = "Custom Title", FontSize = 15, FontWeight = FontWeight.SemiBold },
                        new TextBlock { Text = "With subtitle", FontSize = 11, Opacity = 0.5 },
                    }
                },
                _ => "Appearance"
            };
        }

        private void OnBarLayoutChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DemoNav == null)
                return;
            var behavior = BarLayoutCombo.SelectedIndex == 1
                ? BarLayoutBehavior.Overlay
                : BarLayoutBehavior.Inset;
            foreach (var p in DemoNav.NavigationStack)
                NavigationPage.SetBarLayoutBehavior(p, behavior);
        }

        private void OnBackButtonStyleChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (BackButtonStyleCombo == null)
                return;
            _backButtonStyle = BackButtonStyleCombo.SelectedIndex;
        }

        private void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            var page = MakePage($"Page {_pageCount}", "Check the back button style.", _pageCount);

            object? backContent = _backButtonStyle switch
            {
                1 => (object)"← Back",
                2 => "Cancel",
                _ => null
            };
            NavigationPage.SetBackButtonContent(page, backContent);

            if (BarLayoutCombo?.SelectedIndex == 1)
                NavigationPage.SetBarLayoutBehavior(page, BarLayoutBehavior.Overlay);

            NavigationPage.SetHasNavigationBar(page, HasNavBarCheck.IsChecked == true);
            DemoNav.Push(page);
        }

        private async void OnPop(object? sender, RoutedEventArgs e) => await DemoNav.PopAsync();

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
