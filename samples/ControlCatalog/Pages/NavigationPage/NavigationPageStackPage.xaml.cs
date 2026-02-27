using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageStackPage : UserControl
    {
        private static readonly Color[] PageColors =
        {
            Color.Parse("#BBDEFB"), Color.Parse("#C8E6C9"), Color.Parse("#FFE0B2"),
            Color.Parse("#E1BEE7"), Color.Parse("#FFCDD2"), Color.Parse("#B2EBF2"),
        };

        private int _pageCount;

        public NavigationPageStackPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            DemoNav.Pushed += (s, ev) => RefreshStack();
            DemoNav.Popped += (s, ev) => RefreshStack();
            DemoNav.PoppedToRoot += (s, ev) => RefreshStack();
            DemoNav.PageInserted += (s, ev) => RefreshStack();
            DemoNav.PageRemoved += (s, ev) => RefreshStack();

            await DemoNav.PushAsync(BuildPage("Root", 0), null);
            RefreshStack();
        }

        private void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            DemoNav.Push(BuildPage($"Page {_pageCount}", _pageCount));
        }

        private void OnInsert(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.CurrentPage == null)
                return;
            _pageCount++;
            var newPage = BuildPage($"Inserted {_pageCount}", _pageCount);
            DemoNav.InsertPage(newPage, DemoNav.CurrentPage);
        }

        private void OnRemoveCurrent(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.StackDepth <= 1)
                return;
            var current = DemoNav.CurrentPage;
            if (current != null)
                DemoNav.RemovePage(current);
        }

        private async void OnPopToRoot(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PopToRootAsync();
        }

        private void RefreshStack()
        {
            var stack = DemoNav.NavigationStack;
            DepthLabel.Text = $"Depth: {stack.Count}";

            StackDisplay.Children.Clear();
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                var page = stack[i];
                var isCurrent = i == stack.Count - 1;

                var entry = new Border
                {
                    Background = isCurrent
                        ? new SolidColorBrush(Color.Parse("#2196F3"))
                        : new SolidColorBrush(Color.Parse("#E0E0E0")),
                    CornerRadius = new Avalonia.CornerRadius(3),
                    Padding = new Avalonia.Thickness(6, 3),
                    Child = new TextBlock
                    {
                        Text = isCurrent ? $"▶ {page.Header} (current)" : $"  {page.Header}",
                        Foreground = isCurrent ? Brushes.White : Brushes.Black,
                        FontSize = 11
                    }
                };

                StackDisplay.Children.Add(entry);
            }
        }

        private ContentPage BuildPage(string title, int index) =>
            new ContentPage
            {
                Header = title,
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
                            Text = title,
                            FontSize = 18,
                            FontWeight = FontWeight.SemiBold,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = $"Position {index} in the stack",
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
