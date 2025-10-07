using Avalonia.Controls.Notifications;
using Avalonia.UnitTests;
using Xunit;
using Notification = Avalonia.Controls.Notifications.Notification;

namespace Avalonia.Controls.UnitTests
{
    public class WindowNotificationManagerTests : ScopedTestBase
    {
        [Fact]
        public void Show_Notifications_With_Same_String()
        {
            WindowNotificationManager manager = new();

            manager.Show("Notification text");
            manager.Show("Notification text");
            manager.Show("Notification text");
        }
    }

    public class INotificationManagerTests : ScopedTestBase
    {
        [Fact]
        public void Show_Notifications_With_Same_Content()
        {
            INotificationManager manager = new WindowNotificationManager();

            Notification notification = new()
            {
                Message = "Notification text"
            };

            manager.Show(notification);
            manager.Show(notification);
            manager.Show(notification);
        }
    }
}
