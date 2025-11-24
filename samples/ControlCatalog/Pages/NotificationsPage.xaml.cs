using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class NotificationsPage : UserControl
    {
        private NotificationViewModel _viewModel;

        public NotificationsPage()
        {
            InitializeComponent();

            _viewModel = new NotificationViewModel();

            DataContext = _viewModel;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _viewModel.NotificationManager = new WindowNotificationManager(TopLevel.GetTopLevel(this)!);
        }

        public void NotificationOnClick()
        {
            ControlNotifications.Show("Notification clicked");
        }
    }
}
