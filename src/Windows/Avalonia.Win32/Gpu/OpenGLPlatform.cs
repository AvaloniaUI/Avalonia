// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Platform.Gpu;

namespace Avalonia.Win32.Gpu
{
    /// <summary>
    /// Win32 based OpenGL platform.
    /// </summary>
    public class OpenGLPlatform : IOpenGLPlatform
    {
        private OpenGLContext _currentContext;

        /// <inheritdoc />
        public IOpenGLContext CreateContext(IPlatformHandle platformHandle)
        {
            if (platformHandle == null)
            {
                throw new ArgumentNullException(nameof(platformHandle));
            }

            var windowInfo = OpenTK.Platform.Utilities.CreateWindowsWindowInfo(platformHandle.Handle);

            if (windowInfo == null)
            {
                throw new InvalidOperationException("Failed to create Win32 window info for platform handle.");
            }

            var context = new OpenGLContext(windowInfo);

            // Context creation sets it as current
            _currentContext = context;

            return context;
        }

        public bool IsContextCurrent(IOpenGLContext context)
        {
            return _currentContext == context;
        }

        public void MakeContextCurrent(IOpenGLContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var contextImpl = (OpenGLContext) context;

            _currentContext = contextImpl;

            contextImpl.MakeCurrent();
        }
    }
}
