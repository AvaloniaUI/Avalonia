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
            if (SecondaryVisibleCheck.IsChecked != true)
            {
                DemoBar.SecondaryCommands?.Remove(DemoSecondaryCommand);
            }

            ((INotifyCollectionChanged)DemoBar.OverflowItems).CollectionChanged += OnOverflowChanged;
            ((INotifyCollectionChanged)DemoBar.VisiblePrimaryCommands).CollectionChanged += OnOverflowChanged;
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

        private void OnSecondaryVisibilityChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoBar?.SecondaryCommands == null || DemoSecondaryCommand == null)
                return;

            bool shouldInclude = SecondaryVisibleCheck.IsChecked == true;
            bool isIncluded = DemoBar.SecondaryCommands.Contains(DemoSecondaryCommand);

            if (shouldInclude && !isIncluded)
            {
                DemoBar.SecondaryCommands.Add(DemoSecondaryCommand);
            }
            else if (!shouldInclude && isIncluded)
            {
                DemoBar.SecondaryCommands.Remove(DemoSecondaryCommand);
            }

            UpdateStatus();
        }

        private void OnOverflowChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            int visiblePrimaryCommandCount = 0;
            int visiblePrimarySeparatorCount = 0;
            foreach (var item in DemoBar.VisiblePrimaryCommands)
            {
                if (item is CommandBarSeparator)
                    visiblePrimarySeparatorCount++;
                else
                    visiblePrimaryCommandCount++;
            }

            int overflowCommandCount = 0;
            int overflowSeparatorCount = 0;
            bool hasSyntheticOverflowDivider = false;
            foreach (var item in DemoBar.OverflowItems)
            {
                if (item is CommandBarSeparator separator)
                {
                    overflowSeparatorCount++;
                    if (!DemoBar.PrimaryCommands.Contains(separator) &&
                        !DemoBar.SecondaryCommands.Contains(separator))
                    {
                        hasSyntheticOverflowDivider = true;
                    }
                }
                else
                {
                    overflowCommandCount++;
                }
            }

            StatusText.Text =
                $"Visible primary: {visiblePrimaryCommandCount} commands, {visiblePrimarySeparatorCount} separators\n" +
                $"Overflow items: {overflowCommandCount} commands, {overflowSeparatorCount} separators\n" +
                $"Synthetic overflow divider: {(hasSyntheticOverflowDivider ? "present" : "absent")}";
        }
    }
}
