using System.ComponentModel;
using JetBrains.Annotations;

namespace Avalonia.Notifications.Native
{
    [PublicAPI]
    public enum NativeNotificationCloseReason : byte
    {
        [Description("Unknown reason")] Unknown = 0,

        [Description("The notification expired")]
        Expired = 1,

        [Description("The notification was dismissed by the user")]
        DismissedByUser = 2,

        [Description("The notification was closed by a call to CloseNotification")]
        ActivelyClosed = 3,

        [Description("Undefined/reserved reasons")]
        Reserved = 4
    }
}
