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
        public string VolumePath { get; set; }
        public ulong VolumeSizeBytes { get; set; }

        public bool Equals(MountedVolumeInfo other)
        {
            return this.VolumeSizeBytes.Equals(other.VolumeSizeBytes) &&
                   this.VolumePath.Equals(other.VolumePath) &&
                   (this.VolumeLabel ?? string.Empty).Equals(other.VolumeLabel ?? string.Empty);
        }
    }
}
