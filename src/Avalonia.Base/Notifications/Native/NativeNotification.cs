using System;
using System.Collections.Generic;

namespace Avalonia.Notifications.Native
{
    public class NativeNotification : Notification
    {
        public NativeNotification(
            string title, string message, NotificationType type = NotificationType.Information,
            TimeSpan? expiration = null, Action onClick = null, Action onClose = null
        )
            : base(title, message, type, expiration, onClick, onClose)
        {
        }

        /// <summary>
        /// When set the server will treat the notification as transient and by-pass the server's persistence capability, if it should exist.
        /// </summary>
        public bool? Transient { get; set; }

        /// <summary>
        /// When set the server will not automatically remove the notification when an action has been invoked.
        /// The notification will remain resident in the server until it is explicitly removed by the user or by the sender.
        /// This is only useful when the server has the "persistence" capability.
        /// </summary>
        public bool? Resident { get; set; }

        /// <summary>
        /// A list of <see cref="NativeNotificationAction"/>
        /// </summary>
        public IReadOnlyList<NativeNotificationAction> Actions { get; set; }

        public event EventHandler<ActionInvokedHandlerArgs> ActionInvoked;

        /// <summary>
        /// Event invoker for <see cref="ActionInvoked"/>
        /// </summary>
        /// <param name="e">The event args</param>
        public virtual void OnActionInvoked(ActionInvokedHandlerArgs e)
        {
            ActionInvoked?.Invoke(this, e);

            InvokeAction(e);
        }

        /// <summary>
        /// Invokes the <see cref="NativeNotificationAction.Action"/> if able to find a <see cref="NativeNotificationAction.Key"/> that matches in <see cref="Actions"/>.
        /// </summary>
        /// <param name="e">The event args</param>
        protected virtual void InvokeAction(ActionInvokedHandlerArgs e)
        {
            if (Actions == null)
                return;

            foreach (var action in Actions)
            {
                if (action.Key.Equals(e.Action, StringComparison.Ordinal))
                    action.Action.Invoke(action);
            }
        }
    }
}
