using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.Rendering.Composition;
using ControlCatalog.Pages.OpenGl;
using static Avalonia.OpenGL.GlConsts;

namespace ControlCatalog.Pages;

/// <summary>
/// Demonstrates the ICompositionGlContext API: an OpenGL context compatible with the compositor
/// and a texture presented to a CompositionDrawingSurface, without using OpenGlControlBase.
/// </summary>
public partial class OpenGlInteropPage : ContentPage
{
    private ICompositionGlContext? _context;
    private ICompositionGlTexture? _texture;
    private Task? _lastPresent;
    private CompositionDrawingSurface? _surface;
    private CompositionSurfaceVisual? _visual;
    private OpenGlContent? _content;
    private int _fbo;
    private int _depthBuffer;
    private PixelSize _depthBufferSize;
    private bool _updateQueued;
    private bool _attached;
    private int _generation;

    public OpenGlInteropPage()
    {
        InitializeComponent();
    }

    private void ViewportAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _attached = true;
        Initialize();
    }

    private void ViewportDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        Cleanup();
    }

    private async void Initialize()
    {
        var generation = ++_generation;
        if (ElementComposition.GetElementVisual(Viewport) is not { } elementVisual)
            return;
        var compositor = elementVisual.Compositor;

        var context = await compositor.TryCreateCompatibleGlContextAsync();
        if (context == null)
        {
            Knobs.Info = "Compositor OpenGL interop is not available on this platform";
            return;
        }

        if (!_attached || generation != _generation)
        {
            // The viewport got detached or reinitialized while we were awaiting
            await context.DisposeAsync();
            return;
        }

        _context = context;
        _surface = compositor.CreateDrawingSurface();
        _visual = compositor.CreateSurfaceVisual();
        _visual.Surface = _surface;
        ElementComposition.SetElementChildVisual(Viewport, _visual);

        using (context.GlContext.MakeCurrent())
        {
            var gl = context.GlContext.GlInterface;
            _fbo = gl.GenFramebuffer();
            _content = new OpenGlContent();
            _content.Init(gl, context.GlContext.Version);
        }

        Knobs.Info = _content.Info;
        Viewport.SizeChanged += OnViewportSizeChanged;
        QueueUpdate();
    }

    private async void Cleanup()
    {
        _generation++;
        Viewport.SizeChanged -= OnViewportSizeChanged;
        ElementComposition.SetElementChildVisual(Viewport, null);
        _visual = null;

        var context = _context;
        _context = null;
        if (context == null)
            return;

        try
        {
            using (context.GlContext.MakeCurrent())
            {
                var gl = context.GlContext.GlInterface;
                _content?.Deinit(gl);
                if (_fbo != 0)
                    gl.DeleteFramebuffer(_fbo);
                if (_depthBuffer != 0)
                    gl.DeleteRenderbuffer(_depthBuffer);
            }
        }
        catch
        {
            // The context is likely lost
        }

        _content = null;
        _fbo = 0;
        _depthBuffer = 0;
        _depthBufferSize = default;
        _texture = null;
        _lastPresent = null;
        _surface?.Dispose();
        _surface = null;

        // Also disposes the textures created from it
        await context.DisposeAsync();
    }

    private void OnViewportSizeChanged(object? sender, SizeChangedEventArgs e) => QueueUpdate();

    private void KnobsPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == GlPageKnobs.YawProperty
            || e.Property == GlPageKnobs.RollProperty
            || e.Property == GlPageKnobs.PitchProperty
            || e.Property == GlPageKnobs.DiscoProperty)
            QueueUpdate();
    }

    private void QueueUpdate()
    {
        if (_updateQueued || _context == null)
            return;
        _updateQueued = true;
        _context.Compositor.RequestCompositionUpdate(Update);
    }

    private void Update()
    {
        _updateQueued = false;
        if (_context == null || _content == null || _visual == null || _surface == null)
            return;

        if (!_context.IsValidForInterop)
        {
            // The context is no longer usable for presentation, drop everything and start over
            Cleanup();
            Initialize();
            return;
        }

        var scaling = TopLevel.GetTopLevel(Viewport)?.RenderScaling ?? 1;
        var size = new PixelSize(
            Math.Max(1, (int)(Viewport.Bounds.Width * scaling)),
            Math.Max(1, (int)(Viewport.Bounds.Height * scaling)));
        _visual.Size = new Vector(Viewport.Bounds.Width, Viewport.Bounds.Height);

        if (_texture != null && (_texture.Size != size || _lastPresent?.IsFaulted == true))
        {
            _texture.DisposeAsync();
            _texture = null;
            _lastPresent = null;
        }

        if (_texture == null)
            _texture = _context.CreateTexture(_surface, size);
        else if (!_texture.IsReadyForDraw)
        {
            // The previous frame hasn't reached the render thread yet, try again on the next composition update
            QueueUpdate();
            return;
        }

        using (_context.GlContext.MakeCurrent())
        {
            var gl = _context.GlContext.GlInterface;
            using (var lease = _texture.BeginDraw())
            {
                var info = lease.Texture;
                gl.BindFramebuffer(GL_FRAMEBUFFER, _fbo);
                UpdateDepthBuffer(gl, size);
                gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, info.Target, info.TextureId, 0);

                _content.OnOpenGlRender(gl, _fbo, size, Knobs.Yaw, Knobs.Pitch, Knobs.Roll, Knobs.Disco);

                gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, info.Target, 0, 0);
                gl.BindFramebuffer(GL_FRAMEBUFFER, 0);
                _lastPresent = lease.PresentAsync();
            }
        }

        if (Knobs.Disco > 0.01)
            QueueUpdate();
    }

    private void UpdateDepthBuffer(GlInterface gl, PixelSize size)
    {
        if (size == _depthBufferSize && _depthBuffer != 0)
            return;

        gl.GetIntegerv(GL_RENDERBUFFER_BINDING, out var oldRenderBuffer);
        if (_depthBuffer != 0)
            gl.DeleteRenderbuffer(_depthBuffer);

        _depthBuffer = gl.GenRenderbuffer();
        gl.BindRenderbuffer(GL_RENDERBUFFER, _depthBuffer);
        gl.RenderbufferStorage(GL_RENDERBUFFER,
            _context!.GlContext.Version.Type == GlProfileType.OpenGLES ? GL_DEPTH_COMPONENT16 : GL_DEPTH_COMPONENT,
            size.Width, size.Height);
        gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, _depthBuffer);
        gl.BindRenderbuffer(GL_RENDERBUFFER, oldRenderBuffer);
        _depthBufferSize = size;
    }
}
