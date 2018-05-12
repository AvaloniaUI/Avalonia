// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Platform.Gpu;
using SkiaSharp;

namespace Avalonia.Skia.Gpu
{
    /// <summary>
    /// Skia OpenGL render context.
    /// </summary>
    public class OpenGLRenderContext : IGpuRenderContext
    {
        private readonly IOpenGLPlatform _openGLPlatform;
        private readonly IOpenGLContext _context;
        private GRGlInterface _glInterface;

        public OpenGLRenderContext(IPlatformHandle platformHandle, IOpenGLPlatform openGLPlatform)
        {
            _openGLPlatform = openGLPlatform ?? throw new ArgumentNullException(nameof(openGLPlatform));
            PlatformHandle = platformHandle ?? throw new ArgumentNullException(nameof(platformHandle));
            
            _context = _openGLPlatform.CreateContext(platformHandle);

            if (_context == null)
            {
                throw new InvalidOperationException("Failed to create OpenGL context.");
            }

            CreateSkiaContext();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
            Context?.Dispose();
            _glInterface?.Dispose();
        }

        /// <inheritdoc />
        public GRContext Context { get; private set; }

        /// <inheritdoc />
        public IPlatformHandle PlatformHandle { get; }

        /// <inheritdoc />
        public FramebufferParameters GetPrimaryFramebufferDescriptor()
        {
            _openGLPlatform.MakeContextCurrent(_context);

            return _context.GetCurrentFramebufferParameters();
        }

        /// <inheritdoc />
        public void NotifyResize()
        {
            _context.ResizeNotify();
        }

        /// <inheritdoc />
        public void PrepareForRendering()
        {
            if (_openGLPlatform.IsContextCurrent(_context))
            {
                return;
            }

            _openGLPlatform.MakeContextCurrent(_context);
        }
        
        /// <inheritdoc />
        public void Present()
        {
            _context.SwapBuffers();
        }

        /// <inheritdoc />
        public Size GetFramebufferSize(IPlatformHandle platformHandle)
        {
            var size = _context.GetFramebufferSize(platformHandle);

            return new Size(size.width, size.height);
        }

        /// <summary>
        /// Create Skia rendering context using OpenGL interface.
        /// </summary>
        private void CreateSkiaContext()
        {
            _glInterface = GRGlInterface.CreateNativeGlInterface();

            if (_glInterface == null)
            {
                Dispose();

                throw new InvalidOperationException("Failed to create OpenGL interface.");
            }

            var options = GRContextOptions.Default;
            
            Context = GRContext.Create(GRBackend.OpenGL, _glInterface, options);
            
            if (Context == null)
            {
                Dispose();

                throw new InvalidOperationException("Failed to create Skia OpenGL context.");
            }
        }
    }
}