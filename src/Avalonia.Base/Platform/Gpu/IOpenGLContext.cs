// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Platform.Gpu
{
    /// <summary>
    /// OpenGL rendering context.
    /// </summary>
    public interface IOpenGLContext : IDisposable
    {
        /// <summary>
        /// Notify context that it's backing window was resized.
        /// </summary>
        void ResizeNotify();

        /// <summary>
        /// Swap buffers of backing window.
        /// </summary>
        void SwapBuffers();

        /// <summary>
        /// Get framebuffer size.
        /// </summary>
        /// <param name="platformHandle">Platform handle.</param>
        /// <returns>Size of a framebuffer.</returns>
        (int width, int height) GetFramebufferSize(IPlatformHandle platformHandle);

        /// <summary>
        /// Gets current framebuffer parameters.
        /// </summary>
        /// <returns>Current framebuffer parameters.</returns>
        FramebufferParameters GetCurrentFramebufferParameters();
    }
}
