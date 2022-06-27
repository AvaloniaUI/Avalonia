using System;
using System.Collections.ObjectModel;
using Avalonia.Metadata;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Defines a platform-specific mount volumes info provider implementation.
    /// </summary>
    [Unstable]
    public interface IMountedVolumeInfoProvider 
    {
        /// <summary>
        /// Listens to any changes in volume mounts and
        /// forwards updates to the referenced
        /// <see cref="ObservableCollection{MountedDriveInfo}"/>.
        /// </summary> 
        IDisposable Listen(ObservableCollection<MountedVolumeInfo> mountedDrives);
    }
}
