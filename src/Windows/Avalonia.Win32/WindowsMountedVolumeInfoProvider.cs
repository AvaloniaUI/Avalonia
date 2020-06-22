using System;
using System.Collections.ObjectModel;
using Avalonia.Controls.Platform;

namespace Avalonia.Win32
{
    public class WindowsMountedVolumeInfoProvider : IMountedVolumeInfoProvider
    {
        public IDisposable Listen(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            Contract.Requires<ArgumentNullException>(mountedDrives != null);
            return new WindowsMountedVolumeInfoListener(mountedDrives);
        }
    }
}
