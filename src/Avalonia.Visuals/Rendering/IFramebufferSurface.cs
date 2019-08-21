using Avalonia.Platform;

namespace Avalonia.Rendering
{
    public interface IFramebufferSurface
    {
        /// <summary>
        /// Provides a framebuffer descriptor for drawing.
        /// </summary>
        /// <remarks>
        /// Contents should be drawn on actual surface after disposing.
        /// </remarks>
        ILockedFramebuffer Lock();
    }
}
