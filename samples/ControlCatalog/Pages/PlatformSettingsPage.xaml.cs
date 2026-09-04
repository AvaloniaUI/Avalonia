using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
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
            _viewModel.Subscribe(this.GetPlatformSettings() ?? Application.Current?.PlatformSettings);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _viewModel.Unsubscribe();
            base.OnDetachedFromVisualTree(e);
        }
    }
}
