using System;

namespace Avalonia.Notifications.Native
{
    public sealed class ActionInvokedHandlerArgs : EventArgs
    {
        public ActionInvokedHandlerArgs(string action)
        {
            Action = action;
        }

        public string Action { get; }
    }
}
