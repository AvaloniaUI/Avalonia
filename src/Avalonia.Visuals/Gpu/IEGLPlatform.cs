// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Gpu
{
    /// <summary>
    /// OpenGL support platform.
    /// </summary>
    public interface IEGLPlatform
    {
        /// <summary>
        /// Create EGL surface for given surfaces.
        /// </summary>
        /// <param name="surfaces">Surfaces.</param>
        /// <returns>Created surface, or null if creation failed.</returns>
        IEGLSurface CreateSurface(IEnumerable<object> surfaces);

        /// <summary>
        /// Check if platform is supported.
        /// </summary>
        /// <returns>True if platform is supported.</returns>
        bool IsSupported();

        /// <summary>
        /// Initialize platform.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Make given surface current.
        /// </summary>
        /// <param name="surface">Surface to make current.</param>
        /// <returns>True if surface was made current.</returns>
        bool MakeCurrent(IEGLSurface surface);

        /// <summary>
        /// Swap buffers of given surface.
        /// </summary>
        /// <param name="surface">Surface to swap buffer in.</param>
        /// <returns>True if surface buffers were swapped.</returns>
        bool SwapBuffers(IEGLSurface surface);

        /// <summary>
        /// Destroys passed surface.
        /// </summary>
        /// <param name="surface">Surface to destroy.</param>
        void DestroySurface(IEGLSurface surface);

        /// <summary>
        /// Recreate given surface.
        /// </summary>
        /// <param name="surface">Surface to recreate.</param>
        /// <returns>Recreated surface.</returns>
        IEGLSurface RecreateSurface(IEGLSurface surface);
    }
}