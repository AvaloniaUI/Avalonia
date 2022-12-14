using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.OpenGL.Imaging;
using Avalonia.VisualTree;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.OpenGL.Controls
{
    public abstract class OpenGlControlBase : Control
    {
        private IGlContext _context;
        private int _fb, _depthBuffer;
        private OpenGlBitmap _bitmap;
        private IOpenGlBitmapAttachment _attachment;
        private PixelSize _depthBufferSize;
        
        private Task<bool> _initialization;
        private IOpenGlTextureSharingRenderInterfaceContextFeature _feature;

        protected GlVersion GlVersion { get; private set; }
        public sealed override void Render(DrawingContext context)
        {
            if(!EnsureInitialized())
                return;
            
            using (_context.MakeCurrent())
            {
                _context.GlInterface.BindFramebuffer(GL_FRAMEBUFFER, _fb);
                EnsureTextureAttachment();
                EnsureDepthBufferAttachment(_context.GlInterface);
                if(!CheckFramebufferStatus(_context.GlInterface))
                    return;
                
                OnOpenGlRender(_context.GlInterface, _fb);
                _attachment.Present();
            }

            context.DrawImage(_bitmap, new Rect(_bitmap.Size), new Rect(Bounds.Size));
            base.Render(context);
        }
        
        void EnsureTextureAttachment()
        {
            _context.GlInterface.BindFramebuffer(GL_FRAMEBUFFER, _fb);
            if (_bitmap == null || _attachment == null || _bitmap.PixelSize != GetPixelSize())
            {
                _attachment?.Dispose();
                _attachment = null;
                _bitmap?.Dispose();
                _bitmap = null;
                _bitmap = new OpenGlBitmap(_feature, GetPixelSize(), new Vector(96, 96));
                _attachment = _bitmap.CreateFramebufferAttachment(_context);
            }
        }
        
        void EnsureDepthBufferAttachment(GlInterface gl)
        {
            var size = GetPixelSize();
            if (size == _depthBufferSize && _depthBuffer != 0)
                return;
                    
            gl.GetIntegerv(GL_RENDERBUFFER_BINDING, out var oldRenderBuffer);
            if (_depthBuffer != 0) gl.DeleteRenderbuffer(_depthBuffer);

            _depthBuffer = gl.GenRenderbuffer();
            gl.BindRenderbuffer(GL_RENDERBUFFER, _depthBuffer);
            gl.RenderbufferStorage(GL_RENDERBUFFER,
                GlVersion.Type == GlProfileType.OpenGLES ? GL_DEPTH_COMPONENT16 : GL_DEPTH_COMPONENT,
                size.Width, size.Height);
            gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, _depthBuffer);
            gl.BindRenderbuffer(GL_RENDERBUFFER, oldRenderBuffer);
        }

        void DoCleanup()
        {
            if (_context != null)
            {
                using (_context.MakeCurrent())
                {
                    var gl = _context.GlInterface;
                    gl.ActiveTexture(GL_TEXTURE0);
                    gl.BindTexture(GL_TEXTURE_2D, 0);
                    gl.BindFramebuffer(GL_FRAMEBUFFER, 0);
                    if (_fb != 0)
                        gl.DeleteFramebuffer(_fb);
                    _fb = 0;
                    if (_depthBuffer != 0)
                        gl.DeleteRenderbuffer(_depthBuffer);
                    _depthBuffer = 0;
                    _attachment?.Dispose();
                    _attachment = null;
                    _bitmap?.Dispose();
                    _bitmap = null;
                    
                    try
                    {
                        if (_initialization is { Status: TaskStatus.RanToCompletion, Result: true })
                        {
                            OnOpenGlDeinit(_context.GlInterface, _fb);
                            _initialization = null;
                        }
                    }
                    finally
                    {
                        _context.Dispose();
                        _context = null;
                    }
                }
            }

            _fb = _depthBuffer = 0;
            _attachment = null;
            _bitmap = null;
            _feature = null;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            DoCleanup();
            base.OnDetachedFromVisualTree(e);
        }

        private bool EnsureInitializedCore(IOpenGlTextureSharingRenderInterfaceContextFeature feature)
        {
            try
            {
                _context = feature.CreateSharedContext();
                _feature = feature;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Unable to initialize OpenGL: unable to create additional OpenGL context: {exception}", e);
                return false;
            }

            if (_context == null)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Unable to initialize OpenGL: unable to create additional OpenGL context.");
                return false;
            }

            GlVersion = _context.Version;
            try
            {
                _bitmap = new OpenGlBitmap(_feature, GetPixelSize(), new Vector(96, 96));
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
                    var gl = _context.GlInterface;
                    _fb = gl.GenFramebuffer();
                    gl.BindFramebuffer(GL_FRAMEBUFFER, _fb);
                    
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

        private static bool CheckFramebufferStatus(GlInterface gl)
        {
            var status = gl.CheckFramebufferStatus(GL_FRAMEBUFFER);
            if (status != GL_FRAMEBUFFER_COMPLETE)
            {
                int code;
                while ((code = gl.GetError()) != 0)
                    Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                        "Unable to initialize OpenGL FBO: {code}", code);
                return false;
            }

            return true;
        }

        void ContextLost()
        {
            _context = null;
            _feature = null;
            _initialization = null;
            _attachment = null;
            _bitmap = null;
            _fb = 0;
            _depthBuffer = 0;
            _depthBufferSize = default;
            OnOpenGlLost();
        }

        private bool EnsureInitialized()
        {
            if (_initialization != null)
            {
                // Check if we've previously failed to initialize OpenGL on this platform
                if (_initialization is { IsCompleted: true, Result: false } ||
                    _initialization?.IsFaulted == true)
                    return false;

                // Check if we are still waiting for init to complete
                if (_initialization is { IsCompleted: false })
                    return false;

                if (_context.IsLost)
                    ContextLost();
                else 
                    return true;
            }

            _initialization = InitializeAsync();
            return false;

        }

        private async Task<bool> InitializeAsync()
        {
            var contextSharingFeature =
                (IOpenGlTextureSharingRenderInterfaceContextFeature)
                await this.GetVisualRoot()!.Renderer.TryGetRenderInterfaceFeature(
                    typeof(IOpenGlTextureSharingRenderInterfaceContextFeature));

            if (contextSharingFeature == null || !contextSharingFeature.CanCreateSharedContext)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("OpenGlControlBase",
                    "Unable to initialize OpenGL: current platform does not support multithreaded context sharing");
                return false;
            }

            if (!EnsureInitializedCore(contextSharingFeature))
            {
                DoCleanup();
                return false;
            }

            using (_context.MakeCurrent())
                OnOpenGlInit(_context.GlInterface, _fb);

            InvalidateVisual();
            
            return true;
        }
        
        private PixelSize GetPixelSize()
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
        
        protected virtual void OnOpenGlLost()
        {
            
        }
        
        protected abstract void OnOpenGlRender(GlInterface gl, int fb);
    }
}
