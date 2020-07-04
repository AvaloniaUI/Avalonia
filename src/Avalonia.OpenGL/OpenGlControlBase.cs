using System;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.OpenGL.Imaging;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.OpenGL
{
    public abstract class OpenGlControlBase : Control
    {
        private IGlContext _context;
        private int _fb, _texture, _renderBuffer;
        private OpenGlTextureBitmap _bitmap;
        private PixelSize _oldSize;
        private bool _glFailed;
        protected GlVersion GlVersion { get; private set; }
        public sealed override void Render(DrawingContext context)
        {
            if(!EnsureInitialized())
                return;
                
            using (_context.MakeCurrent())
            {
                using (_bitmap.Lock())
                {
                    var gl = _context.GlInterface;
                    gl.BindFramebuffer(GL_FRAMEBUFFER, _fb);
                    if (_oldSize != GetPixelSize())
                        ResizeTexture(gl);

                    OnOpenGlRender(gl, _fb);
                    gl.Flush();
                }
            }

            context.DrawImage(_bitmap, new Rect(_bitmap.Size), Bounds);
            base.Render(context);
        }

        void DoCleanup(bool callUserDeinit)
        {
            if (_context != null)
            {
                using (_context.MakeCurrent())
                {
                    var gl = _context.GlInterface;
                    gl.BindTexture(GL_TEXTURE_2D, 0);
                    gl.BindFramebuffer(GL_FRAMEBUFFER, 0);
                    gl.DeleteFramebuffers(1, new[] { _fb });
                    using (_bitmap.Lock()) 
                        _bitmap.SetTexture(0, 0, new PixelSize(1, 1), 1);
                    gl.DeleteTextures(1, new[] { _texture });
                    gl.DeleteRenderbuffers(1, new[] { _renderBuffer });
                    _bitmap.Dispose();
                    
                    try
                    {
                        if (callUserDeinit)
                            OnOpenGlDeinit(_context.GlInterface, _fb);
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
            DoCleanup(true);
            base.OnDetachedFromVisualTree(e);
        }

        bool EnsureInitialized()
        {
            if (_context != null)
                return true;
            
            if (_glFailed)
                return false;
            
            var feature = AvaloniaLocator.Current.GetService<IWindowingPlatformGlFeature>();
            if (feature == null)
                return false;
            try
            {
                _context = feature.CreateContext();

            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Unable to initialize OpenGL: unable to create additional OpenGL context: {exception}", e);
                _glFailed = true;
                return false;
            }

            GlVersion = _context.Version;
            try
            {
                _bitmap = new OpenGlTextureBitmap();
            }
            catch (Exception e)
            {
                _context.Dispose();
                _context = null;
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Unable to initialize OpenGL: unable to create OpenGlTextureBitmap: {exception}", e);
                _glFailed = true;
                return false;
            }

            using (_context.MakeCurrent())
            {
                try
                {
                    _oldSize = GetPixelSize();
                    var gl = _context.GlInterface;
                    var oneArr = new int[1];
                    gl.GenFramebuffers(1, oneArr);
                    _fb = oneArr[0];
                    gl.BindFramebuffer(GL_FRAMEBUFFER, _fb);

                    gl.GenTextures(1, oneArr);
                    _texture = oneArr[0];
                    
                    ResizeTexture(gl);

                    gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, _texture, 0);

                    var status = gl.CheckFramebufferStatus(GL_FRAMEBUFFER);
                    if (status != GL_FRAMEBUFFER_COMPLETE)
                    {
                        int code;
                        while ((code = gl.GetError()) != 0)
                            Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                                "Unable to initialize OpenGL FBO: {code}", code);

                        _glFailed = true;
                        return false;
                    }
                }
                catch(Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                        "Unable to initialize OpenGL FBO: {exception}", e);
                    _glFailed = true;
                }

                if (!_glFailed)
                    OnOpenGlInit(_context.GlInterface, _fb);
            }

            if (_glFailed)
            {
                DoCleanup(false);
            }

            return true;
        }

        void ResizeTexture(GlInterface gl)
        {
            var size = GetPixelSize();

            gl.GetIntegerv( GL_TEXTURE_BINDING_2D, out var oldTexture);
            gl.BindTexture(GL_TEXTURE_2D, _texture);
            gl.TexImage2D(GL_TEXTURE_2D, 0, 
                GlVersion.Type == GlProfileType.OpenGLES ? GL_RGBA : GL_RGBA8,
                size.Width, size.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
            gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
            gl.BindTexture(GL_TEXTURE_2D, oldTexture);

            gl.GetIntegerv(GL_RENDERBUFFER_BINDING, out var oldRenderBuffer);
            gl.DeleteRenderbuffers(1, new[] { _renderBuffer });
            var oneArr = new int[1];
            gl.GenRenderbuffers(1, oneArr);
            _renderBuffer = oneArr[0];
            gl.BindRenderbuffer(GL_RENDERBUFFER, _renderBuffer);
            gl.RenderbufferStorage(GL_RENDERBUFFER,
                GlVersion.Type == GlProfileType.OpenGLES ? GL_DEPTH_COMPONENT16 : GL_DEPTH_COMPONENT,
                size.Width, size.Height);
            gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, _renderBuffer);
            gl.BindRenderbuffer(GL_RENDERBUFFER, oldRenderBuffer);
            using (_bitmap.Lock())
                _bitmap.SetTexture(_texture, GL_RGBA8, size, 1);
        }
        
        PixelSize GetPixelSize()
        {
            var scaling = VisualRoot.RenderScaling;
            return new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)),
                Math.Max(1, (int)(Bounds.Height * scaling)));
        }


        protected virtual void OnOpenGlInit(GlInterface gl, int fb)
        {
            
        }

        protected virtual void OnOpenGlDeinit(GlInterface gl, int fb)
        {
            
        }
        
        protected abstract void OnOpenGlRender(GlInterface gl, int fb);
    }
}
