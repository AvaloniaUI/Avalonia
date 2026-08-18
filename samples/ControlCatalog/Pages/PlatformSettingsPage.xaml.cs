using Avalonia;
using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class PlatformSettingsPage : ContentPage
    {
        private readonly PlatformSettingsViewModel _viewModel = new();

        public PlatformSettingsPage()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _viewModel.Subscribe();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _viewModel.Unsubscribe();
            base.OnDetachedFromVisualTree(e);
        }
    }
}
