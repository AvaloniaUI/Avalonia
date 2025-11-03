using System.Linq;
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

            Assert.Equal(3, manager.Notifications.Count());
        }

        [Fact]
        public void Show_And_Close_Notification()
        {
            WindowNotificationManager manager = new();

            manager.Show("Notification text");

            Assert.Equal(1, manager.Notifications.Count());

            manager.Close("Notification text");

            Assert.True(!manager.Notifications.Any(x => !x.IsClosing));
        }

        [Fact]
        public void Show_And_Close_All_Notifications()
        {
            WindowNotificationManager manager = new();

            manager.Show("Notification 1");
            manager.Show("Notification 2");

            Assert.Equal(2, manager.Notifications.Count());

            manager.CloseAll();

            Assert.True(!manager.Notifications.Any(x => !x.IsClosing));
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

            Assert.Equal(3, ((WindowNotificationManager)manager).Notifications.Count());
        }

        [Fact]
        public void Show_And_Close_Notification()
        {
            INotificationManager manager = new WindowNotificationManager();

            Notification notification = new()
            {
                Message = "Notification text"
            };

            manager.Show(notification);

            Assert.Equal(1, ((WindowNotificationManager)manager).Notifications.Count());

            manager.Close(notification);

            Assert.True(!((WindowNotificationManager)manager).Notifications.Any(x => !x.IsClosing));
        }

        [Fact]
        public void Show_And_Close_All_Notifications()
        {
            INotificationManager manager = new WindowNotificationManager();

            Notification notification1 = new()
            {
                Message = "Notification text"
            };

            Notification notification2 = new()
            {
                Message = "Notification text"
            };

            manager.Show(notification1);
            manager.Show(notification2);

            Assert.Equal(2, ((WindowNotificationManager)manager).Notifications.Count());

            manager.CloseAll();

            Assert.True(!((WindowNotificationManager)manager).Notifications.Any(x => !x.IsClosing));
        }
    }
}
