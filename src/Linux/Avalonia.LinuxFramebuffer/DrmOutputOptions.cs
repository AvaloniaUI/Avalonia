using Avalonia.Media;

namespace Avalonia.LinuxFramebuffer
{
    /// <summary>
    /// DRM Output Options
    /// </summary>
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

        /// <summary>
        /// specific the video mode with which the DrmOutput should be created, if it is not found it will fallback to the preferred mode.
        /// If NULL preferred mode will be used.
        /// </summary>
        public PixelSize? VideoMode { get; set; }

        /// <summary>
        /// Specific whether our connector is HDMI-A, DVI, DisplayPort, etc.
        /// If NULL preferred connector will be used.
        /// </summary>
        public DrmConnectorType? ConnectorType { get; init; }

        /// <summary>
        /// Specific whether connector id using for <see cref="ConnectorType"/>
        /// If NULL preferred connector id will be used
        /// </summary>
        public uint? ConnectorType_Id { get; init; }
    }
}
