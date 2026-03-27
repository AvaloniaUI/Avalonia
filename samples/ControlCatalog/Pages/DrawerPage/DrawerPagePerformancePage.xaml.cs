using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class DrawerPagePerformancePage : UserControl
    {
        private readonly NavigationPerformanceMonitorHelper _perf = new();
        private readonly List<string> _detailHistory = new();
        private int _swapCounter;
        private int _pageCounter;

        private readonly List<(Border Container, Border Badge, TextBlock IndexText,
            TextBlock TitleText, TextBlock BadgeText)> _historyRowCache = new();

        public DrawerPagePerformancePage()
        {
            InitializeComponent();
        }

        private void OnControlLoaded(object? sender, RoutedEventArgs e)
        {
            _perf.InitHeap();
            _perf.OpStopwatch.Restart();
            _perf.TrackPage(DetailPage);
            _detailHistory.Add("Home");
            _perf.OpStopwatch.Stop();

            Log("Init", "Initial detail page: Home");
            RefreshAll();
        }

        private void OnControlUnloaded(object? sender, RoutedEventArgs e)
        {
            _perf.StopAutoRefresh();
        }

        private void OnMenuItemClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            var item = button.Tag?.ToString() ?? "Home";

            _perf.OpStopwatch.Restart();
            SwapDetailTo(item);
            DrawerPageControl.IsOpen = false;
            _perf.StopMetrics(LastOpTimeText);
        }

        private void OnSwapDetail(object? sender, RoutedEventArgs e)
        {
            _pageCounter++;
            _perf.OpStopwatch.Restart();
            SwapDetailTo($"Page {_pageCounter}");
            _perf.StopMetrics(LastOpTimeText);
        }

        private void OnSwap5(object? sender, RoutedEventArgs e)
        {
            _perf.OpStopwatch.Restart();
            for (int i = 0; i < 5; i++)
            {
                _pageCounter++;
                SwapDetailTo($"Page {_pageCounter}");
            }
            _perf.StopMetrics(LastOpTimeText);
        }

        private void OnToggleDrawer(object? sender, RoutedEventArgs e)
        {
            _perf.OpStopwatch.Restart();
            DrawerPageControl.IsOpen = !DrawerPageControl.IsOpen;
            _perf.StopMetrics(LastOpTimeText);
        }

        private void OnForceGC(object? sender, RoutedEventArgs e)
        {
            _perf.ForceGC(RefreshAll);
            Log("GC", "Forced full garbage collection");
        }

        private void OnClearLog(object? sender, RoutedEventArgs e) => LogPanel.Children.Clear();

        private void OnAutoRefreshChanged(object? sender, RoutedEventArgs e) =>
            _perf.OnAutoRefreshChanged(AutoRefreshCheck, RefreshAll);

        private void SwapDetailTo(string title)
        {
            _swapCounter++;

            var newDetail = NavigationDemoHelper.MakePage(title,
                $"Detail swap #{_swapCounter}\n\nEach detail page allocates ~50 KB to make memory changes visible. " +
                "Swap pages and watch the heap grow. The old detail page is replaced " +
                "but not freed until the garbage collector runs.", _swapCounter);
            newDetail.Tag = new byte[51200];
            _perf.TrackPage(newDetail);
            _detailHistory.Add(title);

            DrawerPageControl.Content = newDetail;

            Log("Swap", $"Detail \u2192 \"{title}\"");
            RefreshAll();
        }

        private void RefreshAll()
        {
            var currentDetail = DrawerPageControl.Content as Page;
            CurrentDetailText.Text = $"Current Detail: {currentDetail?.Header ?? "\u2014"}";
            SwapCountText.Text     = $"Detail Swaps: {_swapCounter}";
            LiveInstancesText.Text = $"Live Page Instances: {_perf.CountLiveInstances()}";
            TotalCreatedText.Text  = $"Total Pages Created: {_perf.TotalCreated}";
            _perf.UpdateHeapDelta(ManagedMemoryText, MemoryDeltaText);
            RefreshHistory();
        }

        private void RefreshHistory()
        {
            int start = Math.Max(0, _detailHistory.Count - 10);
            int count = _detailHistory.Count - start;

            while (_historyRowCache.Count < count)
                _historyRowCache.Add(NavigationPerformanceMonitorHelper.CreateStackRow());

            while (HistoryPanel.Children.Count > count)
                HistoryPanel.Children.RemoveAt(HistoryPanel.Children.Count - 1);
            while (HistoryPanel.Children.Count < count)
                HistoryPanel.Children.Add(_historyRowCache[HistoryPanel.Children.Count].Container);

            for (int displayIdx = 0; displayIdx < count; displayIdx++)
            {
                int historyIdx = _detailHistory.Count - 1 - displayIdx;
                bool isCurrent = historyIdx == _detailHistory.Count - 1;

                var row = _historyRowCache[displayIdx];
                if (!ReferenceEquals(HistoryPanel.Children[displayIdx], row.Container))
                    HistoryPanel.Children[displayIdx] = row.Container;

                NavigationPerformanceMonitorHelper.UpdateStackRow(row, historyIdx,
                    _detailHistory[historyIdx], isCurrent, isRoot: false);
            }
        }

        private void Log(string action, string detail) =>
            _perf.LogOperation(action, detail, LogPanel, LogScrollViewer);
    }
}
