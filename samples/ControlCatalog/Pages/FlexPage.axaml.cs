using Avalonia.Controls;
using Avalonia.Interactivity;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class FlexPage : UserControl
    {
        public FlexPage()
        {
            InitializeComponent();

            DataContext = new FlexViewModel();
        }

        private void OnItemTapped(object? sender, RoutedEventArgs e)
        {
            if (sender is ListBoxItem control && DataContext is FlexViewModel vm && control.DataContext is FlexItemViewModel item)
            {
                if (vm.SelectedItem != null)
                {
                    vm.SelectedItem.IsSelected = false;
                }

                if (vm.SelectedItem == item)
                {
                    vm.SelectedItem = null;
                }
                else
                {
                    vm.SelectedItem = item;
                    vm.SelectedItem.IsSelected = true;
                }
            }
        }
    }
}
