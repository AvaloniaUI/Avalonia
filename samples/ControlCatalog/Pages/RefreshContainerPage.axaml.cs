using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class RefreshContainerPage : ContentPage
    {
        private RefreshContainerViewModel _viewModel;

        public RefreshContainerPage()
        {
            InitializeComponent();

            RefreshButton.Click += RefreshButton_Click;

            _viewModel = new RefreshContainerViewModel();

            DataContext = _viewModel;
        }

        private void RefreshButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Refresh.RequestRefresh();
        }

        private async void RefreshContainerPage_RefreshRequested(object? sender, RefreshRequestedEventArgs e)
        {
            var deferral = e.GetDeferral();

            await _viewModel.AddToTop();

            deferral.Complete();
        }
    }
}
