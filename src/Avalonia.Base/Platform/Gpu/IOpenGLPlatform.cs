// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Platform.Gpu
{
    /// <summary>
    /// OpenGL support platform.
    /// </summary>
    public interface IOpenGLPlatform
    {
        /// <summary>
        /// Create OpenGL context.
        /// </summary>
        /// <param name="platformHandle">Platform handle of backing window.</param>
        /// <returns>Created OpenGL context.</returns>
        /// <exception cref="InvalidOperationException">Thrown when context cannot be created.</exception>
        IOpenGLContext CreateContext(IPlatformHandle platformHandle);

        /// <summary>
        /// Check if given context is current.
        /// </summary>
        /// <param name="context">Context to check.</param>
        /// <returns>True if context is current.</returns>
        bool IsContextCurrent(IOpenGLContext context);

        /// <summary>
        /// Makes given context current.
        /// </summary>
        /// <param name="context">Context to set current.</param>
        void MakeContextCurrent(IOpenGLContext context);
    }
}