using Avalonia.LinuxFramebuffer.Output;
using Avalonia.Media;
using JetBrains.Annotations;

namespace Avalonia.LinuxFramebuffer
{
    public class LinuxDrmOptions
    {
        /// <summary>
        /// Path for DrmCard to use, if no <see cref="OutputBackend"/> is passed.
        /// Default: null
        /// </summary>
        [CanBeNull] public string Card { get; set; }
        
        /// <summary>
        /// True to call drmModeGetConnector for all available connectors, otherwise drmModeGetConnectorCurrent is called to get the kernel-cached connected connector.
        /// Info: since some hardware might have incorrect connector information on startup for some reason, you may need to set this parameter to true.
        /// Default: False
        /// </summary>
        public bool DrmConnectorsForceProbe { get; set; } = false;
        
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
        /// IOutputBackend. If Null, a new DrmOutput for <see cref="Card"/> with <see cref="Scaling"/> will be created.
        /// Default: null
        /// </summary>
        [CanBeNull] public IOutputBackend OutputBackend { get; set; }
    }
}
