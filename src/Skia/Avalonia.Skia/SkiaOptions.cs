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

        /// <summary>
        /// Use Skia's SaveLayer API to handling opacity.
        /// </summary>
        /// <remarks>
        /// Enabling this might have performance implications.
        /// </remarks>
        public bool UseOpacitySaveLayer { get; set; } = false;
        
        /// <summary>
        /// Gets whether stencil buffers can be used for various draw operations, improving clipping performance.
        /// Enabling them lets Skia pick multisample-based path rendering, which quantizes edge coverage and
        /// visibly degrades anti-aliasing of vector geometry, so they are disabled unless explicitly requested.
        /// </summary>
        public bool? UseStencilBuffers { get; set; }

        /// <summary>
        /// Maps <see cref="UseStencilBuffers"/> onto Skia's inverted <c>GRContextOptions.AvoidStencilBuffers</c>.
        /// Only an explicit <c>true</c> opts in: stencil buffers trade geometry anti-aliasing quality for
        /// clipping speed (see issue #21760), so they must never be enabled implicitly.
        /// </summary>
        internal static bool ShouldAvoidStencilBuffers(bool? useStencilBuffers) => useStencilBuffers != true;
    }
}
