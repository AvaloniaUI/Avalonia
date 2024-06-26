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
        /// Specifies the video mode with which the DrmOutput should be created, if it is not found it will fallback to the preferred mode.
        /// If null, the preferred mode will be used.
        /// </summary>
        public PixelSize? VideoMode { get; set; }

        /// <summary>
        /// Specifies whether our connector is HDMI-A, DVI, DisplayPort, etc.
        /// If null, the preferred connector will be used.
        /// </summary>
        public DrmConnectorType? ConnectorType { get; set; }

        /// <summary>
        /// Specifies the connector type ID used with <see cref="ConnectorType"/>.
        /// If null, the preferred connector type ID will be used.
        /// </summary>
        public uint? ConnectorTypeId { get; set; }
    }
}
