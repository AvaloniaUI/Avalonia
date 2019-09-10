using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Avalonia.Notifications.Native
{
    [PublicAPI]
    public interface INativeNotificationManager : INotificationManager
    {
        /// <summary>
        /// Show a notification.
        /// </summary>
        /// <param name="notification">The notification to be displayed.</param>
        Task ShowAsync([NotNull] INotification notification);

        /// <summary>
        /// Replaces a notification with a new one
        /// </summary>
        /// <param name="old">The notification to be removed</param>
        /// <param name="new">The new notification</param>
        Task ReplaceAsync([NotNull] INotification old, [NotNull] INotification @new);

        /// <summary>
        /// Removes an already shown notification from the notification dashboard
        /// </summary>
        /// <param name="notification">A notification that was already shown to be removed (see <see cref="ShowAsync"/>)</param>
        Task CloseAsync([NotNull] INotification notification);

        /// <summary>
        /// Gets the servers capabilities<para/>
        /// Use this to verify which functionality can be used
        /// </summary>
        /// <remarks>https://developer.gnome.org/notification-spec/#id2825605</remarks>
        /// <returns>A string array with the servers capabilities</returns>
        [ItemNotNull]
        Task<string[]> GetCapabilitiesAsync();

        /// <summary>
        /// Gets the server information
        /// </summary>
        Task<ServerInfo> GetServerInfoAsync();

        Task<bool> IsAvailable();
    }
}
