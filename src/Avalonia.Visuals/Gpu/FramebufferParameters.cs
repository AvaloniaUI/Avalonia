// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Gpu
{
    /// <summary>
    /// Framebuffer descriptor info.
    /// </summary>
    public struct FramebufferParameters
    {
        /// <summary>
        /// Handle to framebuffer.
        /// </summary>
        public IntPtr FramebufferHandle;

        /// <summary>
        /// Sample count used by framebuffer.
        /// </summary>
        public int SampleCount;

        /// <summary>
        /// Stencil bits used by framebuffer.
        /// </summary>
        public int StencilBits;
    }
}