// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Platform.Gpu;

namespace Avalonia.Skia.Gpu
{
    /// <summary>
    /// Skia OpenGL render backend.
    /// </summary>
    public class OpenGLRenderBackend : IGpuRenderBackend
    {
        private readonly IOpenGLPlatform _openGLPlatform;
        private readonly IWindowingPlatform _windowingPlatform;
        private IWindowImpl _resourceWindow;
        private IGpuRenderContext _resourceRenderContext;
        
        /// <summary>
        /// Create new OpenGL render backend using provided platform.
        /// </summary>
        /// <param name="openGLPlatform">OpenGL platform to use.</param>
        /// <param name="windowingPlatform">Windowing platform to use.</param>
        public OpenGLRenderBackend(IOpenGLPlatform openGLPlatform, IWindowingPlatform windowingPlatform)
        {
            _windowingPlatform = windowingPlatform ?? throw new ArgumentNullException(nameof(windowingPlatform));
            _openGLPlatform = openGLPlatform ?? throw new ArgumentNullException(nameof(openGLPlatform));
        }

        /// <inheritdoc />
        public IGpuRenderContext ResourceRenderContext
        {
            get
            {
                if (_resourceRenderContext == null)
                {
                    _resourceWindow = _windowingPlatform.CreateWindow();

                    if (_resourceWindow == null)
                    {
                        return null;
                    }

                    _resourceRenderContext = new OpenGLRenderContext(_resourceWindow.Handle, _openGLPlatform);
                }

                return _resourceRenderContext;
            }
        }
        
        /// <inheritdoc />
        public IGpuRenderContext CreateRenderContext(IPlatformHandle platformHandle)
        {
            return new OpenGLRenderContext(platformHandle, _openGLPlatform);
        }
    }
}