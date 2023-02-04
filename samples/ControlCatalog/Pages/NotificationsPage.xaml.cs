using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class NotificationsPage : UserControl
    {
        private NotificationViewModel _viewModel;

        public NotificationsPage()
        {
            this.InitializeComponent();

            _viewModel = new NotificationViewModel();

            DataContext = _viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _viewModel.NotificationManager = new Avalonia.Controls.Notifications.WindowNotificationManager(TopLevel.GetTopLevel(this));
        }
    }
}
