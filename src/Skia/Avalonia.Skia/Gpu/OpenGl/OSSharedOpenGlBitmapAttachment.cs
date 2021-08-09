using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia
{
    internal class OSSharedOpenGlBitmapAttachment : IOpenGlBitmapAttachment, IGlPresentableOpenGlSurface
    {
        private readonly IGlContextWithOSTextureSharing _sourceContext;
        private IGlOSSharedTexture _sharedTexture;
        private int _sourceFbo;
        private readonly int _backBuffer;
        private bool _disposed;
        private readonly GlOpenGlBitmapImpl _bitmap;
        private readonly IGlContextWithOSTextureSharing _destContext;
        private readonly Action _presentCallback;

        public OSSharedOpenGlBitmapAttachment(GlOpenGlBitmapImpl bitmap, IGlContextWithOSTextureSharing sourceContext, Action presentCallback)
        {
            _destContext = (IGlContextWithOSTextureSharing)bitmap.Context;
            _sourceContext = sourceContext;
            _bitmap = bitmap;
            _presentCallback = presentCallback;
            using (sourceContext.EnsureCurrent())
            {
                var gl = _sourceContext.GlInterface;
                _sourceContext.GlInterface.GetIntegerv(GlConsts.GL_FRAMEBUFFER_BINDING, out _sourceFbo);
                _sharedTexture = _sourceContext.CreateOSSharedTexture(sourceContext, bitmap.PixelSize.Width, bitmap.PixelSize.Height);
                var tex = new int[1];
                gl.GenTextures(1, tex);
                _backBuffer = tex[0];
                

                gl.GetIntegerv(GlConsts.GL_TEXTURE_BINDING_2D, out var oldTexture);
                
                gl.BindTexture(GlConsts.GL_TEXTURE_2D, _backBuffer);
                gl.TexImage2D(GlConsts.GL_TEXTURE_2D, 0,
                    DetectTextureFormat(_sourceContext),
                    bitmap.PixelSize.Width, bitmap.PixelSize.Height,
                    0, GlConsts.GL_RGBA, GlConsts.GL_UNSIGNED_BYTE, IntPtr.Zero);
                
                gl.TexParameteri(GlConsts.GL_TEXTURE_2D, GlConsts.GL_TEXTURE_MAG_FILTER, GlConsts.GL_NEAREST);
                gl.TexParameteri(GlConsts.GL_TEXTURE_2D, GlConsts.GL_TEXTURE_MIN_FILTER, GlConsts.GL_NEAREST);

                gl.FramebufferTexture2D(GlConsts.GL_FRAMEBUFFER, GlConsts.GL_COLOR_ATTACHMENT0, GlConsts.GL_TEXTURE_2D, _backBuffer, 0);
                gl.BindTexture(GlConsts.GL_TEXTURE_2D, oldTexture);
            }
        }
        
        int DetectTextureFormat(IGlContext gl) =>
            gl.Version.Type == GlProfileType.OpenGLES ? GlConsts.GL_RGBA : GlConsts.GL_RGBA8;

        public void Dispose()
        {
            _sharedTexture?.Dispose();
            _sharedTexture = null;
            if(_disposed)
                return;
            using (_sourceContext.EnsureCurrent()) 
                _sourceContext.GlInterface.DeleteTextures(1, new[] { _backBuffer });
            _disposed = true;
        }

        private static byte[] Buffer = new byte[1024 * 1024 * 4];
        private static IntPtr BufferPointer = GCHandle.Alloc(Buffer, GCHandleType.Pinned).AddrOfPinnedObject(); 
        public unsafe void Present()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ContextSharedOpenGlBitmapAttachment));
            using (_sourceContext.EnsureCurrent())
            {
                var gl = _sourceContext.GlInterface;
                gl.Finish();
                using (_sharedTexture.Lock())
                {
                    gl.GetIntegerv(GlConsts.GL_FRAMEBUFFER_BINDING, out var oldFbo);
                    gl.GetIntegerv(GlConsts.GL_TEXTURE_BINDING_2D, out var oldTexture);
                    gl.GetIntegerv(GlConsts.GL_ACTIVE_TEXTURE, out var oldActive);

                    gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, _sourceFbo);
                    gl.BindTexture(GlConsts.GL_TEXTURE_2D, _sharedTexture.TextureId);
                    gl.ActiveTexture(GlConsts.GL_TEXTURE0);

                    gl.CopyTexSubImage2D(GlConsts.GL_TEXTURE_2D, 0, 0, 0, 0, 0, _bitmap.PixelSize.Width,
                        _bitmap.PixelSize.Height);

                    gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, oldFbo);
                    gl.BindTexture(GlConsts.GL_TEXTURE_2D, oldTexture);
                    gl.ActiveTexture(oldActive);

                    gl.Finish();
                }
            }

            _bitmap.Present(this);
            _presentCallback?.Invoke();
        }

        public ILockedPresentableOpenGlSurface LockForPresentation()
        {
            var destShared = _destContext.ImportOSSharedTexture(_sharedTexture);
            var l = destShared.Lock();
            return new LockedGlPresentableOpenGlSurface(destShared.TextureId, DetectTextureFormat(_destContext),
                destShared.Fbo,
                () =>
                {
                    l.Dispose();
                    destShared.Dispose();
                });
        }
    }
}
