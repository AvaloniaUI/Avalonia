// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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