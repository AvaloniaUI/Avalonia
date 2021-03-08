using System;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    internal class AvaloniaNativeApplicationPlatform : CallbackBase, IApplicationPlatform, IAvnApplicationEvents
    {
        public Action<string[]> FilesOpened { get; set; }
        
        void IAvnApplicationEvents.FilesOpened(IAvnStringArray urls)
        {
            FilesOpened?.Invoke(urls.ToStringArray());
        }
    }
}
