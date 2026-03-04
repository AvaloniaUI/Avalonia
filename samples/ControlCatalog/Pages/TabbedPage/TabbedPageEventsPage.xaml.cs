using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageEventsPage : UserControl
    {
        private readonly List<string> _log = new();

        public TabbedPageEventsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            var pagesList = (IList)DemoTabs.Pages!;
            var pageNames = new[] { "Home", "Explore", "Library", "Profile" };
            foreach (var name in pageNames)
            {
                var page = new ContentPage
                {
                    Header = name,
                    Content = new TextBlock
                    {
                        Text = $"{name} tab content",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        FontSize = 18,
                        Opacity = 0.7
                    }
                };

                page.NavigatedTo += (_, args) => AppendLog($"NavigatedTo: {name} (from {(args.PreviousPage as ContentPage)?.Header ?? "—"})");
                page.NavigatedFrom += (_, args) => AppendLog($"NavigatedFrom: {name} (to {(args.DestinationPage as ContentPage)?.Header ?? "—"})");

                pagesList.Add(page);
            }

            DemoTabs.SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(object? sender, PageSelectionChangedEventArgs e)
        {
            AppendLog($"SelectionChanged: {(e.PreviousPage as ContentPage)?.Header ?? "—"} → {(e.CurrentPage as ContentPage)?.Header ?? "—"}");
        }

        private void OnSelectNext(object? sender, RoutedEventArgs e)
        {
            int next = DemoTabs.SelectedIndex + 1;
            if (DemoTabs.Pages is IList pages && next < pages.Count)
                DemoTabs.SelectedIndex = next;
        }

        private void OnSelectPrevious(object? sender, RoutedEventArgs e)
        {
            int prev = DemoTabs.SelectedIndex - 1;
            if (prev >= 0)
                DemoTabs.SelectedIndex = prev;
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
