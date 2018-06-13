// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Gpu;
using SkiaSharp;

namespace Avalonia.Skia.Gpu
{
    /// <summary>
    /// Skia EGL render backend.
    /// </summary>
    public class EGLRenderBackend : IGpuRenderBackend
    {
        private readonly IEGLPlatform _platform;
        private GRGlInterface _interface;
        private GRContext _context;
        private bool _initialized;

        /// <summary>
        /// Create new EGL render backend.
        /// </summary>
        /// <param name="platform">Platform to use.</param>
        public EGLRenderBackend(IEGLPlatform platform)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        }

        /// <summary>
        /// Ensure that backend is initialized.
        /// </summary>
        private void EnsureInitialized()
        {
            // Due to threading issues on OpenGL the platform must be initialized on render thread
            // Probably we need to add a check in constructor to make sure that context "could" be created, otherwise fallback to cpu won't work.
            if (_initialized)
            {
                return;
            }
            
            _platform.Initialize();
            _platform.MakeCurrent(null);

            CreateSkiaContext();

            _initialized = true;
        }

        /// <summary>
        /// Create Skia context using EGL.
        /// </summary>
        private void CreateSkiaContext()
        {
            // Long story short - on OpenGL the GrContext cannot be created from AssembleInterface for some reason.
            var (context, glInterface) = TryCreateContext(() => GRGlInterface.AssembleInterface((o, name) => EGL.GetProcAddress(name)));

            if (context == null || glInterface == null)
            {
                // Fallback to native interface
                (context, glInterface) = TryCreateContext(GRGlInterface.CreateNativeGlInterface);
            }

            if (context == null || glInterface == null)
            {
                throw new InvalidOperationException("Failed to create Skia OpenGL context.");
            }

            _interface = glInterface;
            _context = context;
        }

        /// <summary>
        /// Try creating Skia context.
        /// </summary>
        /// <param name="interfaceFactory">Interface factory.</param>
        /// <returns>Context and interface if creation worked.</returns>
        private (GRContext context, GRGlInterface glInterface) TryCreateContext(Func<GRGlInterface> interfaceFactory)
        {
            var glInterface = interfaceFactory();

            if (glInterface == null)
            {
                return default;
            }

            var options = GRContextOptions.Default;
            var context = GRContext.Create(GRBackend.OpenGL, glInterface, options);

            if (context == null)
            {
                glInterface.Dispose();

                return default;
            }

            return (context, glInterface);
        }

        /// <inheritdoc />
        public IGpuRenderContext CreateRenderContext(IEnumerable<object> surfaces)
        {
            EnsureInitialized();

            var surface = _platform.CreateSurface(surfaces);

            return surface != null ? new EGLRenderContext(surface, _platform, _context) : null;
        }

        /// <inheritdoc />
        public IGpuRenderContextBase CreateOffscreenRenderContext()
        {
            EnsureInitialized();

            return new EGLRenderContextBase(_platform, _context);
        }
    }
}