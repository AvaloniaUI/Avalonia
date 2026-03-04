using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageStackPage : UserControl
    {
        private int _pageCount;

        public NavigationPageStackPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            DemoNav.Pushed       += (s, ev) => RefreshStack();
            DemoNav.Popped       += (s, ev) => RefreshStack();
            DemoNav.PoppedToRoot += (s, ev) => RefreshStack();
            DemoNav.PageInserted += (s, ev) => RefreshStack();
            DemoNav.PageRemoved  += (s, ev) => RefreshStack();

            _pageCount++;
            await DemoNav.PushAsync(NavigationDemoHelper.MakePage("Home", $"Stack position #{_pageCount}", _pageCount), null);
            // RefreshStack is called via the Pushed event above.
        }

        private async void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            await DemoNav.PushAsync(NavigationDemoHelper.MakePage($"Page {_pageCount}", $"Stack position #{_pageCount}", _pageCount));
        }

        private void OnInsert(object? sender, RoutedEventArgs e)
        {
            var current = DemoNav.CurrentPage;
            if (current == null || DemoNav.StackDepth <= 1)
                return;

            _pageCount++;
            var inserted = NavigationDemoHelper.MakePage($"Inserted {_pageCount}", $"Stack position #{_pageCount}", _pageCount);
            DemoNav.InsertPage(inserted, current);
            RefreshStack();
        }

        private void RefreshStack()
        {
            var stack   = DemoNav.NavigationStack;
            var depth   = stack.Count;
            var current = DemoNav.CurrentPage;

            DepthLabel.Text = $"depth: {depth}";

            StackDisplay.Children.Clear();

            // Render from top (current) down to root.
            for (int i = depth - 1; i >= 0; i--)
            {
                var page      = stack[i];
                var isCurrent = ReferenceEquals(page, current);
                var isRoot    = i == 0;

                StackDisplay.Children.Add(BuildStackEntry(page, i + 1, isCurrent, isRoot));
            }
        }

        private Border BuildStackEntry(Page page, int position, bool isCurrent, bool isRoot)
        {
            var background = NavigationDemoHelper.GetPageBrush(position - 1);
            var badge = new Border
            {
                Width             = 24,
                Height            = 24,
                CornerRadius      = new CornerRadius(12),
                Background        = background,
                VerticalAlignment = VerticalAlignment.Center,
                Child             = new TextBlock
                {
                    Text                = position.ToString(),
                    FontSize            = 11,
                    FontWeight          = FontWeight.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                }
            };
            var title = new TextBlock
            {
                Text              = page.Header?.ToString() ?? "(untitled)",
                FontWeight        = isCurrent ? FontWeight.SemiBold : FontWeight.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming      = TextTrimming.CharacterEllipsis,
                Margin            = new Thickness(6, 0, 0, 0),
            };
            string? badgeText  = isCurrent ? "current" : (isRoot ? "root" : null);
            var badgeLabel = new TextBlock
            {
                Text              = badgeText,
                FontSize          = 10,
                Opacity           = 0.5,
                VerticalAlignment = VerticalAlignment.Center,
                IsVisible         = badgeText != null,
                Margin            = new Thickness(4, 0, 0, 0),
            };

            // Remove button (disabled when it is the only page in the stack)
            var removeBtn = new Button
            {
                Content           = "Remove",
                FontSize          = 11,
                Padding           = new Thickness(6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                IsEnabled         = !(isRoot && DemoNav.StackDepth == 1),
            };
            removeBtn.Click += (_, _) =>
            {
                DemoNav.RemovePage(page);
                RefreshStack();
            };

            // Insert-before button (not shown for current page — use the dedicated button instead)
            var insertBtn = new Button
            {
                Content           = "Insert \u2191",
                FontSize          = 11,
                Padding           = new Thickness(6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                IsVisible         = !isCurrent,
            };
            ToolTip.SetTip(insertBtn, $"Insert a new page before \"{page.Header}\"");
            insertBtn.Click += (_, _) =>
            {
                _pageCount++;
                var inserted = NavigationDemoHelper.MakePage($"Inserted {_pageCount}", $"Stack position #{_pageCount}", _pageCount);
                DemoNav.InsertPage(inserted, page);
                RefreshStack();
            };

            var buttonsPanel = new StackPanel
            {
                Orientation       = Orientation.Horizontal,
                Spacing           = 4,
                VerticalAlignment = VerticalAlignment.Center,
            };
            buttonsPanel.Children.Add(removeBtn);
            buttonsPanel.Children.Add(insertBtn);

            var titleRow = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(buttonsPanel, Dock.Right);
            titleRow.Children.Add(buttonsPanel);
            titleRow.Children.Add(badge);
            titleRow.Children.Add(title);
            titleRow.Children.Add(badgeLabel);

            return new Border
            {
                BorderBrush     = new SolidColorBrush(isCurrent
                                    ? Color.Parse("#0078D4")
                                    : Color.Parse("#CCCCCC")),
                BorderThickness = new Thickness(isCurrent ? 2 : 1),
                CornerRadius    = new CornerRadius(6),
                Padding         = new Thickness(8, 6),
                Child           = titleRow,
            };
        }
    }
}
