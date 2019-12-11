// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Defines a platform-specific mount volumes info provider implementation.
    /// </summary>
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
