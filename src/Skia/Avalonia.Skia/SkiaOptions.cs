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
        /// Custom gpu factory to use. Can be used to customize behavior of Skia renderer.
        /// </summary>
        public Func<ISkiaGpu> CustomGpuFactory { get; set; }
    }
}
