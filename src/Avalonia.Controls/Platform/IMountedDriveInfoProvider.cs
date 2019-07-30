// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Defines a platform-specific drive mount information provider implementation.
    /// </summary>
    public interface IMountedDriveInfoProvider
    {
        /// <summary>
        /// Observable list of currently-mounted drives.
        /// </summary>
        ObservableCollection<MountedDriveInfo> CurrentDrives { get; }

        /// <summary>
        /// Determines if the service is currently monitoring.
        /// </summary> 
        bool IsMonitoring { get; }

        /// <summary>
        /// Start the service polling routine.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the service polling routine.
        /// </summary>
        void Stop();
    }
}
