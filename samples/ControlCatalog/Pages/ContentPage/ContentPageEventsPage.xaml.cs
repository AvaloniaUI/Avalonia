using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class ContentPageEventsPage : UserControl
    {
        private int _pageCount;
        private readonly ObservableCollection<string> _logItems = new();

        public ContentPageEventsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            EventLogItems.ItemsSource = _logItems;

            var root = MakePage("Root", "Navigate to see lifecycle events in the log below.");
            SubscribeEvents(root);
            await DemoNav.PushAsync(root);
        }

        private void SubscribeEvents(ContentPage page)
        {
            page.NavigatedTo  += (_, e) => AddLog($"[{page.Header}] NavigatedTo (type: {e.NavigationType})");
            page.NavigatedFrom += (_, _) => AddLog($"[{page.Header}] NavigatedFrom");
            page.Navigating   += async args =>
            {
                if (BlockNavCheck.IsChecked == true)
                {
                    args.Cancel = true;
                    AddLog($"[{page.Header}] Navigating — BLOCKED");
                    await Task.Delay(600);
                    args.Cancel = false;
                    AddLog($"[{page.Header}] Navigating — unblocked");
                }
                else
                {
                    AddLog($"[{page.Header}] Navigating");
                }
            };
        }

        private async void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCount++;
            var page = MakePage($"Page {_pageCount}", "Navigate back to see Navigating/NavigatedFrom.");
            SubscribeEvents(page);
            await DemoNav.PushAsync(page);
        }

        private async void OnPop(object? sender, RoutedEventArgs e) => await DemoNav.PopAsync();

        private void OnClearLog(object? sender, RoutedEventArgs e) => _logItems.Clear();

        private void AddLog(string message)
        {
            _logItems.Add(message);
            LogScrollViewer.ScrollToEnd();
        }

        private static ContentPage MakePage(string header, string body) =>
            new ContentPage
            {
                Header = header,
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = header,
                            FontSize = 24,
                            FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = body,
                            FontSize = 13,
                            Opacity = 0.6,
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            MaxWidth = 260,
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment   = VerticalAlignment.Stretch,
            };
    }
}
