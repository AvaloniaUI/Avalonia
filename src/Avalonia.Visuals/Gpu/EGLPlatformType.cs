// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Gpu
{
    /// <summary>
    /// EGL platform types.
    /// </summary>
    public enum EGLPlatformType
    {
        /// <summary>
        /// Default for current OS.
        /// </summary>
        Default,

        /// <summary>
        /// Direct3D 9
        /// </summary>
        D3D9,

        /// <summary>
        /// Direct3D 11
        /// </summary>
        D3D11,

        /// <summary>
        /// OpenGL
        /// </summary>
        OpenGL,

        /// <summary>
        /// OpenGL ES
        /// </summary>
        OpenGL_ES
    }
}