using System;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.OpenGL.Imaging;
using Silk.NET.OpenGL;

namespace Avalonia.OpenGL.Controls
{
    public abstract class OpenGlControlBase : Control
    {
        private IGlContext _context;
        private uint _fb, _depthBuffer;
        private OpenGlBitmap _bitmap;
        private IOpenGlBitmapAttachment _attachment;
        private PixelSize _depthBufferSize;
        private bool _glFailed;
        private bool _initialized;
        protected GlVersion GlVersion { get; private set; }
        public sealed override void Render(DrawingContext context)
        {
            if(!EnsureInitialized())
                return;
            
            using (_context.MakeCurrent())
            {
                _context.GL.BindFramebuffer(GLEnum.Framebuffer, _fb);
                EnsureTextureAttachment();
                EnsureDepthBufferAttachment(_context.GL);
                if(!CheckFramebufferStatus(_context.GL))
                    return;
                
                OnOpenGlRender(_context.GL, _fb);
                _attachment.Present();
            }

            context.DrawImage(_bitmap, new Rect(_bitmap.Size), Bounds);
            base.Render(context);
        }
        
        private void CheckError(GL gl)
        {
            GLEnum err;
            while ((err = gl.GetError()) != GLEnum.NoError)
                Console.WriteLine(err);
        }

        void EnsureTextureAttachment()
        {
            _context.GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fb);
            if (_bitmap == null || _attachment == null || _bitmap.PixelSize != GetPixelSize())
            {
                _attachment?.Dispose();
                _attachment = null;
                _bitmap?.Dispose();
                _bitmap = null;
                _bitmap = new OpenGlBitmap(GetPixelSize(), new Vector(96, 96));
                _attachment = _bitmap.CreateFramebufferAttachment(_context);
            }
        }
        
        void EnsureDepthBufferAttachment(GL gl)
        {
            var size = GetPixelSize();
            if (size == _depthBufferSize && _depthBuffer != 0)
                return;
                    
            gl.GetInteger(GetPName.RenderbufferBinding, out var oldRenderBuffer);
            if (_depthBuffer != 0) gl.DeleteRenderbuffers(1, new[] { _depthBuffer });

            _depthBuffer = gl.GenRenderbuffer();
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthBuffer);
            gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer,
                GlVersion.Type == GlProfileType.OpenGLES ? InternalFormat.DepthComponent16 : InternalFormat.DepthComponent,
                (uint)size.Width, (uint)size.Height);
            gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _depthBuffer);
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, (uint)oldRenderBuffer);
        }

        void DoCleanup()
        {
            if (_context != null)
            {
                using (_context.MakeCurrent())
                {
                    var gl = _context.GL;
                    gl.BindTexture(TextureTarget.Texture2D, 0);
                    gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    gl.DeleteFramebuffers(1, new[] { _fb });
                    gl.DeleteRenderbuffers(1, new[] { _depthBuffer });
                    _attachment?.Dispose();
                    _attachment = null;
                    _bitmap?.Dispose();
                    _bitmap = null;
                    
                    try
                    {
                        if (_initialized)
                        {
                            _initialized = false;
                            OnOpenGlDeinit(_context.GL, _fb);
                        }
                    }
                    finally
                    {
                        _context.Dispose();
                        _context = null;
                    }
                }
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            DoCleanup();
            base.OnDetachedFromVisualTree(e);
        }

        private bool EnsureInitializedCore()
        {
            if (_context != null)
                return true;
            
            if (_glFailed)
                return false;
            
            var feature = AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>();
            if (feature == null)
                return false;
            if (!feature.CanShareContexts)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Unable to initialize OpenGL: current platform does not support multithreaded context sharing");
                return false;
            }
            try
            {
                _context = feature.CreateSharedContext();
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Unable to initialize OpenGL: unable to create additional OpenGL context: {exception}", e);
                return false;
            }

            GlVersion = _context.Version;
            try
            {
                _bitmap = new OpenGlBitmap(GetPixelSize(), new Vector(96, 96));
                if (!_bitmap.SupportsContext(_context))
                {
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                        "Unable to initialize OpenGL: unable to create OpenGlBitmap: OpenGL context is not compatible");
                    return false;
                }
            }
            catch (Exception e)
            {
                _context.Dispose();
                _context = null;
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Unable to initialize OpenGL: unable to create OpenGlBitmap: {exception}", e);
                return false;
            }

            using (_context.MakeCurrent())
            {
                try
                {
                    _depthBufferSize = GetPixelSize();
                    var gl = _context.GL;
                    _fb = gl.GenFramebuffer();
                    gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fb);
                    
                    EnsureDepthBufferAttachment(gl);
                    EnsureTextureAttachment();

                    return CheckFramebufferStatus(gl);
                }
                catch(Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                        "Unable to initialize OpenGL FBO: {exception}", e);
                    return false;
                }
            }
        }

        private bool CheckFramebufferStatus(GL gl)
        {
            var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
            {
                GLEnum code;
                while ((code = gl.GetError()) != 0)
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                        "Unable to initialize OpenGL FBO: {code}", code);
                return false;
            }

            return true;
        }

        private bool EnsureInitialized()
        {
            if (_initialized)
                return true;
            _glFailed = !(_initialized = EnsureInitializedCore());
            if (_glFailed)
                return false;
            using (_context.MakeCurrent())
                OnOpenGlInit(_context.GL, _fb);
            return true;
        }
        
        private PixelSize GetPixelSize()
        {
            var scaling = VisualRoot.RenderScaling;
            return new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)),
                Math.Max(1, (int)(Bounds.Height * scaling)));
        }


        protected virtual void OnOpenGlInit(GL gl, uint fb)
        {
            
        }

        protected virtual void OnOpenGlDeinit(GL gl, uint fb)
        {
            
        }
        
        protected abstract void OnOpenGlRender(GL gl, uint fb);
    }
}
