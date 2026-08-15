using System;
using Avalonia.Threading;

namespace Avalonia.Win32.Automation
{
    /// <summary>
    /// Marshals UI Automation COM calls, which can arrive on an arbitrary thread, onto the
    /// Avalonia UI thread. Shared by <see cref="AutomationNode"/> and other COM wrapper objects
    /// (e.g. <see cref="Win32TextRangeProvider"/>) that are not themselves an
    /// <see cref="AutomationNode"/>.
    /// </summary>
    internal static class Win32DispatcherHelper
    {
        public static void InvokeSync(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.InvokeAsync(action).Wait();
        }

        public static T InvokeSync<T>(Func<T> func)
        {
            if (Dispatcher.UIThread.CheckAccess())
                return func();
            else
                return Dispatcher.UIThread.InvokeAsync(func).Result;
        }
    }
}
