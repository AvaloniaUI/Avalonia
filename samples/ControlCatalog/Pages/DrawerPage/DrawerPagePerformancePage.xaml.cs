using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public partial class DrawerPagePerformancePage : UserControl
    {
        private static readonly IBrush[] _pageBrushes =
        {
            new SolidColorBrush(Color.Parse("#E3F2FD")),
            new SolidColorBrush(Color.Parse("#E8F5E9")),
            new SolidColorBrush(Color.Parse("#FFF3E0")),
            new SolidColorBrush(Color.Parse("#FCE4EC")),
            new SolidColorBrush(Color.Parse("#F3E5F5")),
            new SolidColorBrush(Color.Parse("#E0F7FA")),
        };

        private static readonly IBrush _positiveDeltaBrush = new SolidColorBrush(Color.Parse("#D32F2F"));
        private static readonly IBrush _negativeDeltaBrush = new SolidColorBrush(Color.Parse("#388E3C"));
        private static readonly IBrush _zeroDeltaBrush = new SolidColorBrush(Color.Parse("#757575"));
        private static readonly IBrush _currentBorderBrush = new SolidColorBrush(Color.Parse("#0078D4"));
        private static readonly IBrush _defaultBorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC"));

        private readonly List<WeakReference<Page>> _trackedPages = new();
        private readonly List<string> _detailHistory = new();
        private int _totalCreated;
        private int _swapCounter;
        private int _pageCounter;
        private double _previousHeapMB;
        private DispatcherTimer? _autoRefreshTimer;

        private readonly Stopwatch _opStopwatch = new();

        private readonly List<(Border Container, Border Badge, TextBlock IndexText,
            TextBlock TitleText, TextBlock BadgeText)> _historyRowCache = new();

        public DrawerPagePerformancePage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            _previousHeapMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

            _opStopwatch.Restart();
            _totalCreated++;
            _trackedPages.Add(new WeakReference<Page>(DetailPage));
            _detailHistory.Add("Home");
            _opStopwatch.Stop();

            LogOperation("Init", "Initial detail page: Home");
            RefreshAll();
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            StopAutoRefresh();
        }

        private void StopMetrics()
        {
            if (!_opStopwatch.IsRunning) return;
            _opStopwatch.Stop();
            LastOpTimeText.Text = $"Last Op: {_opStopwatch.ElapsedMilliseconds} ms";
        }

        private void OnMenuItemClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            var item = button.Tag?.ToString() ?? "Home";

            _opStopwatch.Restart();
            SwapDetailTo(item);
            DrawerPageControl.IsOpen = false;
            StopMetrics();
        }

        private void OnSwapDetail(object? sender, RoutedEventArgs e)
        {
            _pageCounter++;
            _opStopwatch.Restart();
            SwapDetailTo($"Page {_pageCounter}");
            StopMetrics();
        }

        private void OnSwap5(object? sender, RoutedEventArgs e)
        {
            _opStopwatch.Restart();
            for (int i = 0; i < 5; i++)
            {
                _pageCounter++;
                SwapDetailTo($"Page {_pageCounter}");
            }
            StopMetrics();
        }

        private void OnToggleDrawer(object? sender, RoutedEventArgs e)
        {
            _opStopwatch.Restart();
            DrawerPageControl.IsOpen = !DrawerPageControl.IsOpen;
            StopMetrics();
        }

        private void OnForceGC(object? sender, RoutedEventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            RefreshAll();
            LogOperation("GC", "Forced full garbage collection");
        }

        private void OnClearLog(object? sender, RoutedEventArgs e)
        {
            LogPanel.Children.Clear();
        }

        private void OnAutoRefreshChanged(object? sender, RoutedEventArgs e)
        {
            if (AutoRefreshCheck.IsChecked == true)
                StartAutoRefresh();
            else
                StopAutoRefresh();
        }

        private void StartAutoRefresh()
        {
            if (_autoRefreshTimer != null) return;
            _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _autoRefreshTimer.Tick += (_, _) => RefreshAll();
            _autoRefreshTimer.Start();
        }

        private void StopAutoRefresh()
        {
            _autoRefreshTimer?.Stop();
            _autoRefreshTimer = null;
        }

        private void SwapDetailTo(string title)
        {
            _swapCounter++;
            var colorIdx = _swapCounter % _pageBrushes.Length;

            var newDetail = new ContentPage
            {
                Header = title,
                Background = _pageBrushes[colorIdx],
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
                            FontSize = 28,
                            FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = $"Detail swap #{_swapCounter}",
                            FontSize = 14,
                            Opacity = 0.6,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = "Each detail page allocates ~50 KB to make memory changes visible. " +
                                   "Swap pages and watch the heap grow. The old detail page is replaced " +
                                   "but not freed until the garbage collector runs.",
                            FontSize = 12,
                            Opacity = 0.5,
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 400,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 16, 0, 0),
                        },
                    },
                },
                Tag = new byte[51200],
            };

            _totalCreated++;
            _trackedPages.Add(new WeakReference<Page>(newDetail));
            _detailHistory.Add(title);

            DrawerPageControl.Content = newDetail;

            LogOperation("Swap", $"Detail \u2192 \"{title}\"");
            RefreshAll();
        }

        private int CountLiveInstances()
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

        private void RefreshAll()
        {
            var currentDetail = DrawerPageControl.Content as Page;
            CurrentDetailText.Text = $"Current Detail: {currentDetail?.Header ?? "\u2014"}";
            SwapCountText.Text = $"Detail Swaps: {_swapCounter}";
            LiveInstancesText.Text = $"Live Page Instances: {CountLiveInstances()}";
            TotalCreatedText.Text = $"Total Pages Created: {_totalCreated}";

            var heapMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
            ManagedMemoryText.Text = $"Managed Heap: {heapMB:##0.0} MB";

            var delta = heapMB - _previousHeapMB;
            if (Math.Abs(delta) < 0.05)
            {
                MemoryDeltaText.Text = "(no change)";
                MemoryDeltaText.Foreground = _zeroDeltaBrush;
            }
            else
            {
                var sign = delta > 0 ? "+" : "";
                MemoryDeltaText.Text = $"({sign}{delta:0.0} MB)";
                MemoryDeltaText.Foreground = delta > 0 ? _positiveDeltaBrush : _negativeDeltaBrush;
            }
            _previousHeapMB = heapMB;

            RefreshHistory();
        }

        private void RefreshHistory()
        {
            int start = Math.Max(0, _detailHistory.Count - 10);
            int count = _detailHistory.Count - start;

            while (_historyRowCache.Count < count)
                _historyRowCache.Add(CreateHistoryRow());

            while (HistoryPanel.Children.Count > count)
                HistoryPanel.Children.RemoveAt(HistoryPanel.Children.Count - 1);
            while (HistoryPanel.Children.Count < count)
                HistoryPanel.Children.Add(_historyRowCache[HistoryPanel.Children.Count].Container);

            for (int displayIdx = 0; displayIdx < count; displayIdx++)
            {
                int historyIdx = _detailHistory.Count - 1 - displayIdx;
                bool isCurrent = historyIdx == _detailHistory.Count - 1;
                int colorIdx = historyIdx % _pageBrushes.Length;

                var (container, badge, indexText, titleText, badgeText) = _historyRowCache[displayIdx];

                if (!ReferenceEquals(HistoryPanel.Children[displayIdx], container))
                    HistoryPanel.Children[displayIdx] = container;

                badge.Background = _pageBrushes[colorIdx];
                indexText.Text = (historyIdx + 1).ToString();
                titleText.Text = _detailHistory[historyIdx];
                titleText.FontWeight = isCurrent ? FontWeight.SemiBold : FontWeight.Normal;

                badgeText.Text = isCurrent ? "current" : "";
                badgeText.IsVisible = isCurrent;

                container.BorderBrush = isCurrent ? _currentBorderBrush : _defaultBorderBrush;
                container.BorderThickness = new Thickness(isCurrent ? 2 : 1);
            }
        }

        private static (Border Container, Border Badge, TextBlock IndexText,
            TextBlock TitleText, TextBlock BadgeText) CreateHistoryRow()
        {
            var indexText = new TextBlock
            {
                FontSize = 11, FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var badge = new Border
            {
                Width = 24, Height = 24,
                CornerRadius = new CornerRadius(12),
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
                IsVisible = false,
                Margin = new Thickness(4, 0, 0, 0),
            };

            var row = new DockPanel { LastChildFill = true };
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

        private void LogOperation(string action, string detail)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var heapMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
            var timing = _opStopwatch.ElapsedMilliseconds;

            var entry = new TextBlock
            {
                Text = $"{timestamp}  [{action}]  {detail}  \u2014 heap {heapMB:##0.0} MB, {timing} ms",
                FontSize = 11,
                FontFamily = new FontFamily("Cascadia Mono, Consolas, Menlo, monospace"),
                Padding = new Thickness(6, 2),
                TextTrimming = TextTrimming.CharacterEllipsis,
            };

            LogPanel.Children.Add(entry);
            LogScrollViewer.ScrollToEnd();
        }
    }
}
