using System.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageCollectionPage : UserControl
    {
        private int _counter = 3;

        private static readonly string[] Colors =
        {
            "#E53935", "#1E88E5", "#43A047", "#FB8C00",
            "#8E24AA", "#00ACC1", "#6D4C41", "#546E7A"
        };

        public TabbedPageCollectionPage()
        {
            InitializeComponent();
        }

        private void OnAddPage(object? sender, RoutedEventArgs e)
        {
            var index = _counter++;
            var color = Colors[index % Colors.Length];
            var page = new ContentPage
            {
                Header = $"Tab {index + 1}",
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(16),
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"Tab {index + 1}",
                            FontSize = 24,
                            FontWeight = Avalonia.Media.FontWeight.Bold,
                            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(color))
                        },
                        new TextBlock
                        {
                            Text = $"This page was added dynamically (#{index + 1}).",
                            Opacity = 0.7,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        }
                    }
                }
            };

            ((IList)DemoTabs.Pages!).Add(page);
            DemoTabs.SelectedIndex = ((IList)DemoTabs.Pages).Count - 1;
            UpdateStatus();
        }

        private void OnRemoveLast(object? sender, RoutedEventArgs e)
        {
            var pages = (IList)DemoTabs.Pages!;
            if (pages.Count > 0)
            {
                pages.RemoveAt(pages.Count - 1);
                UpdateStatus();
            }
        }

        private void OnClearAll(object? sender, RoutedEventArgs e)
        {
            var pages = (IList)DemoTabs.Pages!;
            while (pages.Count > 0)
                pages.RemoveAt(pages.Count - 1);
            _counter = 0;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            var count = ((IList)DemoTabs.Pages!).Count;
            StatusText.Text = $"{count} tab{(count != 1 ? "s" : "")}";
        }
    }
}
