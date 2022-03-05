using System;
using Avalonia.OpenGL;
using CoreAnimation;
using OpenGLES;

namespace Avalonia.iOS
{
    public class LayerFbo
    {
        private readonly EAGLContext _context;
        private readonly GlInterface _gl;
        private readonly CAEAGLLayer _layer;
        private int[] _framebuffer;
        private int[] _renderbuffer;
        private int[] _depthBuffer;
        private bool _disposed;

        private LayerFbo(EAGLContext context, GlInterface gl, CAEAGLLayer layer, int[] framebuffer, int[] renderbuffer, int[] depthBuffer)
        {
            _context = context;
            _gl = gl;
            _layer = layer;
            _framebuffer = framebuffer;
            _renderbuffer = renderbuffer;
            _depthBuffer = depthBuffer;
        }

        public static LayerFbo TryCreate(EAGLContext context, GlInterface gl, CAEAGLLayer layer)
        {
            if (context != EAGLContext.CurrentContext)
                return null;

            var fb = new int[2];
            var rb = new int[2];
            var db = new int[2];
            
            gl.GenRenderbuffers(1, rb);
            gl.BindRenderbuffer(GlConsts.GL_RENDERBUFFER,  rb[0]);
            context.RenderBufferStorage(GlConsts.GL_RENDERBUFFER, layer);

            gl.GenFramebuffers(1, fb);
            gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, fb[0]);
            gl.FramebufferRenderbuffer(GlConsts.GL_FRAMEBUFFER, GlConsts.GL_COLOR_ATTACHMENT0, GlConsts.GL_RENDERBUFFER, rb[0]);

            int[] w = new int[1];
            int[] h = new int[1];
            gl.GetRenderbufferParameteriv(GlConsts.GL_RENDERBUFFER, GlConsts.GL_RENDERBUFFER_WIDTH, w);
            gl.GetRenderbufferParameteriv(GlConsts.GL_RENDERBUFFER, GlConsts.GL_RENDERBUFFER_HEIGHT, h);
            
            gl.GenRenderbuffers(1, db);
            
            //GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            //GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferInternalFormat.DepthComponent16, w, h);
            gl.FramebufferRenderbuffer(GlConsts.GL_FRAMEBUFFER, GlConsts.GL_DEPTH_ATTACHMENT, GlConsts.GL_RENDERBUFFER, db[0]);

            var frameBufferError = gl.CheckFramebufferStatus(GlConsts.GL_FRAMEBUFFER);
            if(frameBufferError != GlConsts.GL_FRAMEBUFFER_COMPLETE)
            {
                gl.DeleteFramebuffers(1, fb);
                gl.DeleteRenderbuffers(1, db);
                gl.DeleteRenderbuffers(1, rb);
                return null;
            }

            return new LayerFbo(context, gl, layer, fb, rb, db)
            {
                Width = w[0],
                Height = h[0]
            };
        }
        
        public int Width { get; private set; }
        public int Height { get; private set; }

        public void Bind()
        {
            _gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, _framebuffer[0]);
        }

        public void Present()
        {
            Bind();
            var success = _context.PresentRenderBuffer(GlConsts.GL_RENDERBUFFER);
        }

        public void Dispose()
        {
            if(_disposed)
                return;
            _disposed = true;
            _gl.DeleteFramebuffers(1, _framebuffer);
            _gl.DeleteRenderbuffers(1, _depthBuffer);
            _gl.DeleteRenderbuffers(1, _renderbuffer);
            if (_context != EAGLContext.CurrentContext)
                throw new InvalidOperationException("Associated EAGLContext is not current");
        }
    }

    class SizeSynchronizedLayerFbo : IDisposable
    {
        private readonly EAGLContext _context;
        private readonly GlInterface _gl;
        private readonly CAEAGLLayer _layer;
        private LayerFbo _fbo;
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
            _fbo.Bind();
        }

        public void Present() => _fbo.Present();

        public int Width => _fbo?.Width ?? 0;
        public int Height => _fbo?.Height ?? 0;
        public double Scaling => _oldLayerScale;
    }
}
