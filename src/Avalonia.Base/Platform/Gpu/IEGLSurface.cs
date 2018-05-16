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
        /// <returns></returns>
        (int width, int height) GetSize();

        /// <summary>
        /// Get framebuffer parameters.
        /// </summary>
        /// <returns>Framebuffer parameters</returns>
        FramebufferParameters GetFramebufferParameters();
    }
}