using System;
using System.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace ControlCatalog.Pages
{
    public partial class CarouselPagePerformancePage : UserControl
    {
        private readonly NavigationPerformanceMonitorHelper _perf = new();
        private int _counter;

        public CarouselPagePerformancePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            AddPages(5);
            DemoCarousel.SelectionChanged += OnSelectionChanged;
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            DemoCarousel.SelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(object? sender, PageSelectionChangedEventArgs e) => RefreshStats();

        private void AddPages(int count)
        {
            var pages = (IList)DemoCarousel.Pages!;
            _perf.OpStopwatch.Restart();
            for (int i = 0; i < count; i++)
            {
                var idx = ++_counter;
                var page = new ContentPage
                {
                    Header = $"P{idx}",
                    Content = new TextBlock
                    {
                        Text = $"Page {idx}",
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

        private void RemovePages(int count)
        {
            var pages = (IList)DemoCarousel.Pages!;
            _perf.OpStopwatch.Restart();
            for (int i = 0; i < count && pages.Count > 0; i++)
                pages.RemoveAt(pages.Count - 1);

            _perf.StopMetrics(LastOpTimeText);
            RefreshStats();
        }

        private void OnAdd5(object? sender, RoutedEventArgs e) => AddPages(5);
        private void OnAdd20(object? sender, RoutedEventArgs e) => AddPages(20);
        private void OnRemove5(object? sender, RoutedEventArgs e) => RemovePages(5);

        private void OnClearAll(object? sender, RoutedEventArgs e)
        {
            var pages = (IList)DemoCarousel.Pages!;
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
            var pages = (IList)DemoCarousel.Pages!;
            PageCountText.Text = $"Page count: {pages.Count}";
            LiveCountText.Text = $"Live instances: {_perf.CountLiveInstances()} / {_perf.TotalCreated} tracked";
            HeapText.Text = $"Heap: {GC.GetTotalMemory(false) / 1024:N0} KB";
            AllocText.Text = $"Total allocated: {GC.GetTotalAllocatedBytes() / 1024:N0} KB";
        }
    }
}
