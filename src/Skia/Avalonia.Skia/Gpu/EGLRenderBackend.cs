// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Platform.Gpu;
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

        /// <summary>
        /// Create new EGL render backend.
        /// </summary>
        /// <param name="platform">Platform to use.</param>
        public EGLRenderBackend(IEGLPlatform platform)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
            _platform.MakeCurrent(null);

            CreateSkiaContext();
        }

        /// <summary>
        /// Create Skia context using EGL
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
            var surface = _platform.CreateSurface(surfaces);

            return surface != null ? new EGLRenderContext(surface, _platform, _context) : null;
        }

        /// <inheritdoc />
        public IGpuRenderContextBase CreateOffscreenRenderContext()
        {
            return new EGLRenderContextBase(_platform, _context);
        }
    }
}