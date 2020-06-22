using System;
using System.Collections.ObjectModel;

using Avalonia.Controls.Platform;

namespace Avalonia.FreeDesktop
{
    public class LinuxMountedVolumeInfoProvider : IMountedVolumeInfoProvider
    {
        public IDisposable Listen(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            Contract.Requires<ArgumentNullException>(mountedDrives != null);
            return new LinuxMountedVolumeInfoListener(ref mountedDrives);
        }
    }
}
