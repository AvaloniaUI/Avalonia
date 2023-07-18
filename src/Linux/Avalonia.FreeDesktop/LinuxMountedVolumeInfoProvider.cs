using System;
using System.Collections.ObjectModel;

using Avalonia.Controls.Platform;

namespace Avalonia.FreeDesktop
{
    internal class LinuxMountedVolumeInfoProvider : IMountedVolumeInfoProvider
    {
        public IDisposable Listen(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            return new LinuxMountedVolumeInfoListener(ref mountedDrives);
        }
    }
}
