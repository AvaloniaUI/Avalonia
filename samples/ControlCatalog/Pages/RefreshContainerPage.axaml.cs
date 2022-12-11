using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class RefreshContainerPage : UserControl
    {
        private RefreshContainerViewModel _viewModel;

        public RefreshContainerPage()
        {
            this.InitializeComponent();

            _viewModel = new RefreshContainerViewModel();

            DataContext = _viewModel;
        }

        private async void RefreshContainerPage_RefreshRequested(object? sender, RefreshRequestedEventArgs e)
        {
            var deferral = e.GetDeferral();

            await _viewModel.AddToTop();

            deferral.Complete();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
