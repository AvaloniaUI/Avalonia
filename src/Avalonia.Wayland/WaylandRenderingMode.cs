namespace Avalonia.Wayland
{
    /// <summary>
    /// Represents the rendering mode for platform graphics.
    /// </summary>
    public enum WaylandRenderingMode
    {
        /// <summary>
        /// Avalonia is rendered into a framebuffer.
        /// </summary>
        Software = 1,

        /// <summary>
        /// Enables native Linux EGL rendering.
        /// </summary>
        Egl = 2
    }
}
