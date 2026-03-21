using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageProgrammaticPage : UserControl
    {
        private readonly List<string> _log = new();

        public TabbedPageProgrammaticPage()
        {
            InitializeComponent();
        }

        private void OnSelectionChanged(object? sender, PageSelectionChangedEventArgs e)
        {
            UpdateCurrentLabel();

            var from = (e.PreviousPage as ContentPage)?.Header?.ToString() ?? "—";
            var to = (e.CurrentPage as ContentPage)?.Header?.ToString() ?? "—";
            _log.Insert(0, $"{from} \u2192 {to}");
            if (_log.Count > 6) _log.RemoveAt(_log.Count - 1);
            if (SelectionLog != null)
                SelectionLog.Text = string.Join("\n", _log);
        }

        private void UpdateCurrentLabel()
        {
            if (CurrentTabLabel == null || DemoTabs == null) return;
            var idx = DemoTabs.SelectedIndex;
            var name = (DemoTabs.SelectedPage as ContentPage)?.Header?.ToString() ?? "—";
            CurrentTabLabel.Text = $"Index {idx} | {name}";
        }

        private void OnJumpTo(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int index))
                DemoTabs.SelectedIndex = index;
        }

        private void OnPrevious(object? sender, RoutedEventArgs e)
        {
            if (DemoTabs.SelectedIndex > 0) DemoTabs.SelectedIndex--;
        }

        private void OnNext(object? sender, RoutedEventArgs e)
        {
            if (DemoTabs.SelectedIndex < 3) DemoTabs.SelectedIndex++;
        }
    }
}
