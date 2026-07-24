using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.OpenGL;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using ControlCatalog.Pages.OpenGl;
using static Avalonia.OpenGL.GlConsts;

namespace ControlCatalog.Pages;

/// <summary>
/// Demonstrates rendering to a CompositionDrawingSurface from a dedicated thread:
/// a CompositorController provides a Compositor attached to the render thread of the UI compositor,
/// and a proxy of the surface is fed from the worker thread. The scene keeps animating
/// even while the UI thread is stalled.
/// </summary>
public partial class OpenGlOffThreadPage : ContentPage
{
    private CompositionDrawingSurface? _surface;
    private CompositionSurfaceVisual? _visual;
    private GlRenderWorker? _worker;
    private int _generation;

    public OpenGlOffThreadPage()
    {
        InitializeComponent();
    }

    private void ViewportAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => Initialize();

    private void ViewportDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => Cleanup();

    private void Initialize()
    {
        var generation = ++_generation;
        if (ElementComposition.GetElementVisual(Viewport) is not { } elementVisual)
            return;
        var compositor = elementVisual.Compositor;

        _surface = compositor.CreateDrawingSurface();
        _visual = compositor.CreateSurfaceVisual();
        _visual.Surface = _surface;
        ElementComposition.SetElementChildVisual(Viewport, _visual);

        _worker = new GlRenderWorker(compositor,
            createProxy: workerCompositor =>
                generation == _generation && _surface is { IsDisposed: false } surface
                    ? surface.CreateProxyForCompositor(workerCompositor)
                    : null,
            reportInfo: info =>
            {
                if (generation == _generation)
                    Knobs.Info = info;
            },
            restartRequested: () =>
            {
                // The GL context is no longer usable for interop, drop everything and start over
                if (generation == _generation)
                {
                    Cleanup();
                    Initialize();
                }
            });

        Viewport.SizeChanged += OnViewportSizeChanged;
        UpdateWorkerParameters();
        _worker.Start();
    }

    private void Cleanup()
    {
        _generation++;
        Viewport.SizeChanged -= OnViewportSizeChanged;
        ElementComposition.SetElementChildVisual(Viewport, null);
        _visual = null;
        _worker?.Stop();
        _worker = null;
        // The worker holds a proxy, so the server-side surface stays alive until the worker is done with it
        _surface?.Dispose();
        _surface = null;
    }

    private void OnViewportSizeChanged(object? sender, SizeChangedEventArgs e) => UpdateWorkerParameters();

