using System;
using System.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace ControlCatalog.Pages
{
    public partial class TabbedPagePerformancePage : UserControl
    {
        private readonly NavigationPerformanceMonitorHelper _perf = new();
        private int _counter;

        public TabbedPagePerformancePage()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                DemoTabs.SelectionChanged += (_, _) => RefreshStats();
            };
        }

        private void AddTabs(int count)
        {
            var pages = (IList)DemoTabs.Pages!;
            _perf.OpStopwatch.Restart();
            for (int i = 0; i < count; i++)
            {
                var idx = ++_counter;
                var page = new ContentPage
                {
                    Header = $"T{idx}",
                    Content = new TextBlock
                    {
                        Text = $"Tab {idx}",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 18,
                        Opacity = 0.7
                    },
                    Tag = new byte[51200],
                };
                _perf.TrackPage(page);
                pages.Add(page);
            }

            _perf.StopMetrics(LastOpTimeText);
            RefreshStats();
        }

        private void RemoveTabs(int count)
        {
            var pages = (IList)DemoTabs.Pages!;
            _perf.OpStopwatch.Restart();
            for (int i = 0; i < count && pages.Count > 0; i++)
                pages.RemoveAt(pages.Count - 1);

            _perf.StopMetrics(LastOpTimeText);
            RefreshStats();
        }

        private void OnAdd5(object? sender, RoutedEventArgs e) => AddTabs(5);
        private void OnAdd20(object? sender, RoutedEventArgs e) => AddTabs(20);
        private void OnRemove5(object? sender, RoutedEventArgs e) => RemoveTabs(5);

        private void OnClearAll(object? sender, RoutedEventArgs e)
        {
            var pages = (IList)DemoTabs.Pages!;
            _perf.OpStopwatch.Restart();
            while (pages.Count > 0)
                pages.RemoveAt(pages.Count - 1);
            _perf.StopMetrics(LastOpTimeText);
            RefreshStats();
        }

        private void OnForceGC(object? sender, RoutedEventArgs e)
        {
            _perf.ForceGC(RefreshStats);
        }

        private void OnRefresh(object? sender, RoutedEventArgs e) => RefreshStats();

        private void RefreshStats()
        {
            var pages = (IList)DemoTabs.Pages!;
            TabCountText.Text = $"Tab count: {pages.Count}";
            LiveCountText.Text = $"Live instances: {_perf.CountLiveInstances()} / {_perf.TotalCreated} tracked";
            HeapText.Text = $"Heap: {GC.GetTotalMemory(false) / 1024:N0} KB";
            AllocText.Text = $"Total allocated: {GC.GetTotalAllocatedBytes() / 1024:N0} KB";
        }
    }
}
