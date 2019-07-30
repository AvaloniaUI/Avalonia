// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Describes a Drive's properties.
    /// </summary>
    public class MountedDriveInfo : IEquatable<MountedDriveInfo>
    {
        public string DriveLabel { get; set; }
        public string DriveName { get; set; }
        public ulong DriveSizeBytes { get; set; }
        public string DevicePath { get; set; }
        public string MountPath { get; set; }

        public bool Equals(MountedDriveInfo other)
        {
            return this.DriveLabel.Equals(other.DriveLabel) &&
                   this.DriveName.Equals(other.DriveName) &&
                   this.DriveSizeBytes.Equals(other.DriveSizeBytes) &&
                   this.DevicePath.Equals(other.DevicePath) &&
                   this.MountPath.Equals(other.MountPath);
        }
    }
}
