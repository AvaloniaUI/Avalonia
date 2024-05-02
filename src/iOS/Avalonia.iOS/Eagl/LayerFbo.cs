using System;
using System.Runtime.Versioning;
using Avalonia.OpenGL;
using CoreAnimation;
using OpenGLES;

namespace Avalonia.iOS.Eagl
{
    [ObsoletedOSPlatform("ios12.0", "Use 'Metal' instead.")]
    [ObsoletedOSPlatform("tvos12.0", "Use 'Metal' instead.")]
    [SupportedOSPlatform("ios")]
    [SupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("maccatalyst")]
    internal class LayerFbo
    {
        private readonly EAGLContext _context;
        private readonly GlInterface _gl;
        private readonly CAEAGLLayer _layer;
        private int _framebuffer;
        private int _renderbuffer;
        private int _depthBuffer;
        private bool _disposed;

        private LayerFbo(EAGLContext context, GlInterface gl, CAEAGLLayer layer, int framebuffer, int renderbuffer, int depthBuffer)
        {
            _context = context;
            _gl = gl;
            _layer = layer;
            _framebuffer = framebuffer;
            _renderbuffer = renderbuffer;
            _depthBuffer = depthBuffer;
        }

        public static LayerFbo? TryCreate(EAGLContext context, GlInterface gl, CAEAGLLayer layer)
        {
            if (context != EAGLContext.CurrentContext)
                return null;

            var rb = gl.GenRenderbuffer();
            gl.BindRenderbuffer(GlConsts.GL_RENDERBUFFER,  rb);
            context.RenderBufferStorage(GlConsts.GL_RENDERBUFFER, layer);

            var fb = gl.GenFramebuffer();
            gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, fb);
            gl.FramebufferRenderbuffer(GlConsts.GL_FRAMEBUFFER, GlConsts.GL_COLOR_ATTACHMENT0, GlConsts.GL_RENDERBUFFER, rb);
            
            gl.GetRenderbufferParameteriv(GlConsts.GL_RENDERBUFFER, GlConsts.GL_RENDERBUFFER_WIDTH, out var w);
            gl.GetRenderbufferParameteriv(GlConsts.GL_RENDERBUFFER, GlConsts.GL_RENDERBUFFER_HEIGHT, out var h);

            var db = gl.GenRenderbuffer();
            
            //GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            //GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferInternalFormat.DepthComponent16, w, h);
            gl.FramebufferRenderbuffer(GlConsts.GL_FRAMEBUFFER, GlConsts.GL_DEPTH_ATTACHMENT, GlConsts.GL_RENDERBUFFER, db);

            var frameBufferError = gl.CheckFramebufferStatus(GlConsts.GL_FRAMEBUFFER);
            if(frameBufferError != GlConsts.GL_FRAMEBUFFER_COMPLETE)
            {
                gl.DeleteFramebuffer(fb);
                gl.DeleteRenderbuffer(db);
                gl.DeleteRenderbuffer(rb);
                return null;
            }

            return new LayerFbo(context, gl, layer, fb, rb, db)
            {
                Width = w,
                Height = h
            };
        }
        
        public int Width { get; private set; }
        public int Height { get; private set; }

        public void Bind()
        {
            _gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, _framebuffer);
        }

        public void Present()
        {
            Bind();
            _context.PresentRenderBuffer(GlConsts.GL_RENDERBUFFER);
        }

        public void Dispose()
        {
            if(_disposed)
                return;
            _disposed = true;
            _gl.DeleteFramebuffer(_framebuffer);
            _gl.DeleteRenderbuffer(_depthBuffer);
            _gl.DeleteRenderbuffer(_renderbuffer);
            if (_context != EAGLContext.CurrentContext)
                throw new InvalidOperationException("Associated EAGLContext is not current");
        }
    }

    [ObsoletedOSPlatform("ios12.0", "Use 'Metal' instead.")]
    [ObsoletedOSPlatform("tvos12.0", "Use 'Metal' instead.")]
    [SupportedOSPlatform("ios")]
    [SupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("maccatalyst")]
    class SizeSynchronizedLayerFbo : IDisposable
    {
        private readonly EAGLContext _context;
        private readonly GlInterface _gl;
        private readonly CAEAGLLayer _layer;
        private LayerFbo? _fbo;
        private double _oldLayerWidth, _oldLayerHeight, _oldLayerScale;
        
        public SizeSynchronizedLayerFbo(EAGLContext context, GlInterface gl, CAEAGLLayer layer)
        {
            _context = context;
            _gl = gl;
            _layer = layer;
        }

        public bool Sync()
        {
            if (_fbo != null 
                && _oldLayerWidth == _layer.Bounds.Width
                && _oldLayerHeight == _layer.Bounds.Height
                && _oldLayerScale == _layer.ContentsScale)
                return true;
            _fbo?.Dispose();
            _fbo = null;
            _fbo = LayerFbo.TryCreate(_context, _gl, _layer);
            _oldLayerWidth = _layer.Bounds.Width;
            _oldLayerHeight = _layer.Bounds.Height;
            _oldLayerScale = _layer.ContentsScale;
            return _fbo != null;
        }

        public void Dispose()
        {
            if (_context != EAGLContext.CurrentContext)
                throw new InvalidOperationException("Associated EAGLContext is not current");
            _fbo?.Dispose();
            _fbo = null;
        }

        public void Bind()
        {
            if(!Sync())
                throw new InvalidOperationException("Unable to create a render target");
            _fbo!.Bind();
        }

        public void Present() => _fbo!.Present();

        public int Width => _fbo?.Width ?? 0;
        public int Height => _fbo?.Height ?? 0;
        public double Scaling => _oldLayerScale;
    }
}
