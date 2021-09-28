namespace Avalonia.LinuxFramebuffer
{
    /// <summary>
    /// Platform-specific options which apply to the Linux framebuffer.
    /// </summary>
    public class LinuxFramebufferPlatformOptions
    {
        /// <summary>
        /// Gets or sets the number of frames per second at which the renderer should run.
        /// Default 60.
        /// </summary>
        public int Fps { get; set; } = 60;
    }
}
