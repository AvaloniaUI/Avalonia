// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Describes a Drive's properties.
    /// </summary>
    public class MountedVolumeInfo : IEquatable<MountedVolumeInfo>
    {
        public string VolumeLabel { get; set; }
        public string VolumeName { get; set; }
        public ulong VolumeSizeBytes { get; set; }
        public string DevicePath { get; set; }
        public string MountPath { get; set; }

        public bool Equals(MountedVolumeInfo other)
        {
            return this.VolumeLabel.Equals(other.VolumeLabel) &&
                   this.VolumeName.Equals(other.VolumeName) &&
                   this.VolumeSizeBytes.Equals(other.VolumeSizeBytes) &&
                   this.DevicePath.Equals(other.DevicePath) &&
                   this.MountPath.Equals(other.MountPath);
        }
    }
}
