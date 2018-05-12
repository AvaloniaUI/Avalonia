// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;
using Avalonia.Platform.Gpu;
using Avalonia.Skia.Gpu;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia backend types.
    /// </summary>
    public enum RenderBackendType
    {
        /// <summary>
        /// Cpu based raster backend.
        /// </summary>
        Raster,

        /// <summary>
        /// Gpu accelerated backend.
        /// </summary>
        OpenGL
    }

    /// <summary>
    /// Skia platform initializer.
    /// </summary>
    public static class SkiaPlatform
    {
        /// <summary>
        /// Initialize Skia platform.
        /// </summary>
        /// <param name="preferredBackendType">Preferred backend type - will fallback to raster if platform has not support for it..</param>
        public static void Initialize(RenderBackendType preferredBackendType)
        {
            IGpuRenderBackend renderBackend = null;
            
            // Check if platform has OpenGL support
            if (preferredBackendType == RenderBackendType.OpenGL)
            {
                var openGLPlatform = AvaloniaLocator.Current.GetService<IOpenGLPlatform>();
                var windowingPlatform = AvaloniaLocator.Current.GetService<IWindowingPlatform>();

                if (openGLPlatform != null && windowingPlatform != null)
                {
                    renderBackend = new OpenGLRenderBackend(openGLPlatform, windowingPlatform);
                }
            }

            var renderInterface = new PlatformRenderInterface(renderBackend);
            
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformRenderInterface>().ToConstant(renderInterface);
        }

        /// <summary>
        /// Default DPI.
        /// </summary>
        public static Vector DefaultDpi => new Vector(96.0f, 96.0f);
    }
}
