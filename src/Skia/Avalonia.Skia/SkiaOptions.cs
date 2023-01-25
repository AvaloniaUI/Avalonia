using System;
using Avalonia.Skia;

namespace Avalonia
{
    /// <summary>
    /// Options for Skia rendering subsystem.
    /// </summary>
    public class SkiaOptions
    {
        /// <summary>
        /// The maximum number of bytes for video memory to store textures and resources.
        /// </summary>
        /// <remarks>
        /// This is set by default to the recommended value for Avalonia.
        /// Setting this to null will give you the default Skia value.
        /// </remarks>
        public long? MaxGpuResourceSizeBytes { get; set; } = 1024 * 600 * 4 * 12; // ~28mb 12x 1024 x 600 textures. 
    }
}
