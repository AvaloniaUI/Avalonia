using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageEventsPage : UserControl
    {
        private int _pageCount;

        public NavigationPageEventsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            DemoNav.Pushed       += (s, ev) => AddLog($"Pushed → {ev.Page?.Header}");
            DemoNav.Popped       += (s, ev) => AddLog($"Popped ← {ev.Page?.Header}");
            DemoNav.PoppedToRoot += (s, ev) => AddLog("PoppedToRoot");
            DemoNav.PageInserted += (s, ev) => AddLog($"PageInserted: {ev.Page?.Header}");
            DemoNav.PageRemoved  += (s, ev) => AddLog($"PageRemoved: {ev.Page?.Header}");
            DemoNav.ModalPushed  += (s, ev) => AddLog($"ModalPushed → {ev.Modal?.Header}");
            DemoNav.ModalPopped  += (s, ev) => AddLog($"ModalPopped ← {ev.Modal?.Header}");

            var root = NavigationDemoHelper.MakePage("Home", "Push pages and watch the event log.\nAll navigation events are captured.", 0);
            SubscribePage(root);
            await DemoNav.PushAsync(root, null);
        }

        private void SubscribePage(ContentPage page)
        {
            page.NavigatedTo  += (s, e) => AddLog($"[{page.Header}] NavigatedTo");
            page.NavigatedFrom += (s, e) => AddLog($"[{page.Header}] NavigatedFrom");
            page.Navigating += args =>
            {
                AddLog($"[{page.Header}] Navigating");
                return System.Threading.Tasks.Task.CompletedTask;
            };
        }

        private async void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            var page = NavigationDemoHelper.MakePage($"Page {_pageCount}", "Navigate back to see events.", _pageCount);
            SubscribePage(page);
            await DemoNav.PushAsync(page);
        }

        private async void OnPop(object? sender, RoutedEventArgs e) => await DemoNav.PopAsync();
        private async void OnPopToRoot(object? sender, RoutedEventArgs e) => await DemoNav.PopToRootAsync();

        private void OnInsertPage(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.CurrentPage == null)
                return;
            _pageCount++;
            var page = NavigationDemoHelper.MakePage($"Inserted {_pageCount}", "Inserted below the current page.", _pageCount);
            SubscribePage(page);
            DemoNav.InsertPage(page, DemoNav.CurrentPage);
        }

        private void OnRemovePage(object? sender, RoutedEventArgs e)
        {
            var stack = DemoNav.NavigationStack;
            if (stack.Count < 2)
                return;
            DemoNav.RemovePage(stack[stack.Count - 2]);
        }

        private async void OnPushModal(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            var page = NavigationDemoHelper.MakePage($"Modal {_pageCount}", "Dismiss to see ModalPopped event.", _pageCount);
            SubscribePage(page);
            await DemoNav.PushModalAsync(page);
        }

        private async void OnPopModal(object? sender, RoutedEventArgs e) => await DemoNav.PopModalAsync();

        private void OnClearLog(object? sender, RoutedEventArgs e)
        {
            LogPanel.Children.Clear();
        }

        private void AddLog(string message)
        {
            LogPanel.Children.Insert(0, new TextBlock
            {
                Text = message,
                FontFamily = new FontFamily("Cascadia Code,Consolas,Menlo,monospace"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 1),
            });
        }
    }
}
