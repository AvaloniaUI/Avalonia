using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class RefreshContainerPage : UserControl
    {
        private RefreshContainerViewModel _viewModel;

        public RefreshContainerPage()
        {
            InitializeComponent();

            _viewModel = new RefreshContainerViewModel();

            DataContext = _viewModel;
        }

        private async void RefreshContainerPage_RefreshRequested(object? sender, RefreshRequestedEventArgs e)
        {
            var deferral = e.GetDeferral();

            await _viewModel.AddToTop();

            deferral.Complete();
        }
    }
}