    private void KnobsPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == GlPageKnobs.YawProperty
            || e.Property == GlPageKnobs.RollProperty
            || e.Property == GlPageKnobs.PitchProperty
            || e.Property == GlPageKnobs.DiscoProperty)
            UpdateWorkerParameters();
    }

    private void UpdateWorkerParameters()
    {
        if (_worker == null)
            return;
        var scaling = TopLevel.GetTopLevel(Viewport)?.RenderScaling ?? 1;
        var size = new PixelSize(
            Math.Max(1, (int)(Viewport.Bounds.Width * scaling)),
            Math.Max(1, (int)(Viewport.Bounds.Height * scaling)));
        if (_visual != null)
            _visual.Size = new Vector(Viewport.Bounds.Width, Viewport.Bounds.Height);
        _worker.SetParameters(size, Knobs.Yaw, Knobs.Pitch, Knobs.Roll, Knobs.Disco);
    }

    private void StallUiThread(object? sender, RoutedEventArgs e) => Thread.Sleep(3000);

    private sealed class GlRenderWorker
    {
        private readonly Compositor _uiCompositor;
        private readonly Func<Compositor, CompositionDrawingSurface?> _createProxy;
        private readonly Action<string> _reportInfo;
        private readonly Action _restartRequested;
        private readonly CancellationTokenSource _cts = new();
        private readonly Thread _thread;
        private readonly object _parametersLock = new();
        private Parameters _parameters;

        private struct Parameters
        {
            public PixelSize Size;
            public float Yaw, Pitch, Roll, Disco;
        }

        public GlRenderWorker(Compositor uiCompositor,
            Func<Compositor, CompositionDrawingSurface?> createProxy,
            Action<string> reportInfo, Action restartRequested)
        {
            _uiCompositor = uiCompositor;
            _createProxy = createProxy;
            _reportInfo = reportInfo;
            _restartRequested = restartRequested;
            _thread = new Thread(ThreadMain) { Name = "GL interop worker", IsBackground = true };
        }

        public void Start() => _thread.Start();

        public void Stop() => _cts.Cancel();

        public void SetParameters(PixelSize size, float yaw, float pitch, float roll, float disco)
        {
            lock (_parametersLock)
                _parameters = new Parameters { Size = size, Yaw = yaw, Pitch = pitch, Roll = roll, Disco = disco };
        }

        private void ReportInfo(string info) => Dispatcher.UIThread.Post(() => _reportInfo(info));

        private void ThreadMain()
        {
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                var frame = new DispatcherFrame();
                dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        await RunAsync();
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception e)
                    {
                        ReportInfo("Off-thread rendering failed: " + e.Message);
                    }
                    finally
                    {
                        frame.Continue = false;
                    }
                });
                // Pumps posted jobs and keeps await continuations on this thread
                dispatcher.PushFrame(frame);
                dispatcher.InvokeShutdown();
            }
            catch (Exception e)
            {
                ReportInfo("Off-thread rendering failed: " + e.Message);
            }
        }

        private async Task RunAsync()
        {
            var controller = _uiCompositor.CreateCompositorController();
            var compositor = controller.Compositor;

            var proxy = await Dispatcher.UIThread.InvokeAsync(() => _createProxy(compositor));
            if (proxy == null)
                return;

            ICompositionGlContext? context = null;
            try
            {
                context = await compositor.TryCreateCompatibleGlContextAsync();
                if (context == null)
                {
                    ReportInfo("Compositor OpenGL interop is not available on this platform");
                    return;
                }

                await RenderLoop(context, proxy, _cts.Token);
            }
            finally
            {
                proxy.Dispose();
                if (context != null)
                    await context.DisposeAsync();
                // Make sure the dispose jobs reach the render thread even though the loop is exiting
                controller.Commit();
            }

            if (!_cts.IsCancellationRequested)
                Dispatcher.UIThread.Post(_restartRequested);
        }

        private async Task RenderLoop(ICompositionGlContext context, CompositionDrawingSurface proxy,
            CancellationToken cancellationToken)
        {
            var gl = context.GlContext.GlInterface;
            var content = new OpenGlContent();
            int fbo;
            var depthBuffer = 0;
            var depthBufferSize = default(PixelSize);
            ICompositionGlTexture? texture = null;
            var clock = Stopwatch.StartNew();

            using (context.GlContext.MakeCurrent())
            {
                fbo = gl.GenFramebuffer();
                content.Init(gl, context.GlContext.Version);
            }
            ReportInfo($"{content.Info}\nRendering from a dedicated thread");

            void UpdateDepthBuffer(PixelSize size)
            {
                if (size == depthBufferSize && depthBuffer != 0)
                    return;
                gl.GetIntegerv(GL_RENDERBUFFER_BINDING, out var oldRenderBuffer);
                if (depthBuffer != 0)
                    gl.DeleteRenderbuffer(depthBuffer);
                depthBuffer = gl.GenRenderbuffer();
                gl.BindRenderbuffer(GL_RENDERBUFFER, depthBuffer);
                gl.RenderbufferStorage(GL_RENDERBUFFER,
                    context.GlContext.Version.Type == GlProfileType.OpenGLES
                        ? GL_DEPTH_COMPONENT16
                        : GL_DEPTH_COMPONENT,
                    size.Width, size.Height);
                gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, depthBuffer);
                gl.BindRenderbuffer(GL_RENDERBUFFER, oldRenderBuffer);
                depthBufferSize = size;
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested && context.IsValidForInterop)
                {
                    Parameters parameters;
                    lock (_parametersLock)
                        parameters = _parameters;

                    if (parameters.Size.Width < 1 || parameters.Size.Height < 1)
                    {
                        await Task.Delay(100, cancellationToken);
                        continue;
                    }

                    if (texture != null && texture.Size != parameters.Size)
                    {
                        await texture.DisposeAsync();
                        texture = null;
                    }
                    texture ??= context.CreateTexture(proxy, parameters.Size);

                    Task present;
                    using (context.GlContext.MakeCurrent())
                    {
                        using var lease = texture.BeginDraw();
                        var textureInfo = lease.Texture;
                        gl.BindFramebuffer(GL_FRAMEBUFFER, fbo);
                        UpdateDepthBuffer(parameters.Size);
                        gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0,
                            textureInfo.Target, textureInfo.TextureId, 0);

                        // Extra time-based spin so the scene visibly animates on its own
                        var spin = (float)(clock.Elapsed.TotalSeconds * 0.5);
                        content.OnOpenGlRender(gl, fbo, parameters.Size,
                            parameters.Yaw + spin, parameters.Pitch, parameters.Roll, parameters.Disco);

                        gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, textureInfo.Target, 0, 0);
                        gl.BindFramebuffer(GL_FRAMEBUFFER, 0);
                        present = lease.PresentAsync();
                    }

                    try
                    {
                        // Frame pacing comes from the compositor's render loop processing the frame
                        await present;
                    }
                    catch
                    {
                        // Presentation failures indicate a lost context, exit and let the page reinitialize
                        break;
                    }
                }
            }
            finally
            {
                if (texture != null)
                    await texture.DisposeAsync();
                try
                {
                    using (context.GlContext.MakeCurrent())
                    {
                        content.Deinit(gl);
                        if (depthBuffer != 0)
                            gl.DeleteRenderbuffer(depthBuffer);
                        gl.DeleteFramebuffer(fbo);
                    }
                }
                catch
                {
                    // The context is likely lost
                }
            }
        }
    }
}
