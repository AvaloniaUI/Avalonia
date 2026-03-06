using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using ControlCatalog.Pages.OpenGl;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace ControlCatalog.Pages;

public partial class OpenGlLeasePage : UserControl
{
    private CompositionCustomVisual? _visual;

    class GlVisual : CompositionCustomVisualHandler
    {
        private OpenGlContent _content;
        private Parameters _parameters;
        private bool _contentInitialized;
        private OpenGlFbo? _fbo;
        private bool _reRender;
        private IGlContext? _gl;

        public GlVisual(OpenGlContent content, Parameters parameters)
        {
            _content = content;
            _parameters = parameters;
        }

        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            if (_parameters.Disco > 0.01f)
                RegisterForNextAnimationFrameUpdate();
            var bounds = GetRenderBounds();
            var size = PixelSize.FromSize(bounds.Size, 1);
            if (size.Width < 1 || size.Height < 1)
                return;
            
            if(drawingContext.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var skiaFeature))
            {
                using var skiaLease = skiaFeature.Lease();
                var grContext = skiaLease.GrContext;
                if (grContext == null)
                    return;
                SKImage? snapshot;
                using (var platformApiLease = skiaLease.TryLeasePlatformGraphicsApi())
                {
                    if (platformApiLease?.Context is not IGlContext glContext)
                        return;

                    var gl = glContext.GlInterface;
                    if (_gl != glContext)
                    {
                        // The old context is lost
                        _fbo = null;
                        _contentInitialized = false;
                        _gl = glContext;
                    }

                    gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var oldFb);

                    _fbo ??= new OpenGlFbo(glContext, grContext);
                    if (_fbo.Size != size)
                        _fbo.Resize(size);

                    gl.BindFramebuffer(GL_FRAMEBUFFER, _fbo.Fbo);

                    
                    if (!_contentInitialized)
                    {
                        _content.Init(gl, glContext.Version);
                        _contentInitialized = true;
                    }

                    

                    _content.OnOpenGlRender(gl, _fbo.Fbo, size, _parameters.Yaw, _parameters.Pitch,
                        _parameters.Roll, _parameters.Disco);

                    snapshot = _fbo.Snapshot();
                    gl.BindFramebuffer(GL_FRAMEBUFFER, oldFb);
                }

                using(snapshot)
                    if (snapshot != null)
                        skiaLease.SkCanvas.DrawImage(snapshot, new SKRect(0, 0,
                            (float)bounds.Width, (float)bounds.Height));
            }
        }

        public override void OnAnimationFrameUpdate()
        {
            if (_reRender || _parameters.Disco > 0.01f)
            {
                _reRender = false;
                Invalidate();
            }

            base.OnAnimationFrameUpdate();
        }

        public override void OnMessage(object message)
        {
            if (message is Parameters p)
            {
                _parameters = p;
                _reRender = true;
                RegisterForNextAnimationFrameUpdate();
            }
            else if (message is DisposeMessage)
            {
                if (_gl != null)
                {
                    try
                    {
                        if (_fbo != null || _contentInitialized)
                        {
                            using (_gl.MakeCurrent())
                            {
                                if (_contentInitialized)
                                    _content.Deinit(_gl.GlInterface);
                                _contentInitialized = false;
                                _fbo?.Dispose();
                                _fbo = null;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }

                    _gl = null;
                }
            }

            base.OnMessage(message);
        }
    }

    public class Parameters
    {
        public float Yaw;
        public float Pitch;
        public float Roll;
        public float Disco;
    }

    public class DisposeMessage
    {
        
    }
    
    public OpenGlLeasePage()
    {
        InitializeComponent();
    }

    private void KnobsPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == GlPageKnobs.YawProperty
            || change.Property == GlPageKnobs.RollProperty
            || change.Property == GlPageKnobs.PitchProperty
            || change.Property == GlPageKnobs.DiscoProperty)
            _visual?.SendHandlerMessage(GetParameters());
    }

    Parameters GetParameters() => new()
    {
        Yaw = Knobs!.Yaw, Pitch = Knobs.Pitch, Roll = Knobs.Roll, Disco = Knobs.Disco
    };
    
    private void ViewportAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        var visual = ElementComposition.GetElementVisual(Viewport);
        if(visual == null)
            return;
        _visual = visual.Compositor.CreateCustomVisual(new GlVisual(new OpenGlContent(), GetParameters()));
        ElementComposition.SetElementChildVisual(Viewport, _visual);
        UpdateSize(Bounds.Size);
    }

    private void UpdateSize(Size size)
    {
        if (_visual != null)
            _visual.Size = new Vector(size.Width, size.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        UpdateSize(size);
        return size;
    }

    private void ViewportDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _visual?.SendHandlerMessage(new DisposeMessage());
        _visual = null;
        ElementComposition.SetElementChildVisual(Viewport, null);
        base.OnDetachedFromVisualTree(e);
    }
}
