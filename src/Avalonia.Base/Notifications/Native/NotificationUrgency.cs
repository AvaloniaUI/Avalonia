using JetBrains.Annotations;

namespace Avalonia.Notifications.Native
{
    [PublicAPI]
    public enum NotificationUrgency : byte
    {
        Low = 0,
        Normal = 1,
        Critical = 2
    }
}
