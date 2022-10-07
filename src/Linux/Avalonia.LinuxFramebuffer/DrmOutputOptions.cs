using Avalonia.LinuxFramebuffer.Output;
using Avalonia.Media;
using JetBrains.Annotations;

namespace Avalonia.LinuxFramebuffer
{
    public class DrmOutputOptions
    {
        /// <summary>
        /// Scaling factor.
        /// Default: 1.0
        /// </summary>
        public double Scaling { get; set; } = 1.0;
        
        /// <summary>
        /// If true an two cycle buffer swapping is processed at init.
        /// Default: True
        /// </summary>
        public bool EnableInitialBufferSwapping { get; set; } = true;
        
        /// <summary>
        /// Color for <see cref="EnableInitialBufferSwapping"/>
        /// Default: R0 G0 B0 A0
        /// </summary>
        public Color InitialBufferSwappingColor { get; set; } = new Color(0, 0, 0, 0);

        public PixelSize? VideoMode { get; set; }
    }
}
