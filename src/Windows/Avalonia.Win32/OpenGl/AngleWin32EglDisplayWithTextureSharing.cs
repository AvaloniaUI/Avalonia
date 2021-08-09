using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using static Avalonia.OpenGL.Egl.EglConsts;

namespace Avalonia.Win32.OpenGl
{
    public class AngleWin32EglDisplayWithTextureSharing : AngleWin32EglDisplay, IEglDisplayWithOSTextureSharing
    {
        private const string DXGITexture2DSharedHandle = "DXGITexture2DSharedHandle";
        public IGlOSSharedTexture CreateOSSharedTexture(EglContext ctx, string type, int width, int height)
        {
            throw new NotImplementedException();
        }
        
        public IGlOSSharedTexture CreateOSSharedTexture(EglContext ctx, IGlContext compatibleWith, int width, int height)
        {
            throw new NotImplementedException();
        }

        public IGlContextWithOSTextureSharing CreateOSTextureSharingCompatibleContext(EglContext compatibleWith, IGlContext shareWith, IList<GlVersion> probeVersions) 
            => WglDisplay.CreateContext(probeVersions.ToArray(), shareWith);

        public bool SupportsOSSharedTextureType(EglContext ctx, string type) => type == DXGITexture2DSharedHandle;

        public IGlOSSharedTexture ImportOSSharedTexture(EglContext ctx, IGlOSSharedTexture osSharedTexture)
        {
            if (osSharedTexture is IPlatformHandle shareHandle &&
                shareHandle.HandleDescriptor == DXGITexture2DSharedHandle)
            {
                return new AngleDxgiSharedTexture(ctx, shareHandle.Handle, osSharedTexture.Width,
                    osSharedTexture.Height);

            }
            throw new NotImplementedException();
        }

        public bool AreOSTextureSharingCompatible(EglContext ctx, IGlContext compatibleWith) =>
            compatibleWith is IGlContextWithOSTextureSharing sharing &&
            sharing.SupportsOSSharedTextureType(DXGITexture2DSharedHandle);
    }
}
