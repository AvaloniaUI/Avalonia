using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CarouselPageEventsPage : UserControl
    {
        private readonly List<string> _log = new();

        public CarouselPageEventsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            var pageNames = new[] { "Home", "Explore", "Library", "Profile" };
            for (int i = 0; i < pageNames.Length; i++)
            {
                var name = pageNames[i];
                var page = new ContentPage
                {
                    Header = name,
                    Background = NavigationDemoHelper.GetPageBrush(i),
                    Content = new TextBlock
                    {
                        Text = $"{name}",
                        FontSize = 28,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    },
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Stretch
                };

                page.NavigatedTo += (_, args) =>
                    AppendLog($"NavigatedTo: {name} (from {(args.PreviousPage as ContentPage)?.Header ?? "—"})");
                page.NavigatedFrom += (_, args) =>
                    AppendLog($"NavigatedFrom: {name} (to {(args.DestinationPage as ContentPage)?.Header ?? "—"})");

                ((Avalonia.Collections.AvaloniaList<Page>)DemoCarousel.Pages!).Add(page);
            }

            DemoCarousel.SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(object? sender, PageSelectionChangedEventArgs e)
        {
            AppendLog($"SelectionChanged: {(e.PreviousPage as ContentPage)?.Header ?? "—"} → {(e.CurrentPage as ContentPage)?.Header ?? "—"}");
        }

        private void OnPrevious(object? sender, RoutedEventArgs e)
        {
            if (DemoCarousel.SelectedIndex > 0)
                DemoCarousel.SelectedIndex--;
        }

        private void OnNext(object? sender, RoutedEventArgs e)
        {
            var pageCount = ((AvaloniaList<Page>)DemoCarousel.Pages!).Count;
            if (DemoCarousel.SelectedIndex < pageCount - 1)
                DemoCarousel.SelectedIndex++;
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            DemoCarousel.SelectionChanged -= OnSelectionChanged;
        }

        private void OnClearLog(object? sender, RoutedEventArgs e)
        {
            _log.Clear();
            EventLog.Text = string.Empty;
        }

        private void AppendLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            _log.Add($"[{timestamp}] {message}");
            if (_log.Count > 50)
                _log.RemoveAt(0);
            EventLog.Text = string.Join(Environment.NewLine, _log);
            LogScrollViewer.ScrollToEnd();
        }
    }
}
