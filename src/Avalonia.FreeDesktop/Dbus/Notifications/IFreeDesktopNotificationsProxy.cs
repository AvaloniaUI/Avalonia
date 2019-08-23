using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]

namespace Avalonia.FreeDesktop.Dbus.Notifications
{
    /// <seealso>https://people.gnome.org/~mccann/docs/notification-spec/notification-spec-latest.html</seealso>
    /// <summary>
    /// Interface for notifications
    /// </summary>
    /// <remarks>
    /// Do not make changes to the methods signature as they are merely a 
    /// </remarks>
    [DBusInterface("org.freedesktop.Notifications")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    internal interface IFreeDesktopNotificationsProxy : IDBusObject
    {
        /// <summary>
        /// Sends a notification to the notification server.
        /// </summary>
        /// <param name="appName">
        /// The optional name of the application sending the notification. Can be <see cref="System.String.Empty"/>.
        /// </param>
        /// <param name="replacesId">
        /// The optional notification ID that this notification replaces.<para/>
        /// The server must automatically (ie with no flicker or other visual cues) replace the given notification with this one.<para/>
        /// This allows clients to effectively modify the notification while it's active.
        /// A value of value of <code>0</code> means that this notification won't replace any existing notifications.
        /// </param>
        /// <param name="appIcon">
        /// The optional program icon of the calling application.
        /// Can be an empty string, indicating no icon.<para/>
        /// See Icons and Images (https://people.gnome.org/~mccann/docs/notification-spec/notification-spec-latest.html#icons-and-images)
        /// </param>
        /// <param name="summary">The summary text briefly describing the notification.</param>
        /// <param name="body">The optional detailed body text. Can be <see cref="System.String.Empty"/>.</param>
        /// <param name="actions">
        /// Actions are sent over as a list of pairs. <para/>
        /// Each even element in the list (starting at index <code>0</code>) represents the identifier for the action.<para/>
        /// Each odd element in the list is the localized string that will be displayed to the user.<para/>
        /// </param>
        /// <param name="hints">
        /// <b>Optional</b> hints that can be passed to the server from the client program. Can be empty. <para/>
        /// Although clients and servers should never assume each other supports any specific hints, they can be used to pass along information, such as the process PID or window ID, that the server may be able to make use of.<para/>
        /// See Hints (https://people.gnome.org/~mccann/docs/notification-spec/notification-spec-latest.html#hints)
        /// </param>
        /// <param name="expireTimeout">
        /// The timeout time in <b>milliseconds</b> since the display of the notification at which the notification should automatically close.<para/>
        /// If <code>-1</code> (<see cref="FreeDesktopNotificationManager.DEFAULT_NOTIFICATION_EXPIRATION"/>), the notification's expiration time is dependent on the notification server's settings, and may vary for the type of notification.<para/>
        /// If <code>0</code> (<see cref="FreeDesktopNotificationManager.FOREVER_NOTIFICATION_EXPIRATION"/>), never expire. 
        /// </param>
        /// <returns>A task that will resolve to a notification ID</returns>
        Task<uint> NotifyAsync(
            string appName, uint replacesId, string appIcon, string summary, string body, string[] actions,
            IDictionary<string, object> hints, int expireTimeout
        );

        /// <summary>
        /// Causes a notification to be forcefully closed and removed from the user's view.
        /// It can be used, for example, in the event that what the notification pertains to is no longer relevant, or to cancel a notification with no expiration time.<para/>
        /// The <code>NotificationClosed</code> signal is emitted by this method.<para/>
        /// If the notification no longer exists, an empty D-BUS Error message is sent back.<para/>
        /// See <see cref="WatchNotificationClosedAsync"/>. 
        /// </summary>
        /// <param name="id">The notification ID.</param>
        /// <returns>A completed task.</returns>
        Task CloseNotificationAsync(uint id);

        /// <summary>
        /// It returns an array of strings.
        /// Each string describes an optional capability implemented by the server.
        /// </summary>
        /// <remarks>
        /// The following values are defined by this spec:<para/>
        /// <code>"action-icons"</code>: Supports using icons instead of text for displaying actions. Using icons for actions must be enabled on a per-notification basis using the "action-icons" hint.<para/>
        /// <code>"actions"</code>: The server will provide the specified actions to the user. Even if this cap is missing, actions may still be specified by the client, however the server is free to ignore them.<para/>
        /// <code>"body"</code>: Supports body text. Some implementations may only show the summary (for instance, onscreen displays, marquee/scrollers)<para/>
        /// <code>"body-hyperlinks"</code>: The server supports hyperlinks in the notifications.<para/>
        /// <code>"body-images"</code>: The server supports images in the notifications.<para/>
        /// <code>"body-markup"</code>: Supports markup in the body text. If marked up text is sent to a server that does not give this cap, the markup will show through as regular text so must be stripped clientside.<para/>
        /// <code>"icon-multi"</code>: The server will render an animation of all the frames in a given image array. The client may still specify multiple frames even if this cap and/or "icon-static" is missing, however the server is free to ignore them and use only the primary frame.<para/>
        /// <code>"icon-static"</code>: Supports display of exactly 1 frame of any given image array. This value is mutually exclusive with "icon-multi", it is a protocol error for the server to specify both.<para/>
        /// <code>"persistence"</code>: The server supports persistence of notifications. Notifications will be retained until they are acknowledged or removed by the user or recalled by the sender. The presence of this capability allows clients to depend on the server to ensure a notification is seen and eliminate the need for the client to display a reminding function (such as a status icon) of its own.<para/>
        /// <code>"sound"</code>: The server supports sounds on notifications. If returned, the server must support the "sound-file" and "suppress-sound" hints.<para/>
        /// </remarks>
        /// <returns>It returns an array of strings</returns>
        Task<string[]> GetCapabilitiesAsync();

        /// <summary>
        /// This message returns the information on the server.
        /// </summary>
        /// <returns>
        /// The server name, vendor, and version number in a <see cref="Tuple{T1,T2,T3,T4}"/>.<para/>
        /// <code>name</code>: The product name of the server.<para/>
        /// <code>vendor</code>: The vendor name. For example, "KDE," "GNOME," "freedesktop.org," or "Microsoft".<para/>
        /// <code>version</code>: The server's version number.
        /// <code>specVersion</code>: The specification version the server is compliant with.
        /// </returns>
        Task<(string name, string vendor, string version, string specVersion)> GetServerInformationAsync();

        /// <summary>
        /// A completed notification is one that has timed out, or has been dismissed by the user.
        /// </summary>
        /// <remarks>
        /// See <see cref="CloseNotificationAsync"/>
        /// </remarks>
        /// <param name="handler">
        /// A callback for every signal received.<para/>
        /// The <see cref="Tuple{T1,T2}"/> consists of:<para/>
        /// The <code>ID</code> of the notification that was closed.<para/>
        /// The <code>reason</code> the notification was closed.<para/>
        /// 1 - The notification expired.<para/>
        /// 2 - The notification was dismissed by the user.<para/>
        /// 3 - The notification was closed by a call to CloseNotification.<para/>
        /// 4 - Undefined/reserved reasons.
        /// </param>
        /// <param name="onError">
        /// A callback for errors from watching this signal
        /// </param>
        /// <returns>
        /// A disposable wrapper to cleanup when done watching this signal
        /// </returns>
        Task<IDisposable> WatchNotificationClosedAsync(
            Action<(uint id, uint reason)> handler,
            Action<Exception> onError = null
        );

        /// <summary>
        /// This signal is emitted when one of the following occurs:<para/>
        /// - The user performs some global "invoking" action upon a notification. For instance, clicking somewhere on the notification itself.<para/>
        /// - The user invokes a specific action as specified in the original Notify request. For example, clicking on an action button.<para/>
        /// </summary>
        /// <param name="handler">
        /// A callback for every signal received.<para/>
        /// <code>id</code>: The ID of the notification emitting the <code>ActionInvoked</code> signal.
        /// <code>actionKey</code>: The key of the action invoked. These match the keys sent over in the list of actions.
        /// </param>
        /// <param name="onError">
        /// A callback for errors from watching this signal
        /// </param>
        /// <returns>
        /// A disposable wrapper to cleanup when done watching this signal
        /// </returns>
        Task<IDisposable> WatchActionInvokedAsync(
            Action<(uint id, string actionKey)> handler,
            Action<Exception> onError = null
        );
    }
}
