using Avalonia.Metadata;

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// Represents a notification manager that can be used to show notifications in a window or using
	/// the host operating system.
    /// </summary>
    [NotClientImplementable]
    public interface INotificationManager
    {
        /// <summary>
        /// Show a notification.
        /// </summary>
        /// <param name="notification">The notification to be displayed.</param>
        void Show(INotification notification);
    }
}
