using System.Threading.Tasks;

namespace Avalonia.Notifications.Native
{
    public interface INativeNotificationManager : INotificationManager
    {
        /// <summary>
        /// Show a notification.
        /// </summary>
        /// <param name="notification">The notification to be displayed.</param>
        Task ShowAsync(INotification notification);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        Task CloseAsync(INotification notification);

        Task<string[]> GetCapabilitiesAsync();

        Task<ServerInfo> GetServerInfoAsync();

        Task<bool> IsAvailable();
    }
}
