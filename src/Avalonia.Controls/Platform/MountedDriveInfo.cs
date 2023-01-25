using System;
using Avalonia.Metadata;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Describes a Drive's properties.
    /// </summary>
    [Unstable]
    public class MountedVolumeInfo : IEquatable<MountedVolumeInfo>
    {
        public string? VolumeLabel { get; set; }
        public string? VolumePath { get; set; }
        public ulong VolumeSizeBytes { get; set; }

        public bool Equals(MountedVolumeInfo? other)
        {
            return this.VolumeSizeBytes.Equals(other?.VolumeSizeBytes) &&
                   Equals(this.VolumePath, other.VolumePath) &&
                   (this.VolumeLabel ?? string.Empty).Equals(other.VolumeLabel ?? string.Empty);
        }
    }
}
