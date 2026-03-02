using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CommandBarDynamicOverflowPage : UserControl
    {
        public CommandBarDynamicOverflowPage()
        {
            InitializeComponent();
            ((INotifyCollectionChanged)DemoBar.OverflowItems).CollectionChanged += OnOverflowChanged;
            UpdateStatus();
        }

        private void OnWidthChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (BarContainer == null)
                return;
            var width = (int)WidthSlider.Value;
            BarContainer.Width = width;
            WidthLabel.Text = $"{width}";
        }

        private void OnDynamicOverflowChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoBar == null)
                return;
            DemoBar.IsDynamicOverflowEnabled = DynamicOverflowCheck.IsChecked == true;
        }

        private void OnOverflowChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            var total = DemoBar.PrimaryCommands.Count;
            var overflow = DemoBar.OverflowItems.Count;
            var visible = total - overflow;
            StatusText.Text = $"Showing {visible} of {total} commands, {overflow} in overflow";
        }
    }
}
