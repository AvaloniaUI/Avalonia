using System;
using JetBrains.Annotations;

namespace Avalonia.Notifications.Native
{
    /// <summary>
    /// The representation of the <code>actions</code> capability, which makes the server send a request message back
    /// to the notification client when invoked. <para/>
    /// This functionality may not be implemented by the notification server, conforming clients should check
    /// if it is available before using it (see <see cref="INativeNotificationManager.GetCapabilitiesAsync"/>).
    /// </summary>
    public class NativeNotificationAction
    {
        /// <param name="text">Used as <see cref="Key"/> and <see cref="Label"/>.</param>
        /// <param name="action">Callback</param>
        public NativeNotificationAction(
            [NotNull] string text,
            [NotNull] Action<NativeNotificationAction> action
        )
            : this(text, text, action)
        {
        }

        /// <param name="key">Identifier</param>
        /// <param name="label">Displayed text (this will not be translated by the server)</param>
        /// <param name="action">Callback</param>
        public NativeNotificationAction(
            [NotNull] string key,
            [NotNull] string label,
            [NotNull] Action<NativeNotificationAction> action
        )
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>
        /// Identifier
        /// </summary>
        [NotNull]
        public string Key { get; }

        /// <summary>
        /// Displayed text (this will not be translated by the server)
        /// </summary>
        [NotNull]
        public string Label { get; }

        /// <summary>
        /// Callback
        /// </summary>
        [NotNull]
        public Action<NativeNotificationAction> Action { get; }
    }
}
