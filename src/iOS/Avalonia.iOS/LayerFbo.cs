using System;
using CoreAnimation;
using OpenGLES;
using OpenTK.Graphics.ES20;

namespace Avalonia.iOS
{
    public class LayerFbo
    {
        private readonly EAGLContext _context;
        private readonly CAEAGLLayer _layer;
        private int _framebuffer;
        private int _renderbuffer;
        private int _depthBuffer;
        private bool _disposed;

        private LayerFbo(EAGLContext context, CAEAGLLayer layer, in int framebuffer, in int renderbuffer, in int depthBuffer)
        {
            _context = context;
            _layer = layer;
            _framebuffer = framebuffer;
            _renderbuffer = renderbuffer;
            _depthBuffer = depthBuffer;
        }

        public static LayerFbo TryCreate(EAGLContext context, CAEAGLLayer layer)
        {
            if (context != EAGLContext.CurrentContext)
                return null;
            GL.GenFramebuffers(1, out int fb);
            GL.GenRenderbuffers(1, out int rb);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fb);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer,  rb);
            context.RenderBufferStorage((uint) All.Renderbuffer, layer);
            
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.ColorAttachment0, RenderbufferTarget.Renderbuffer, rb);

            int w;
            int h;
            GL.GetRenderbufferParameter(RenderbufferTarget.Renderbuffer, RenderbufferParameterName.RenderbufferWidth, out w);
            GL.GetRenderbufferParameter(RenderbufferTarget.Renderbuffer, RenderbufferParameterName.RenderbufferHeight, out h);
            
            GL.GenRenderbuffers(1, out int depthBuffer);
            
            //GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            //GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferInternalFormat.DepthComponent16, w, h);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferSlot.DepthAttachment, RenderbufferTarget.Renderbuffer, depthBuffer);

            var frameBufferError = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if(frameBufferError != FramebufferErrorCode.FramebufferComplete)
            {
                GL.DeleteFramebuffers(1, ref fb);
                GL.DeleteRenderbuffers(1, ref depthBuffer);
                GL.DeleteRenderbuffers(1, ref rb);
                return null;
            }

            return new LayerFbo(context, layer, fb, rb, depthBuffer)
            {
                Width = w,
                Height = h
            };
        }
        
        public int Width { get; private set; }
        public int Height { get; private set; }

        public void Bind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        }

        public void Present()
        {
            Bind();
            var success = _context.PresentRenderBuffer((uint) All.Renderbuffer);
        }

        public void Dispose()
        {
            if(_disposed)
                return;
            _disposed = true;
            GL.DeleteFramebuffers(1, ref _framebuffer);
            GL.DeleteRenderbuffers(1, ref _depthBuffer);
            GL.DeleteRenderbuffers(1, ref _renderbuffer);
            if (_context != EAGLContext.CurrentContext)
                throw new InvalidOperationException("Associated EAGLContext is not current");
        }
    }

    class SizeSynchronizedLayerFbo : IDisposable
    {
        private readonly EAGLContext _context;
        private readonly CAEAGLLayer _layer;
        private LayerFbo _fbo;
        private nfloat _oldLayerWidth, _oldLayerHeight, _oldLayerScale;
        
        public SizeSynchronizedLayerFbo(EAGLContext context, CAEAGLLayer layer)
        {
            _context = context;
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
            _fbo = LayerFbo.TryCreate(_context, _layer);
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