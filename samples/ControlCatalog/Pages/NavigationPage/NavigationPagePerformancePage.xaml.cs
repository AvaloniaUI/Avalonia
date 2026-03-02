using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public partial class NavigationPagePerformancePage : UserControl
    {
        private static readonly IBrush[] PageBrushes =
        [
            new SolidColorBrush(Color.Parse("#E3F2FD")),
            new SolidColorBrush(Color.Parse("#E8F5E9")),
            new SolidColorBrush(Color.Parse("#FFF3E0")),
            new SolidColorBrush(Color.Parse("#FCE4EC")),
            new SolidColorBrush(Color.Parse("#F3E5F5")),
            new SolidColorBrush(Color.Parse("#E0F7FA")),
        ];

        private static readonly IBrush PositiveDeltaBrush = new SolidColorBrush(Color.Parse("#D32F2F"));
        private static readonly IBrush NegativeDeltaBrush = new SolidColorBrush(Color.Parse("#388E3C"));
        private static readonly IBrush ZeroDeltaBrush     = new SolidColorBrush(Color.Parse("#757575"));
        private static readonly IBrush CurrentBorderBrush = new SolidColorBrush(Color.Parse("#0078D4"));
        private static readonly IBrush DefaultBorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC"));

        private readonly List<WeakReference<ContentPage>> _trackedPages = new();
        private int _totalCreated;
        private int _pageCounter;
        private double _previousHeapMB;
        private DispatcherTimer? _autoRefreshTimer;

        private readonly Stopwatch _opStopwatch = new();

        // Cached stack row elements (avoid per-refresh allocations)
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

            _previousHeapMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

            _opStopwatch.Restart();
            _pageCounter++;
            await DemoNav.PushAsync(BuildPage("Home", _pageCounter), null);
            _opStopwatch.Stop();
            LogOperation("Init", "Pushed root page");
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            StopAutoRefresh();
        }

        private void OnStackChanged(object? sender, NavigationEventArgs e) => RefreshAll();

        private void StopMetrics()
        {
            if (!_opStopwatch.IsRunning) return;
            _opStopwatch.Stop();
            LastOpTimeText.Text = $"Last Op: {_opStopwatch.ElapsedMilliseconds} ms";
        }

        private void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCounter++;
            var page = BuildPage($"Page {_pageCounter}", _pageCounter);
            _opStopwatch.Restart();
            DemoNav.Push(page);
            StopMetrics();
            LogOperation("Push", $"Pushed \"{page.Header}\"");
        }

        private void OnPush5(object? sender, RoutedEventArgs e)
        {
            int first = _pageCounter + 1;
            _opStopwatch.Restart();
            for (int i = 0; i < 5; i++)
            {
                _pageCounter++;
                DemoNav.Push(BuildPage($"Page {_pageCounter}", _pageCounter));
            }
            StopMetrics();
            LogOperation("Push ×5", $"Pushed pages {first}–{_pageCounter}");
        }

        private void OnPop(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.StackDepth > 1)
            {
                var header = DemoNav.CurrentPage?.Header?.ToString();
                _opStopwatch.Restart();
                DemoNav.Pop();
                StopMetrics();
                LogOperation("Pop", $"Popped \"{header}\"");
            }
        }

        private async void OnPopToRoot(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.StackDepth > 1)
            {
                int removed = DemoNav.StackDepth - 1;
                _opStopwatch.Restart();
                await DemoNav.PopToRootAsync();
                StopMetrics();
                LogOperation("PopToRoot", $"Removed {removed} page(s)");
            }
        }

        private void OnForceGC(object? sender, RoutedEventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            RefreshAll();
            LogOperation("GC", "Forced full garbage collection");
        }

        private void OnClearLog(object? sender, RoutedEventArgs e) => LogPanel.Children.Clear();

        private void OnAutoRefreshChanged(object? sender, RoutedEventArgs e)
        {
            if (AutoRefreshCheck.IsChecked == true)
                StartAutoRefresh();
            else
                StopAutoRefresh();
        }

        private void StartAutoRefresh()
        {
            if (_autoRefreshTimer != null)
                return;
            _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _autoRefreshTimer.Tick += (_, _) => RefreshAll();
            _autoRefreshTimer.Start();
        }

        private void StopAutoRefresh()
        {
            _autoRefreshTimer?.Stop();
            _autoRefreshTimer = null;
        }

        private int CountLiveInstances()
        {
            int alive = 0;
            for (int i = _trackedPages.Count - 1; i >= 0; i--)
            {
                if (_trackedPages[i].TryGetTarget(out _)) alive++;
                else _trackedPages.RemoveAt(i);
            }
            return alive;
        }

        private void RefreshAll()
        {
            StackDepthText.Text    = $"Stack Depth: {DemoNav.StackDepth}";
            LiveInstancesText.Text = $"Live Page Instances: {CountLiveInstances()}";
            TotalCreatedText.Text  = $"Total Pages Created: {_totalCreated}";

            var heapMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
            ManagedMemoryText.Text = $"Managed Heap: {heapMB:##0.0} MB";

            var delta = heapMB - _previousHeapMB;
            if (Math.Abs(delta) < 0.05)
            {
                MemoryDeltaText.Text       = "(no change)";
                MemoryDeltaText.Foreground = ZeroDeltaBrush;
            }
            else
            {
                var sign = delta > 0 ? "+" : "";
                MemoryDeltaText.Text       = $"({sign}{delta:0.0} MB)";
                MemoryDeltaText.Foreground = delta > 0 ? PositiveDeltaBrush : NegativeDeltaBrush;
            }
            _previousHeapMB = heapMB;

            RefreshStack();
        }

        private void RefreshStack()
        {
            var stack   = DemoNav.NavigationStack;
            var current = DemoNav.CurrentPage;
            int count   = stack.Count;

            // Grow cache if needed
            while (_stackRowCache.Count < count)
                _stackRowCache.Add(CreateStackRow());

            // Sync panel children count (remove excess, add missing)
            while (StackItemsPanel.Children.Count > count)
                StackItemsPanel.Children.RemoveAt(StackItemsPanel.Children.Count - 1);
            while (StackItemsPanel.Children.Count < count)
                StackItemsPanel.Children.Add(_stackRowCache[StackItemsPanel.Children.Count].Container);

            // Update each row (displayed in reverse: top of stack first)
            for (int displayIdx = 0; displayIdx < count; displayIdx++)
            {
                int stackIdx = count - 1 - displayIdx;
                var page = stack[stackIdx];
                bool isCurrent = ReferenceEquals(page, current);
                bool isRoot = stackIdx == 0;

                var (container, badge, indexText, titleText, badgeText) = _stackRowCache[displayIdx];

                // Ensure correct container is at this position
                if (!ReferenceEquals(StackItemsPanel.Children[displayIdx], container))
                    StackItemsPanel.Children[displayIdx] = container;

                badge.Background = PageBrushes[stackIdx % PageBrushes.Length];
                indexText.Text = (stackIdx + 1).ToString();
                titleText.Text = page.Header?.ToString() ?? "(untitled)";
                titleText.FontWeight = isCurrent ? FontWeight.SemiBold : FontWeight.Normal;

                string? label = isCurrent ? "current" : (isRoot ? "root" : null);
                badgeText.Text = label ?? "";
                badgeText.IsVisible = label != null;

                container.BorderBrush = isCurrent ? CurrentBorderBrush : DefaultBorderBrush;
                container.BorderThickness = new Avalonia.Thickness(isCurrent ? 2 : 1);
            }
        }

        private static (Border Container, Border Badge, TextBlock IndexText,
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
                CornerRadius = new Avalonia.CornerRadius(11),
                VerticalAlignment = VerticalAlignment.Center,
                Child = indexText,
            };
            var titleText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Avalonia.Thickness(6, 0, 0, 0),
            };
            var badgeText = new TextBlock
            {
                FontSize = 10, Opacity = 0.5,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(4, 0, 0, 0),
                IsVisible = false,
            };

            var row = new DockPanel();
            row.Children.Add(badge);
            row.Children.Add(titleText);
            row.Children.Add(badgeText);

            var container = new Border
            {
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(8, 6),
                Child = row,
            };

            return (container, badge, indexText, titleText, badgeText);
        }

        private void LogOperation(string action, string detail)
        {
            var heapMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
            var timing = _opStopwatch.ElapsedMilliseconds;
            LogPanel.Children.Add(new TextBlock
            {
                Text = $"{DateTime.Now:HH:mm:ss}  [{action}]  {detail}  — depth {DemoNav.StackDepth}, heap {heapMB:##0.0} MB, {timing} ms",
                FontSize = 10,
                FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,monospace"),
                Padding = new Avalonia.Thickness(6, 2),
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            LogScrollViewer.ScrollToEnd();
        }

        private ContentPage BuildPage(string title, int index)
        {
            var page = new ContentPage
            {
                Header = title,
                Background = PageBrushes[(index - 1) % PageBrushes.Length],
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
                            FontSize = 24, FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = $"Stack position #{index}",
                            FontSize = 13, Opacity = 0.6,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = "Push pages and watch the heap grow. Pop them and force GC to see memory reclaimed.",
                            FontSize = 11, Opacity = 0.5,
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 320,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Avalonia.Thickness(0, 12, 0, 0),
                        },
                    },
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                // Allocate ~50 KB so memory deltas are visible
                Tag = new byte[51200],
            };

            _totalCreated++;
            _trackedPages.Add(new WeakReference<ContentPage>(page));
            return page;
        }
    }
}
