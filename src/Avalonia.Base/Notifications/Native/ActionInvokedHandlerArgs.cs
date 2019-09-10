using System;
using JetBrains.Annotations;

namespace Avalonia.Notifications.Native
{
    [PublicAPI]
    public sealed class ActionInvokedHandlerArgs : EventArgs
    {
        public ActionInvokedHandlerArgs([NotNull] string action)
        {
            Action = action;
        }

        [NotNull] public string Action { get; }
    }
}
