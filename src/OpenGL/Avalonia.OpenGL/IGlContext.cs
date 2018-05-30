// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.OpenGL
{
    /// <summary>
    /// GL Context
    /// </summary>
    public interface IGlContext : IDisposable
    {
        /// <summary>
        /// Sets the GL Surface for a Context
        /// </summary>
        IGlSurface Surface { get; }

        /// <summary>
        /// Swaps buffers
        /// </summary>
        void SwapBuffers();

        /// <summary>
        /// Makes this GL Context current
        /// </summary>
        void MakeCurrent();

        IntPtr GetProcAddress(string functionName);

        /// <returns>True if this Context is the current one. False otherwise.</returns>
        bool IsCurrentContext();

        /// <summary>
        /// Recreates the current surface
        /// </summary>
        void RecreateSurface();
    }
}