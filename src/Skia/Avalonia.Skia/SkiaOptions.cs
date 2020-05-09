using System;
using Avalonia.Skia;

namespace Avalonia
{
    /// <summary>
    /// Options for Skia rendering subsystem.
    /// </summary>
    public class SkiaOptions
    {
        public SkiaOptions()
        {
            MaxGpuResourceSizeBytes = 100663296; // Value taken from skia.
        }
        /// <summary>
        /// Custom gpu factory to use. Can be used to customize behavior of Skia renderer.
        /// </summary>
        public Func<ICustomSkiaGpu> CustomGpuFactory { get; set; }

        /// <summary>
        /// The maximum number of bytes for video memory to store textures and resources.
        /// </summary>
        public long MaxGpuResourceSizeBytes { get; set; }
    }
}
