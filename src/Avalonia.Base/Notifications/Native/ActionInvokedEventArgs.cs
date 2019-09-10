using System;
using JetBrains.Annotations;

namespace Avalonia.Notifications.Native
{
    [PublicAPI]
    public sealed class ActionInvokedEventArgs : EventArgs
    {
        /// <param name="action">Identifier</param>
        public ActionInvokedEventArgs([NotNull] string action)
        {
            Action = action;
        }

        /// <summary>
        /// Identifier (see <see cref="NativeNotificationAction.Key"/>) 
        /// </summary>
        [NotNull]
        public string Action { get; }
    }
}
