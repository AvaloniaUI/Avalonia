using Tmds.DBus;

namespace Avalonia.FreeDesktop.Dbus
{
    public class FreeDesktopDbusInfo 
    {
        public static readonly ObjectPath NotificationsPath
            = new ObjectPath("/org/freedesktop/Notifications");

        public static readonly string NotificationsService 
            = "org.freedesktop.Notifications";
    }
}
