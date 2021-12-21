using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Avalonia.Diagnostics.Views
{
    internal class ControlDetailsView : UserControl
    {
        public ControlDetailsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void PropertiesGrid_OnDoubleTapped(object sender, TappedEventArgs e)
        {
            if (sender is DataGrid grid && grid.DataContext is ControlDetailsViewModel controlDetails)
            {
                controlDetails.ApplySelectedProperty();
            }
            
        }
    }
}
