using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    /// <summary>
    /// Shared helpers for the performance-monitor demo pages
    /// (NavigationPage, TabbedPage, DrawerPage, ContentPage).
    /// </summary>
    internal sealed class NavigationPerformanceMonitorHelper
    {
        internal static readonly IBrush PositiveDeltaBrush = new SolidColorBrush(Color.Parse("#D32F2F"));
        internal static readonly IBrush NegativeDeltaBrush = new SolidColorBrush(Color.Parse("#388E3C"));
        internal static readonly IBrush ZeroDeltaBrush     = new SolidColorBrush(Color.Parse("#757575"));
        internal static readonly IBrush CurrentBorderBrush = new SolidColorBrush(Color.Parse("#0078D4"));
        internal static readonly IBrush DefaultBorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC"));

        private readonly List<WeakReference<Page>> _trackedPages = new();
        private double _previousHeapMB;
        private DispatcherTimer? _autoRefreshTimer;

        internal readonly Stopwatch OpStopwatch = new();
        internal int TotalCreated;

        /// <summary>
        /// Track a newly-created page via WeakReference and increment TotalCreated.
        /// </summary>
        internal void TrackPage(Page page)
        {
            TotalCreated++;
            _trackedPages.Add(new WeakReference<Page>(page));
        }

        /// <summary>
        /// Count live (not yet GC'd) tracked page instances.
        /// </summary>
        internal int CountLiveInstances()
        {
            int alive = 0;
            for (int i = _trackedPages.Count - 1; i >= 0; i--)
            {
                if (_trackedPages[i].TryGetTarget(out _))
                    alive++;
                else
                    _trackedPages.RemoveAt(i);
            }
            return alive;
        }

        /// <summary>
        /// Update heap and delta text blocks. Call from RefreshAll().
        /// </summary>
        internal void UpdateHeapDelta(TextBlock heapText, TextBlock deltaText)
        {
            var heapMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
            heapText.Text = $"Managed Heap: {heapMB:##0.0} MB";

            var delta = heapMB - _previousHeapMB;
            if (Math.Abs(delta) < 0.05)
            {
                deltaText.Text = "(no change)";
                deltaText.Foreground = ZeroDeltaBrush;
            }
            else
            {
                var sign = delta > 0 ? "+" : "";
                deltaText.Text = $"({sign}{delta:0.0} MB)";
                deltaText.Foreground = delta > 0 ? PositiveDeltaBrush : NegativeDeltaBrush;
            }
            _previousHeapMB = heapMB;
        }

        /// <summary>
        /// Initialize previous heap baseline.
        /// </summary>
        internal void InitHeap()
        {
            _previousHeapMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        }

        /// <summary>
        /// Stop the stopwatch and write elapsed ms to the given TextBlock.
        /// </summary>
        internal void StopMetrics(TextBlock lastOpText)
        {
            if (!OpStopwatch.IsRunning) return;
            OpStopwatch.Stop();
            lastOpText.Text = $"Last Op: {OpStopwatch.ElapsedMilliseconds} ms";
        }

        /// <summary>
        /// Force full GC, then invoke the refresh callback.
        /// </summary>
        internal void ForceGC(Action refresh)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            refresh();
        }

        /// <summary>
        /// Start a 2-second auto-refresh timer.
        /// </summary>
        internal void StartAutoRefresh(Action refresh)
        {
            if (_autoRefreshTimer != null) return;
            _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _autoRefreshTimer.Tick += (_, _) => refresh();
            _autoRefreshTimer.Start();
        }

        /// <summary>
        /// Stop the auto-refresh timer.
        /// </summary>
        internal void StopAutoRefresh()
        {
            _autoRefreshTimer?.Stop();
            _autoRefreshTimer = null;
        }

        /// <summary>
        /// Toggle auto-refresh based on a CheckBox.
        /// </summary>
        internal void OnAutoRefreshChanged(CheckBox check, Action refresh)
        {
            if (check.IsChecked == true)
                StartAutoRefresh(refresh);
            else
                StopAutoRefresh();
        }

        /// <summary>
        /// Append a timestamped log entry to a StackPanel inside a ScrollViewer.
        /// </summary>
        internal void LogOperation(string action, string detail,
            StackPanel logPanel, ScrollViewer logScroll, string? extraInfo = null)
        {
            var heapMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
            var timing = OpStopwatch.ElapsedMilliseconds;
            var extra = extraInfo != null ? $"  {extraInfo}," : "";

            logPanel.Children.Add(new TextBlock
            {
                Text = $"{DateTime.Now:HH:mm:ss}  [{action}]  {detail}  —{extra} heap {heapMB:##0.0} MB, {timing} ms",
                FontSize = 10,
                FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,monospace"),
                Padding = new Thickness(6, 2),
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            logScroll.ScrollToEnd();
        }

        /// <summary>
        /// Build a tracked ContentPage with a 50 KB dummy allocation.
        /// </summary>
        internal ContentPage BuildTrackedPage(string title, int index, int allocBytes = 51200)
        {
            var page = NavigationDemoHelper.MakePage(title,
                $"Stack position #{index}\nPush more pages ...", index);
            page.Tag = new byte[allocBytes];
            TrackPage(page);
            return page;
        }

        /// <summary>
        /// Create a reusable stack/history row (badge + title + label).
        /// </summary>
        internal static (Border Container, Border Badge, TextBlock IndexText,
            TextBlock TitleText, TextBlock BadgeText) CreateStackRow()
        {
            var indexText = new TextBlock
            {
                FontSize = 10, FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var badge = new Border
            {
                Width = 22, Height = 22,
                CornerRadius = new CornerRadius(11),
                VerticalAlignment = VerticalAlignment.Center,
                Child = indexText,
            };
            var titleText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(6, 0, 0, 0),
            };
            var badgeText = new TextBlock
            {
                FontSize = 10, Opacity = 0.5,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0),
                IsVisible = false,
            };

            var row = new DockPanel();
            row.Children.Add(badge);
            row.Children.Add(titleText);
            row.Children.Add(badgeText);

            var container = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 6),
                Child = row,
            };

            return (container, badge, indexText, titleText, badgeText);
        }

        /// <summary>
        /// Update a stack row with page data.
        /// </summary>
        internal static void UpdateStackRow(
            (Border Container, Border Badge, TextBlock IndexText,
                TextBlock TitleText, TextBlock BadgeText) row,
            int stackIndex, string title, bool isCurrent, bool isRoot)
        {
            row.Badge.Background = NavigationDemoHelper.GetPageBrush(stackIndex);
            row.IndexText.Text = (stackIndex + 1).ToString();
            row.TitleText.Text = title;
            row.TitleText.FontWeight = isCurrent ? FontWeight.SemiBold : FontWeight.Normal;

            string? label = isCurrent ? "current" : (isRoot ? "root" : null);
            row.BadgeText.Text = label ?? "";
            row.BadgeText.IsVisible = label != null;

            row.Container.BorderBrush = isCurrent ? CurrentBorderBrush : DefaultBorderBrush;
            row.Container.BorderThickness = new Thickness(isCurrent ? 2 : 1);
        }

        /// <summary>
        /// Sync a StackPanel of stack rows with data, growing/shrinking the row cache as needed.
        /// </summary>
        internal static void RefreshStackPanel(
            StackPanel panel,
            List<(Border Container, Border Badge, TextBlock IndexText,
                TextBlock TitleText, TextBlock BadgeText)> rowCache,
            IReadOnlyList<Page> stack, Page? currentPage)
        {
            int count = stack.Count;

            while (rowCache.Count < count)
                rowCache.Add(CreateStackRow());

            while (panel.Children.Count > count)
                panel.Children.RemoveAt(panel.Children.Count - 1);
            while (panel.Children.Count < count)
                panel.Children.Add(rowCache[panel.Children.Count].Container);

            for (int displayIdx = 0; displayIdx < count; displayIdx++)
            {
                int stackIdx = count - 1 - displayIdx;
                var page = stack[stackIdx];
                bool isCurrent = ReferenceEquals(page, currentPage);
                bool isRoot = stackIdx == 0;

                var row = rowCache[displayIdx];
                if (!ReferenceEquals(panel.Children[displayIdx], row.Container))
                    panel.Children[displayIdx] = row.Container;

                UpdateStackRow(row, stackIdx, page.Header?.ToString() ?? "(untitled)", isCurrent, isRoot);
            }
        }
    }
}
