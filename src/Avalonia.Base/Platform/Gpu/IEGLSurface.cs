namespace Avalonia.Platform.Gpu
{
    /// <summary>
    /// EGL renderable surface.
    /// </summary>
    public interface IEGLSurface
    {
        /// <summary>
        /// Get size of a surface.
        /// </summary>
        /// <returns>Size of a surface.</returns>
        (int width, int height) GetSize();

        /// <summary>
        /// Get dpi of a surface.
        /// </summary>
        /// <returns>Dpi of a surface</returns>
        (int x, int y) GetDpi();

        /// <summary>
        /// Get framebuffer parameters.
        /// </summary>
        /// <returns>Framebuffer parameters</returns>
        FramebufferParameters GetFramebufferParameters();
    }
}