using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class ContentPagePerformancePage : UserControl
    {
        private static readonly int[] AllocationSizes = [51_200, 512_000, 2_097_152];

        private readonly NavigationPerformanceMonitorHelper _perf = new();
        private int _pageCounter;

        private readonly List<(Border Container, Border Badge, TextBlock IndexText,
            TextBlock TitleText, TextBlock BadgeText)> _stackRowCache = new();

        public ContentPagePerformancePage()
        {
            InitializeComponent();
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            NavPage.Pushed       += OnStackChanged;
            NavPage.Popped       += OnStackChanged;
            NavPage.PoppedToRoot += OnStackChanged;

            _perf.InitHeap();
            _pageCounter++;
            await NavPage.PushAsync(BuildPage("Home", _pageCounter));
            Log("Init", "Pushed root page");
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            _perf.StopAutoRefresh();
        }

        private void OnStackChanged(object? sender, NavigationEventArgs e) => RefreshAll();

        private async void OnPush(object? sender, RoutedEventArgs e)
        {
            _pageCounter++;
            var page = BuildPage($"Page {_pageCounter}", _pageCounter);
            await NavPage.PushAsync(page);
            Log("Push", $"Pushed \"{page.Header}\"");
        }

        private async void OnPush5(object? sender, RoutedEventArgs e)
        {
            int first = _pageCounter + 1;
            for (int i = 0; i < 5; i++)
            {
                _pageCounter++;
                await NavPage.PushAsync(BuildPage($"Page {_pageCounter}", _pageCounter));
            }
            Log("Push ×5", $"Pushed pages {first}–{_pageCounter}");
        }

        private async void OnPop(object? sender, RoutedEventArgs e)
        {
            if (NavPage.StackDepth > 1)
            {
                var popped = NavPage.CurrentPage;
                await NavPage.PopAsync();
                Log("Pop", $"Popped \"{popped?.Header}\"");
            }
        }

        private async void OnPopToRoot(object? sender, RoutedEventArgs e)
        {
            if (NavPage.StackDepth > 1)
            {
                int removed = NavPage.StackDepth - 1;
                await NavPage.PopToRootAsync();
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
            StackDepthText.Text    = $"Stack Depth: {NavPage.StackDepth}";
            LiveInstancesText.Text = $"Live Page Instances: {_perf.CountLiveInstances()}";
            TotalCreatedText.Text  = $"Total Pages Created: {_perf.TotalCreated}";
            _perf.UpdateHeapDelta(ManagedMemoryText, MemoryDeltaText);

            NavigationPerformanceMonitorHelper.RefreshStackPanel(
                StackVisPanel, _stackRowCache,
                NavPage.NavigationStack, NavPage.CurrentPage);
        }

        private void Log(string action, string detail) =>
            _perf.LogOperation(action, detail, LogPanel, LogScrollViewer,
                $"depth {NavPage.StackDepth}");

        private ContentPage BuildPage(string title, int index)
        {
            var weightIndex = WeightCombo.SelectedIndex >= 0 ? WeightCombo.SelectedIndex : 0;
            var allocBytes  = AllocationSizes[Math.Clamp(weightIndex, 0, AllocationSizes.Length - 1)];
            var weightLabel = weightIndex switch { 1 => "~500 KB", 2 => "~2 MB", _ => "~50 KB" };

            var page = NavigationDemoHelper.MakePage(title, $"Stack position #{index}  ·  Weight: {weightLabel}\n\n" +
                "Pop this page and force GC to see the heap drop by the weight shown above. " +
                "Live Instances decreases once the GC finalizes the page.", index);
            page.Tag = new byte[allocBytes];
            _perf.TrackPage(page);
            return page;
        }
    }
}
