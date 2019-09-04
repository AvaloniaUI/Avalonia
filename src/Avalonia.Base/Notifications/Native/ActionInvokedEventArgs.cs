using System;

namespace Avalonia.Notifications.Native
{
    public sealed class ActionInvokedEventArgs : EventArgs
    {
        /// <param name="action"></param>
        public ActionInvokedEventArgs(string action)
        {
            Action = action;
        }

        /// <summary>
        /// Identifier (see <see cref="NativeNotificationAction.Key"/>) 
        /// </summary>
        public string Action { get; }
    }
}
