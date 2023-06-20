using System;
using System.Collections.ObjectModel;
using Avalonia.Controls.Platform;

namespace Avalonia.Win32
{
    internal class WindowsMountedVolumeInfoProvider : IMountedVolumeInfoProvider
    {
        public IDisposable Listen(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            return new WindowsMountedVolumeInfoListener(mountedDrives);
        }
    }
}
