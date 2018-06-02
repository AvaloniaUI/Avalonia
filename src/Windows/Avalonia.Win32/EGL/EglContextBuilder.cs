// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.OpenGL;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Win32.EGL
{
    public enum EglApi
    {
        Gl,
        Gles
    }

    public class EglContextBuilder : IGlContextBuilder
    {
        private readonly IntPtr _display = IntPtr.Zero;
        private List<string> _extensions = new List<string>();
        private readonly int _eglMajor;
        private readonly int _eglMinor;
        private readonly IntPtr _configId;
        private readonly EglApi _eglApi;

        public EglContextBuilder(GlRequest request)
        {
            _display = EGL.GetDisplay((IntPtr)EGL.DEFAULT_DISPLAY);

            if (_display == IntPtr.Zero)
                throw new GlContextException("Could not create EGL display object!");

            if (!EGL.Initialize(_display, out _eglMajor, out _eglMinor))
                throw new GlContextException("Could not initialize EGL!");

            // EGL >= 1.4 lets us query extensions
            if (_eglMajor >= 1 && _eglMinor >= 4)
            {
                _extensions.AddRange(EGL.QueryString(_display, EGL.EXTENSIONS).Split(' '));
            }

            _eglApi = BindAPI(_eglMajor, _eglMinor, request);
            _configId = ChooseFbConfig(_display, _eglMajor, _eglMinor, request, _eglApi);
        }

        public IGlContext Build(IEnumerable<object> surfaces)
        {
            var hwnd = surfaces.FirstOrDefault(s => s is IPlatformHandle) as PlatformHandle;
            if (hwnd == null)
                throw new InvalidOperationException("Could not find a HWND to build a surface from!");

            // TODO: More surface attributes?
            var attributes = new int[]
            {
                EGL.NONE
            };

            var surface = EGL.CreateWindowSurface(_display, _configId, hwnd.Handle, attributes);

            // TODO: Make the GL version configurable
            return new EglContext(
                _display,
                _configId,
                new EglSurface(surface, hwnd),
                CreateContext(3, 0)
            );
        }

        private static EglApi BindAPI(int eglMajor, int eglMinor, GlRequest request)
        {
            // EGL defaults to OPENGL_ES for eglBindAPI.
            var eglApi = EglApi.Gles;

            //If we have EGL >= 1.4, we can try desktop GL if our GlRequest is set to Auto
            if (eglMajor >= 1 && eglMinor >= 4)
            {
                if (request.Api == GlApi.Auto)
                {
                    if (EGL.BindAPI(EGL.OPENGL_API))
                    {
                        eglApi = EglApi.Gl;
                    }
                    else if (EGL.BindAPI(EGL.OPENGL_ES_API))
                    {
                        eglApi = EglApi.Gles;
                    }
                    else
                    {
                        throw new GlContextException("Could not find a suitable OpenGL API to bind to!");
                    }
                }
                else if (request.Api == GlApi.Gl)
                {
                    if (!EGL.BindAPI(EGL.OPENGL_API))
                    {
                        throw new GlContextException("Could not find a suitable OpenGL API to bind to!");
                    }
                }
            }

            return eglApi;
        }

        private static IntPtr ChooseFbConfig(IntPtr display, int major, int minor, GlRequest request, EglApi api)
        {
            var attributes = new List<int>();

            if (major >= 1 && minor >= 2)
            {
                attributes.Add(EGL.COLOR_BUFFER_TYPE);
                attributes.Add(EGL.RGB_BUFFER);
            }

            attributes.Add(EGL.SURFACE_TYPE);
            attributes.Add(EGL.WINDOW_BIT);

            // Add the Renderable type and Conformant attributes based on the selected API
            if (api == EglApi.Gles)
            {
                if (major <= 1 && minor < 3)
                {
                    throw new GlContextException("No available pixel format!");
                }

                if (request.Version == GlVersion.Specific && request.GlMajor == 3)
                {
                    attributes.Add(EGL.RENDERABLE_TYPE);
                    attributes.Add(EGL.OPENGL_ES3_BIT);

                    attributes.Add(EGL.CONFORMANT);
                    attributes.Add(EGL.OPENGL_ES3_BIT);
                }
                else if (request.Version == GlVersion.Specific && request.GlMajor == 2)
                {
                    attributes.Add(EGL.RENDERABLE_TYPE);
                    attributes.Add(EGL.OPENGL_ES2_BIT);

                    attributes.Add(EGL.CONFORMANT);
                    attributes.Add(EGL.OPENGL_ES2_BIT);
                }
                else if (request.Version == GlVersion.Specific && request.GlMajor == 1)
                {
                    attributes.Add(EGL.RENDERABLE_TYPE);
                    attributes.Add(EGL.OPENGL_ES_BIT);

                    attributes.Add(EGL.CONFORMANT);
                    attributes.Add(EGL.OPENGL_ES_BIT);
                }
                else
                {
                    attributes.Add(EGL.RENDERABLE_TYPE);
                    attributes.Add(EGL.OPENGL_ES3_BIT);

                    attributes.Add(EGL.CONFORMANT);
                    attributes.Add(EGL.OPENGL_ES3_BIT);
                }
            }
            else
            {
                if (major <= 1 && minor < 3)
                {
                    throw new GlContextException("No available pixel format!");
                }

                attributes.Add(EGL.RENDERABLE_TYPE);
                attributes.Add(EGL.OPENGL_BIT);
                attributes.Add(EGL.CONFORMANT);
                attributes.Add(EGL.OPENGL_BIT);
            }

            // Use HW acceleration
            attributes.Add(EGL.CONFIG_CAVEAT);
            attributes.Add(EGL.NONE);

            attributes.Add(EGL.RED_SIZE);
            attributes.Add(8);

            attributes.Add(EGL.GREEN_SIZE);
            attributes.Add(8);

            attributes.Add(EGL.BLUE_SIZE);
            attributes.Add(8);

            attributes.Add(EGL.ALPHA_SIZE);
            attributes.Add(8);

            attributes.Add(EGL.DEPTH_SIZE);
            attributes.Add(24);

            attributes.Add(EGL.STENCIL_SIZE);
            attributes.Add(8);

            attributes.Add(EGL.NONE);

            var configIds = new IntPtr[1];
            if (!EGL.ChooseConfig(display, attributes.ToArray(), configIds, 1, out var numConfigs))
            {
                throw new GlContextException("eglChooseConfig failed!");
            }

            if (numConfigs == 0)
            {
                throw new GlContextException("No available pixel format!");
            }

            return configIds[0];
        }

        private IntPtr CreateContext(int contextMajor, int contextMinor)
        {
            var attributes = new List<int>();

            // EGL >= 1.5 or implementations with EGL_KHR_create_context can request a minor version as well
            if ((_eglMajor >= 1 && _eglMinor >= 5) || _extensions.Contains("EGL_KHR_create_context"))
            {
                attributes.Add(EGL.CONTEXT_CLIENT_VERSION);
                attributes.Add(contextMajor);
                attributes.Add(EGL.CONTEXT_MINOR_VERSION);
                attributes.Add(contextMinor);

                // TODO: Robustness
            }
            else if ((_eglMajor >= 1 && _eglMinor >= 3) && _eglApi == EglApi.Gles)
            {
                attributes.Add(EGL.CONTEXT_CLIENT_VERSION);
                attributes.Add(contextMajor);
            }

            attributes.Add(EGL.NONE);

            var context = EGL.CreateContext(_display, _configId, (IntPtr)EGL.NO_CONTEXT, attributes.ToArray());
            if (context == IntPtr.Zero)
                throw new GlContextException($"Could not create OpenGL {contextMajor}.{contextMinor} context!");

            return context;
        }
    }
}