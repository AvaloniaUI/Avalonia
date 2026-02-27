using System;
using System.Collections.Generic;
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

        private readonly List<WeakReference<ContentPage>> _trackedPages = new();
        private int _totalCreated;
        private int _pageCounter;
        private double _previousHeapMB;
        private DispatcherTimer? _autoRefreshTimer;

        public NavigationPagePerformancePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            DemoNav.Pushed += OnStackChanged;
            DemoNav.Popped += OnStackChanged;
            DemoNav.PoppedToRoot += OnStackChanged;

            _previousHeapMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

            _pageCounter++;
            await DemoNav.PushAsync(BuildPage("Home", _pageCounter), null);
            LogOperation("Init", "Pushed root page");
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e) => StopAutoRefresh();

        private void OnStackChanged(object? sender, NavigationEventArgs e) => RefreshAll();

        private void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCounter++;
            var page = BuildPage($"Page {_pageCounter}", _pageCounter);
            DemoNav.Push(page);
            LogOperation("Push", $"Pushed \"{page.Header}\"");
        }

        private void OnPush5(object? sender, RoutedEventArgs e)
        {
            int first = _pageCounter + 1;
            for (int i = 0; i < 5; i++)
            {
                _pageCounter++;
                DemoNav.Push(BuildPage($"Page {_pageCounter}", _pageCounter));
            }
            LogOperation("Push ×5", $"Pushed pages {first}–{_pageCounter}");
        }

        private void OnPop(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.StackDepth > 1)
            {
                var header = DemoNav.CurrentPage?.Header?.ToString();
                DemoNav.Pop();
                LogOperation("Pop", $"Popped \"{header}\"");
            }
        }

        private async void OnPopToRoot(object? sender, RoutedEventArgs e)
        {
            if (DemoNav.StackDepth > 1)
            {
                int removed = DemoNav.StackDepth - 1;
                await DemoNav.PopToRootAsync();
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
            StackItemsPanel.Children.Clear();
            var stack   = DemoNav.NavigationStack;
            var current = DemoNav.CurrentPage;

            for (int i = stack.Count - 1; i >= 0; i--)
            {
                var page      = stack[i];
                bool isCurrent = ReferenceEquals(page, current);
                bool isRoot    = i == 0;

                var colorIdx  = i % PageBrushes.Length;
                var badge = new Border
                {
                    Width = 22, Height = 22,
                    CornerRadius = new Avalonia.CornerRadius(11),
                    Background = PageBrushes[colorIdx],
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = (i + 1).ToString(),
                        FontSize = 10, FontWeight = FontWeight.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    }
                };

                string? badgeText = isCurrent ? "current" : (isRoot ? "root" : null);
                var row = new DockPanel();
                row.Children.Add(badge);
                row.Children.Add(new TextBlock
                {
                    Text = page.Header?.ToString() ?? "(untitled)",
                    FontWeight = isCurrent ? FontWeight.SemiBold : FontWeight.Normal,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Avalonia.Thickness(6, 0, 0, 0),
                });
                if (badgeText != null)
                    row.Children.Add(new TextBlock
                    {
                        Text = badgeText,
                        FontSize = 10, Opacity = 0.5,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Avalonia.Thickness(4, 0, 0, 0),
                    });

                StackItemsPanel.Children.Add(new Border
                {
                    BorderBrush = new SolidColorBrush(isCurrent
                        ? Color.Parse("#0078D4") : Color.Parse("#CCCCCC")),
                    BorderThickness = new Avalonia.Thickness(isCurrent ? 2 : 1),
                    CornerRadius = new Avalonia.CornerRadius(6),
                    Padding = new Avalonia.Thickness(8, 6),
                    Child = row,
                });
            }
        }

        private void LogOperation(string action, string detail)
        {
            var heapMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
            LogPanel.Children.Add(new TextBlock
            {
                Text = $"{DateTime.Now:HH:mm:ss}  [{action}]  {detail}  — depth {DemoNav.StackDepth}, heap {heapMB:##0.0} MB",
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
