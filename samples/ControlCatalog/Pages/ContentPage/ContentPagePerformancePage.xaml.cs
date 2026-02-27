using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public partial class ContentPagePerformancePage : UserControl
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

        private static readonly int[] AllocationSizes = [51_200, 512_000, 2_097_152];

        private readonly List<WeakReference<Page>> _trackedPages = new();
        private int _totalCreated;
        private int _pageCounter;
        private double _previousHeapMB;
        private DispatcherTimer? _autoRefreshTimer;

        public ContentPagePerformancePage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            NavPage.Pushed       += OnStackChanged;
            NavPage.Popped       += OnStackChanged;
            NavPage.PoppedToRoot += OnStackChanged;

            _previousHeapMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

            _pageCounter++;
            NavPage.Push(BuildPage("Home", _pageCounter));
            LogOperation("Init", "Pushed root page");
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            StopAutoRefresh();
        }

        private void OnStackChanged(object? sender, NavigationEventArgs e) => RefreshAll();

        private void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCounter++;
            var page = BuildPage($"Page {_pageCounter}", _pageCounter);
            NavPage.Push(page);
            LogOperation("Push", $"Pushed \"{page.Header}\"");
        }

        private void OnPush5(object? sender, RoutedEventArgs e)
        {
            int first = _pageCounter + 1;
            for (int i = 0; i < 5; i++)
            {
                _pageCounter++;
                NavPage.Push(BuildPage($"Page {_pageCounter}", _pageCounter));
            }
            LogOperation("Push ×5", $"Pushed pages {first}–{_pageCounter}");
        }

        private async void OnPop(object? sender, RoutedEventArgs e)
        {
            if (NavPage.StackDepth > 1)
            {
                var popped = NavPage.CurrentPage;
                await NavPage.PopAsync();
                LogOperation("Pop", $"Popped \"{popped?.Header}\"");
            }
        }

        private async void OnPopToRoot(object? sender, RoutedEventArgs e)
        {
            if (NavPage.StackDepth > 1)
            {
                int removed = NavPage.StackDepth - 1;
                await NavPage.PopToRootAsync();
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
                if (_trackedPages[i].TryGetTarget(out _))
                    alive++;
                else
                    _trackedPages.RemoveAt(i);
            }
            return alive;
        }

        private void RefreshAll()
        {
            StackDepthText.Text    = $"Stack Depth: {NavPage.StackDepth}";
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
            StackVisPanel.Children.Clear();

            var stack   = NavPage.NavigationStack;
            var current = NavPage.CurrentPage;

            for (int i = stack.Count - 1; i >= 0; i--)
            {
                var page   = stack[i];
                var isRoot = i == 0;
                StackVisPanel.Children.Add(
                    BuildStackEntry(page, i + 1, ReferenceEquals(page, current), isRoot));
            }
        }

        private static Border BuildStackEntry(Page page, int position, bool isCurrent, bool isRoot)
        {
            var colorIdx = (position - 1) % PageBrushes.Length;

            var badge = new Border
            {
                Width = 24, Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = PageBrushes[colorIdx],
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = position.ToString(),
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                }
            };

            var title = new TextBlock
            {
                Text = page.Header?.ToString() ?? "(untitled)",
                FontWeight = isCurrent ? FontWeight.SemiBold : FontWeight.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(6, 0, 0, 0),
            };

            string? badgeText = isCurrent ? "current" : (isRoot ? "root" : null);
            var label = new TextBlock
            {
                Text = badgeText,
                FontSize = 10,
                Opacity = 0.5,
                VerticalAlignment = VerticalAlignment.Center,
                IsVisible = badgeText != null,
                Margin = new Thickness(4, 0, 0, 0),
            };

            var row = new DockPanel { LastChildFill = true };
            row.Children.Add(badge);
            row.Children.Add(title);
            row.Children.Add(label);

            return new Border
            {
                BorderBrush = new SolidColorBrush(isCurrent
                    ? Color.Parse("#0078D4")
                    : Color.Parse("#CCCCCC")),
                BorderThickness = new Thickness(isCurrent ? 2 : 1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 6),
                Child = row,
            };
        }

        private void LogOperation(string action, string detail)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var heapMB    = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

            LogPanel.Children.Add(new TextBlock
            {
                Text = $"{timestamp}  [{action}]  {detail}  — depth {NavPage.StackDepth}, heap {heapMB:##0.0} MB",
                FontSize = 11,
                FontFamily = new FontFamily("Cascadia Mono, Consolas, Menlo, monospace"),
                Padding = new Thickness(6, 2),
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            LogScrollViewer.ScrollToEnd();
        }

        private ContentPage BuildPage(string title, int index)
        {
            var colorIdx    = (index - 1) % PageBrushes.Length;
            var weightIndex = WeightCombo.SelectedIndex >= 0 ? WeightCombo.SelectedIndex : 0;
            var allocBytes  = AllocationSizes[Math.Clamp(weightIndex, 0, AllocationSizes.Length - 1)];
            var weightLabel = weightIndex switch { 1 => "~500 KB", 2 => "~2 MB", _ => "~50 KB" };

            var page = new ContentPage
            {
                Header     = title,
                Background = PageBrushes[colorIdx],
                Content    = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
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
                            Text = $"Stack position #{index}  ·  Weight: {weightLabel}",
                            FontSize = 14,
                            Opacity = 0.6,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = "Pop this page and force GC to see the heap drop by the weight shown above. " +
                                   "Live Instances decreases once the GC finalizes the page.",
                            FontSize = 12,
                            Opacity = 0.5,
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 400,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 16, 0, 0),
                        },
                    },
                },
                Tag = new byte[allocBytes],
            };

            _totalCreated++;
            _trackedPages.Add(new WeakReference<Page>(page));
            return page;
        }
    }
}
