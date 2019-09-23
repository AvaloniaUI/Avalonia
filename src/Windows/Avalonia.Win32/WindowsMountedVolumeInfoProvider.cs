using System;
using System.Collections.ObjectModel;
using Avalonia.Controls.Platform;

namespace Avalonia.Win32
{
    public class WindowsMountedVolumeInfoProvider : IMountedVolumeInfoProvider
    {
        public IDisposable Listen(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            Contract.RequireNotNull(mountedDrives);
            return new WindowsMountedVolumeInfoListener(mountedDrives);
        }
    }
}
