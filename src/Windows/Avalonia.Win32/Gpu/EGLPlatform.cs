// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Platform.Gpu;

namespace Avalonia.Win32.Gpu
{
    /// <summary>
    /// Win32 based OpenGL platform.
    /// </summary>
    public class EGLPlatform : IEGLPlatform
    {
        private static bool s_isInitialized;
        private static IntPtr s_context;
        private static IntPtr s_display;
        private static IntPtr s_config;
        
        /// <summary>
        /// Ensure that EGL was initialized.
        /// </summary>
        private void EnsureInitialized()
        {
            if (s_isInitialized)
            {
                return;
            }

            // Check if user provided any hooks.
            var platformHooks = AvaloniaLocator.Current.GetService<IEGLPlatformHooks>();

            var platformType = EGLPlatformType.Default;

            platformHooks?.InspectPlatformType(ref platformType);
            
            // Try to create display
            IntPtr display;

            if (platformType != EGLPlatformType.Default)
            {
                int platformId;

                switch (platformType)
                {
                    case EGLPlatformType.D3D9:
                        platformId = EGL.PLATFORM_ANGLE_TYPE_D3D9_ANGLE;
                        break;
                    case EGLPlatformType.D3D11:
                        platformId = EGL.PLATFORM_ANGLE_TYPE_D3D11_ANGLE;
                        break;
                    case EGLPlatformType.OpenGL:
                        platformId = EGL.PLATFORM_ANGLE_TYPE_OPENGL_ANGLE;
                        break;
                    case EGLPlatformType.OpenGL_ES:
                        platformId = EGL.PLATFORM_ANGLE_TYPE_OPENGLES_ANGLE;
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid EGL platform specified: {platformType}");
                }

                var displayAttributes = new[]
                {
                    EGL.PLATFORM_ANGLE_TYPE_ANGLE, platformId,
                    EGL.NONE
                };

                display = EGL.GetPlatformDisplayEXT(EGL.PLATFORM_ANGLE_ANGLE, (IntPtr)EGL.DEFAULT_DISPLAY, displayAttributes);
            }
            else
            {
                display = EGL.GetDisplay((IntPtr)EGL.DEFAULT_DISPLAY);
            }

            if (display == (IntPtr)EGL.NO_DISPLAY)
            {
                throw new InvalidOperationException($"Failed to create display for EGL platform: {platformType}");
            }

            var initialized = EGL.Initialize(display, out int majorVersion, out int minorVersion);

            if (!initialized)
            {
                throw new InvalidOperationException($"Failed to initialize EGL platform: {platformType}");
            }

            platformHooks?.InspectVersion(majorVersion, minorVersion);

            // Load OpenGL through EGL
            GL.Initialize(EGL.GetProcAddress);

            var configAttribs = new[]
            {
                EGL.SURFACE_TYPE, EGL.WINDOW_BIT,
                EGL.RENDERABLE_TYPE, EGL.OPENGL_ES2_BIT,
                EGL.RED_SIZE, 8,
                EGL.GREEN_SIZE, 8,
                EGL.BLUE_SIZE, 8,
                EGL.ALPHA_SIZE, 8,
                EGL.NONE
            };

            var configs = new IntPtr[1];
            var configFound = EGL.ChooseConfig(display, configAttribs, configs, configs.Length, out int numConfigs);

            if (!configFound || numConfigs == 0)
            {
                var errorCode = EGL.GetError();
                
                throw new InvalidOperationException($"Failed to find config for EGL platform: {platformType}. Error: {errorCode}");
            }

            s_config = configs[0];

            var contextAttribs = new[]
            {
                EGL.CONTEXT_CLIENT_VERSION, 2,
                EGL.NONE
            };

            var context = EGL.CreateContext(display, configs[0], (IntPtr)EGL.NO_CONTEXT, contextAttribs);

            if (context == (IntPtr)EGL.NO_CONTEXT)
            {
                var errorCode = EGL.GetError();
                
                throw new InvalidOperationException($"Failed to create context for EGL platform: {platformType}. Error: {errorCode}");
            }

            var isCurrent = EGL.MakeCurrent(display, (IntPtr) EGL.NO_SURFACE, (IntPtr) EGL.NO_SURFACE, context);
            
            if (!isCurrent)
            {
                // TODO: Destroy context
                var errorCode = EGL.GetError();
                
                throw new InvalidOperationException($"Failed to make EGL context current. Error: {errorCode}");
            }

            s_display = display;
            s_context = context;
            s_isInitialized = true;
        }

        public IEGLSurface CreateSurface(IEnumerable<object> surfaces)
        {
            var platformHandle = surfaces.OfType<IPlatformHandle>().FirstOrDefault();

            if (platformHandle == null)
            {
                return null;
            }
            
            var surfaceAttribs = new[]
            {
                EGL.NONE
            };

            var surfaceHandle = EGL.CreateWindowSurface(s_display, s_config, platformHandle.Handle, surfaceAttribs);
            var wasCreated = surfaceHandle != (IntPtr) EGL.NO_SURFACE;

            if (!wasCreated)
            {
                var errorCode = EGL.GetError();

                Logger.Warning(LogArea.Visual, this, "Failed to create EGL surface. Error {errorCode}", errorCode);
            }
            
            EGL.GetConfigAttrib(s_display, s_config, EGL.STENCIL_SIZE, out int stencilBits);
            EGL.GetConfigAttrib(s_display, s_config, EGL.SAMPLES, out int sampleCount);
            
            return wasCreated ? new EGLSurface(surfaceHandle, platformHandle, stencilBits, sampleCount) : null;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            EnsureInitialized();
        }

        /// <inheritdoc />
        public bool MakeCurrent(IEGLSurface surface)
        {
            var surfaceImpl = (EGLSurface)surface;
            var surfaceHandle = surfaceImpl?.SurfaceHandle ?? (IntPtr)EGL.NO_SURFACE;
            
            var isOk = EGL.MakeCurrent(s_display, surfaceHandle, surfaceHandle, s_context);

            if (!isOk)
            {
                var code = EGL.GetError();

                Logger.Warning(LogArea.Visual, this, "Failed to make context current. Error: {code}", code);
            }

            return isOk;
        }

        /// <inheritdoc />
        public bool SwapBuffers(IEGLSurface surface)
        {
            var surfaceImpl = (EGLSurface)surface;
            
            var isOk = EGL.SwapBuffers(s_display, surfaceImpl.SurfaceHandle);

            if (!isOk)
            {
                var code = EGL.GetError();

                Logger.Warning(LogArea.Visual, this, "Failed to swap buffers. Error: {code}", code);
            }

            return isOk;
        }

        /// <inheritdoc />
        public void DestroySurface(IEGLSurface surface)
        {
            if (surface == null)
            {
                return;
            }

            var surfaceImpl = (EGLSurface)surface;

            var wasDestroyed = EGL.DestroySurface(s_display, surfaceImpl.SurfaceHandle);

            if (!wasDestroyed)
            {
                var error = EGL.GetError();

                Logger.Warning(LogArea.Visual, this, "Failed to destroy EGL surface with handle {handle}. Error: {error}", surfaceImpl.SurfaceHandle, error);
            }
        }
    }
}
