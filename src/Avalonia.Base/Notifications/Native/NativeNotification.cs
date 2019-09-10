using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Avalonia.Notifications.Native
{
    [PublicAPI]
    public class NativeNotification : Notification
    {
        public NativeNotification(
            [NotNull] string title,
            [NotNull] string message,
            NotificationUrgency urgency = NotificationUrgency.Low,
            [CanBeNull] TimeSpan? expiration = null,
            [CanBeNull] Action onClick = null,
            [CanBeNull] Action<NativeNotificationCloseReason> onClose = null
        )
            : base(title, message, NotificationType.Information, expiration, onClick)
        {
            Urgency = urgency;
            OnClose = onClose;
        }

        /// <summary>
        /// Notifications have an urgency level associated with them. This defines the importance of the notification. For example, "Joe Bob signed on" would be a low urgency. "You have new mail" or "A USB device was unplugged" would be a normal urgency. "Your computer is on fire" would be a critical urgency.
        /// </summary>
        public NotificationUrgency Urgency { get; set; }

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

        /// <inheritdoc cref="INotification.OnClose" />
        public new Action<NativeNotificationCloseReason> OnClose { get; set; }

        /// <summary>
        /// A list of <see cref="NativeNotificationAction"/>
        /// </summary>
        [CanBeNull]
        [ItemNotNull]
        public IReadOnlyList<NativeNotificationAction> Actions { get; set; }

        public event EventHandler<ActionInvokedEventArgs> ActionInvoked;

        /// <summary>
        /// Event invoker for <see cref="ActionInvoked"/>
        /// </summary>
        /// <param name="e">The event args</param>
        public virtual void OnActionInvoked([NotNull] ActionInvokedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            ActionInvoked?.Invoke(this, e);

            if (e.Action.Equals("default", StringComparison.Ordinal))
                OnClick?.Invoke();
            else
                InvokeAction(e);
        }

        /// <summary>
        /// Invokes the <see cref="NativeNotificationAction.Action"/> if able to find a <see cref="NativeNotificationAction.Key"/> that matches in <see cref="Actions"/>.
        /// </summary>
        /// <param name="e">The event args</param>
        protected virtual void InvokeAction([NotNull] ActionInvokedEventArgs e)
        {
            if (Actions == null)
                return;

            foreach (var action in Actions)
            {
                if (action.Key.Equals(e.Action, StringComparison.Ordinal))
                    action.Action.Invoke(action);
            }
        }

        /// <inheritdoc />
        public override Task CloseAsync()
        {
            if (NotificationManager is INativeNotificationManager nnm)
                return nnm.CloseAsync(this);
            return base.CloseAsync();
        }

        /// <inheritdoc />
        public override INotification Clone()
        {
            return new NativeNotification(
                Title,
                Message,
                Urgency,
                Expiration,
                OnClick,
                OnClose
            ) { Actions = Actions, Resident = Resident, Transient = Transient };
        }
    }
}
