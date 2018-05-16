// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Logging;
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
        /// Cpu based backend.
        /// </summary>
        Cpu,

        /// <summary>
        /// Gpu based backend.
        /// </summary>
        Gpu
    }

    /// <summary>
    /// Skia platform initializer.
    /// </summary>
    public static class SkiaPlatform
    {
        /// <summary>
        /// Initialize Skia platform.
        /// </summary>
        /// <param name="preferredBackendType">Preferred backend type - will fallback to cpu if platform has not support for it.</param>
        public static void Initialize(RenderBackendType preferredBackendType = RenderBackendType.Cpu)
        {
            IGpuRenderBackend renderBackend = null;

            Logger.Information(LogArea.Visual, null, "SkiaRuntime initializing with backend: {backendType}", preferredBackendType);

            if (preferredBackendType == RenderBackendType.Gpu)
            {
                var eglPlatform = AvaloniaLocator.Current.GetService<IEGLPlatform>();

                if (eglPlatform != null)
                {
                    try
                    {
                        eglPlatform.Initialize();

                        renderBackend = new EGLRenderBackend(eglPlatform);
                    }
                    catch (Exception e)
                    {
                        Logger.Warning(LogArea.Visual, null, "Failed to start EGL platform due to {e}", e);
                    }
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
