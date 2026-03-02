using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class TabbedPagePerformancePage : UserControl
    {
        private int _counter;
        private readonly List<WeakReference<ContentPage>> _weakRefs = new();
        private readonly Stopwatch _opStopwatch = new();

        public TabbedPagePerformancePage()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                DemoTabs.SelectionChanged += (_, _) => RefreshStats();
            };
        }

        private void StopMetrics()
        {
            if (!_opStopwatch.IsRunning) return;
            _opStopwatch.Stop();
            LastOpTimeText.Text = $"Last Op: {_opStopwatch.ElapsedMilliseconds} ms";
        }

        private void AddTabs(int count)
        {
            var pages = (IList)DemoTabs.Pages!;
            _opStopwatch.Restart();
            for (int i = 0; i < count; i++)
            {
                var idx = ++_counter;
                var page = new ContentPage
                {
                    Header = $"T{idx}",
                    Content = new TextBlock
                    {
                        Text = $"Tab {idx}",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        FontSize = 18,
                        Opacity = 0.7
                    },
                    Tag = new byte[51200],
                };
                _weakRefs.Add(new WeakReference<ContentPage>(page));
                pages.Add(page);
            }

            StopMetrics();
            RefreshStats();
        }

        private void RemoveTabs(int count)
        {
            var pages = (IList)DemoTabs.Pages!;
            _opStopwatch.Restart();
            for (int i = 0; i < count && pages.Count > 0; i++)
                pages.RemoveAt(pages.Count - 1);

            StopMetrics();
            RefreshStats();
        }

        private void OnAdd5(object? sender, RoutedEventArgs e) => AddTabs(5);
        private void OnAdd20(object? sender, RoutedEventArgs e) => AddTabs(20);
        private void OnRemove5(object? sender, RoutedEventArgs e) => RemoveTabs(5);

        private void OnClearAll(object? sender, RoutedEventArgs e)
        {
            var pages = (IList)DemoTabs.Pages!;
            _opStopwatch.Restart();
            while (pages.Count > 0)
                pages.RemoveAt(pages.Count - 1);
            StopMetrics();
            RefreshStats();
        }

        private void OnForceGC(object? sender, RoutedEventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            RefreshStats();
        }

        private void OnRefresh(object? sender, RoutedEventArgs e) => RefreshStats();

        private void RefreshStats()
        {
            var pages = (IList)DemoTabs.Pages!;
            TabCountText.Text = $"Tab count: {pages.Count}";

            int liveCount = 0;
            for (int i = _weakRefs.Count - 1; i >= 0; i--)
            {
                if (_weakRefs[i].TryGetTarget(out _))
                    liveCount++;
                else
                    _weakRefs.RemoveAt(i);
            }

            LiveCountText.Text = $"Live instances: {liveCount} / {_weakRefs.Count} tracked";
            HeapText.Text = $"Heap: {GC.GetTotalMemory(false) / 1024:N0} KB";
            AllocText.Text = $"Total allocated: {GC.GetTotalAllocatedBytes() / 1024:N0} KB";
        }
    }
}
