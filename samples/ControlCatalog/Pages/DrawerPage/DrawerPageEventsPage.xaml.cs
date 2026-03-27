using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class DrawerPageEventsPage : UserControl
    {
        private readonly Dictionary<string, ContentPage> _sectionPages;

        public DrawerPageEventsPage()
        {
            InitializeComponent();

            _sectionPages = new Dictionary<string, ContentPage>
            {
                ["Home"] = CreateSectionPage("Home"),
                ["Profile"] = CreateSectionPage("Profile"),
                ["Settings"] = CreateSectionPage("Settings"),
            };

            foreach (var (name, page) in _sectionPages)
            {
                var label = name;
                page.NavigatedTo   += (_, _) => Log($"{label}: NavigatedTo");
                page.NavigatedFrom += (_, _) => Log($"{label}: NavigatedFrom");
            }
        }

        private void OnControlLoaded(object? sender, RoutedEventArgs e)
        {
            DemoDrawer.Opened  += OnDrawerOpened;
            DemoDrawer.Closing += OnClosing;
            DemoDrawer.Closed  += OnDrawerClosed;
            // Set Content here so the initial NavigatedTo events fire
            // (VisualRoot is null in the constructor, which suppresses lifecycle events).
            DemoDrawer.Content = _sectionPages["Home"];
        }

        private void OnControlUnloaded(object? sender, RoutedEventArgs e)
        {
            DemoDrawer.Opened  -= OnDrawerOpened;
            DemoDrawer.Closing -= OnClosing;
            DemoDrawer.Closed  -= OnDrawerClosed;
        }

        private void OnDrawerOpened(object? sender, System.EventArgs e) => Log("Opened");
        private void OnDrawerClosed(object? sender, System.EventArgs e) => Log("Closed");

        private void OnToggle(object? sender, RoutedEventArgs e)
        {
            DemoDrawer.IsOpen = !DemoDrawer.IsOpen;
        }

        private void OnClosing(object? sender, DrawerClosingEventArgs e)
        {
            if (CancelCheck.IsChecked == true)
            {
                e.Cancel = true;
                CancelCheck.IsChecked = false;
                Log("Closing  \u2192  cancelled");
            }
            else
            {
                Log("Closing");
            }
        }

        private void OnSelectSection(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var section = btn.Tag?.ToString() ?? "Home";

            if (!_sectionPages.TryGetValue(section, out var page)) return;
            if (ReferenceEquals(DemoDrawer.Content, page))
            {
                DemoDrawer.IsOpen = false;
                return;
            }

            Log($"\u2192 {section}");
            DemoDrawer.Content = page;
            DemoDrawer.IsOpen = false;
        }

        private void OnClearLog(object? sender, RoutedEventArgs e)
        {
            EventLog.Text = string.Empty;
        }

        private void Log(string message)
        {
            EventLog.Text = $"{message}\n{EventLog.Text}";
        }

        private static ContentPage CreateSectionPage(string header) => new ContentPage
        {
            Header = header,
            Content = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = header,
                        FontSize = 24,
                        FontWeight = FontWeight.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    },
                    new TextBlock
                    {
                        Text = "Tap a drawer item to navigate.\nWatch the event log in the panel.",
                        TextWrapping = TextWrapping.Wrap,
                        Opacity = 0.6,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 13,
                    }
                }
            }
        };
    }
}
