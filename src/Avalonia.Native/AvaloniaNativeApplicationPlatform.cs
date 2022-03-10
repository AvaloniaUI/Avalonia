using System;
using System.ComponentModel;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    internal class AvaloniaNativeApplicationPlatform : NativeCallbackBase, IAvnApplicationEvents, IPlatformLifetimeEventsImpl
    {
        public event EventHandler<ShutdownRequestedEventArgs> ShutdownRequested;
        
        void IAvnApplicationEvents.FilesOpened(IAvnStringArray urls)
        {
            ((IApplicationPlatformEvents)Application.Current).RaiseUrlsOpened(urls.ToStringArray());
        }

        public int TryShutdown()
        {
            if (ShutdownRequested is null) return 1;
            var e = new ShutdownRequestedEventArgs();
            ShutdownRequested(this, e);
            return (!e.Cancel).AsComBool();
        }
    }
}
