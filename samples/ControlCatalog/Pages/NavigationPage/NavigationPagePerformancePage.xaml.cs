using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class NavigationPagePerformancePage : UserControl
    {
        private readonly NavigationPerformanceMonitorHelper _perf = new();
        private int _pageCounter;

        private readonly List<(Border Container, Border Badge, TextBlock IndexText,
            TextBlock TitleText, TextBlock BadgeText)> _stackRowCache = new();

        public NavigationPagePerformancePage()
        {
            InitializeComponent();
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            DemoNav.Pushed += OnStackChanged;
            DemoNav.Popped += OnStackChanged;
            DemoNav.PoppedToRoot += OnStackChanged;

            _perf.InitHeap();
            _perf.OpStopwatch.Restart();
            _pageCounter++;
            await DemoNav.PushAsync(_perf.BuildTrackedPage("Home", _pageCounter), null);
            _perf.OpStopwatch.Stop();
            Log("Init", "Pushed root page");
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            _perf.StopAutoRefresh();
        }

        private void OnStackChanged(object? sender, NavigationEventArgs e) => RefreshAll();

        private void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCounter++;
            var page = _perf.BuildTrackedPage($"Page {_pageCounter}", _pageCounter);
            _perf.OpStopwatch.Restart();
            DemoNav.Push(page);
            _perf.StopMetrics(LastOpTimeText);
            Log("Push", $"Pushed \"{page.Header}\"");
        }

        private void OnPush5(object? sender, RoutedEventArgs e)
        {
            int first = _pageCounter + 1;
            _perf.OpStopwatch.Restart();
            for (int i = 0; i < 5; i++)
            {
                _pageCounter++;
                DemoNav.Push(_perf.BuildTrackedPage($"Page {_pageCounter}", _pageCounter));
            }
            _perf.StopMetrics(LastOpTimeText);
            Log("Push ×5", $"Pushed pages {first}–{_pageCounter}");
        }

        private void OnPop(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.StackDepth > 1)
            {
                var header = DemoNav.CurrentPage?.Header?.ToString();
                _perf.OpStopwatch.Restart();
                DemoNav.Pop();
                _perf.StopMetrics(LastOpTimeText);
                Log("Pop", $"Popped \"{header}\"");
            }
        }

        private async void OnPopToRoot(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.StackDepth > 1)
            {
                int removed = DemoNav.StackDepth - 1;
                _perf.OpStopwatch.Restart();
                await DemoNav.PopToRootAsync();
                _perf.StopMetrics(LastOpTimeText);
                Log("PopToRoot", $"Removed {removed} page(s)");
            }
        }

        private void OnForceGC(object? sender, RoutedEventArgs e)
        {
            _perf.ForceGC(RefreshAll);
            Log("GC", "Forced full garbage collection");
        }

        private void OnClearLog(object? sender, RoutedEventArgs e) => LogPanel.Children.Clear();

        private void OnAutoRefreshChanged(object? sender, RoutedEventArgs e) =>
            _perf.OnAutoRefreshChanged(AutoRefreshCheck, RefreshAll);

        private void RefreshAll()
        {
            StackDepthText.Text    = $"Stack Depth: {DemoNav.StackDepth}";
            LiveInstancesText.Text = $"Live Page Instances: {_perf.CountLiveInstances()}";
            TotalCreatedText.Text  = $"Total Pages Created: {_perf.TotalCreated}";
            _perf.UpdateHeapDelta(ManagedMemoryText, MemoryDeltaText);

            NavigationPerformanceMonitorHelper.RefreshStackPanel(
                StackItemsPanel, _stackRowCache,
                DemoNav.NavigationStack, DemoNav.CurrentPage);
        }

        private void Log(string action, string detail) =>
            _perf.LogOperation(action, detail, LogPanel, LogScrollViewer,
                $"depth {DemoNav.StackDepth}");
    }
}
