#if AVALONIA_SKIA
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia.RenderTests;

public class OpenGlCompositionInteropTests : TestBase
{
    public OpenGlCompositionInteropTests()
        : base(@"Composition\OpenGlInterop")
    {
    }

    private sealed unsafe class CompositorScaffolding : IDisposable
    {
        private readonly IPlatformGraphics _gpu;
        public ManualRenderTimer Timer { get; } = new();
        public Compositor Compositor { get; }
        public TestRenderRoot Root { get; }
        public CompositingRenderer Renderer { get; }
        public IWriteableBitmapImpl Bitmap { get; }

        public CompositorScaffolding(Control content, IPlatformGraphics gpu, PixelSize pixelSize)
        {
            _gpu = gpu;
            var factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
            Bitmap = factory.CreateWriteableBitmap(pixelSize, new Vector(96, 96), PixelFormats.Rgba8888,
                factory.DefaultAlphaFormat);
            Compositor = new Compositor(RenderLoop.FromTimer(Timer), gpu, true,
                new DispatcherCompositorScheduler(), true, Dispatcher.UIThread);
            IPlatformRenderSurface surface = gpu is Avalonia.OpenGL.Egl.EglPlatformGraphics
                ? new FboReadbackGlPlatformSurface(Bitmap, 1)
                : new VulkanReadbackPlatformSurface(Bitmap, 1);
            Root = new TestRenderRoot(1, null!);
            Renderer = new CompositingRenderer(Root, Compositor, () => new[] { surface });
            // TestRenderRoot sizes itself from the child, so the content must have explicit dimensions
            Root.Initialize(Renderer, content);
            Renderer.Start();
            Dispatcher.UIThread.RunJobs();
        }

        public void Pump()
        {
            Dispatcher.UIThread.RunJobs();
            Timer.TriggerTick();
            Dispatcher.UIThread.RunJobs();
        }

        public T WaitFor<T>(ValueTask<T> task) => WaitFor(task.AsTask());

        public void WaitFor(ValueTask task) => WaitFor(task.AsTask());

        public T WaitFor<T>(Task<T> task)
        {
            WaitFor((Task)task);
            return task.GetAwaiter().GetResult();
        }

        public void WaitFor(Task task)
        {
            for (var c = 0; c < 1000 && !task.IsCompleted; c++)
            {
                Pump();
                if (!task.IsCompleted)
                    // Parts of the commit chain hop through the thread pool, give them a chance to run
                    System.Threading.Thread.Sleep(1);
            }
            Assert.True(task.IsCompleted, "The task hasn't been completed after pumping the event loop");
            task.GetAwaiter().GetResult();
        }

        public ICompositionGlContext CreateGlContext(CompositionGlContextOptions? options = null)
        {
            var context = WaitFor(Compositor.TryCreateCompatibleGlContextAsync(options));
            Assert.NotNull(context);
            return context;
        }

        public (byte R, byte G, byte B, byte A) GetPixel(int x, int y)
        {
            Renderer.Paint(new Rect(0, 0, Bitmap.PixelSize.Width, Bitmap.PixelSize.Height), false);
            using var fb = Bitmap.Lock();
            var px = (byte*)fb.Address + fb.RowBytes * y + x * 4;
            return (px[0], px[1], px[2], px[3]);
        }

        public void Dispose()
        {
            Root.Child = null;
            Dispatcher.UIThread.RunJobs();
            Renderer.Dispose();
            // Compositors are created per test, so their backend contexts have to be
            // disposed deterministically instead of piling up until finalization
            var renderInterface = Compositor.Server.RenderInterface;
            using (renderInterface.EnsureCurrent())
                Compositor.Server.ResetAllGpuResources();
            if (!_gpu.UsesSharedContext)
                renderInterface.GpuContext?.Dispose();
            Bitmap.Dispose();
        }
    }

    private static ICompositionGlTexture ClearTexture(ICompositionGlContext context, ICompositionGlTexture texture,
        float r, float g, float b, float a, out Task present)
    {
        using (context.GlContext.MakeCurrent())
        {
            var gl = context.GlContext.GlInterface;
            var fbo = gl.GenFramebuffer();
            using (var lease = texture.BeginDraw())
            {
                var info = lease.Texture;
                gl.BindFramebuffer(GL_FRAMEBUFFER, fbo);
                gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, info.Target, info.TextureId, 0);
                gl.ClearColor(r, g, b, a);
                gl.Clear(GL_COLOR_BUFFER_BIT);
                gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, info.Target, 0, 0);
                gl.BindFramebuffer(GL_FRAMEBUFFER, 0);
                present = lease.PresentAsync();
            }

            gl.DeleteFramebuffer(fbo);
        }

        return texture;
    }

    private static Exception? RunOnOtherThread(Action action)
    {
        // A dedicated thread instead of Task.Run, since waiting for a task can inline it on the current thread
        Exception? exception = null;
        var thread = new System.Threading.Thread(() => exception = Record.Exception(action));
        thread.Start();
        thread.Join();
        return exception;
    }

    private static CompositionSurfaceVisual AttachSurfaceVisual(CompositorScaffolding scaffolding, Control host,
        CompositionDrawingSurface surface, Size size)
    {
        var visual = scaffolding.Compositor.CreateSurfaceVisual();
        visual.Size = new Vector(size.Width, size.Height);
        visual.Surface = surface;
        ElementComposition.SetElementChildVisual(host, visual);
        Dispatcher.UIThread.RunJobs();
        return visual;
    }

    [Fact]
    public void Can_Create_Compatible_Context_With_Gl_Compositor()
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        using var scaffolding = new CompositorScaffolding(new Border { Width = 100, Height = 100 }, MesaSoftwareRenderer.Gl, new PixelSize(100, 100));

        var context = scaffolding.CreateGlContext();
        Assert.Same(scaffolding.Compositor, context.Compositor);
        Assert.NotNull(context.GlContext);
        Assert.True(context.IsValidForInterop);
        var version = context.GlContext.Version;
        scaffolding.WaitFor(context.DisposeAsync());
        Assert.False(context.IsValidForInterop);

        // GlProfiles preference is plumbed through
        var context2 = scaffolding.CreateGlContext(new CompositionGlContextOptions { GlProfiles = new[] { version } });
        Assert.Equal(version, context2.GlContext.Version);
        scaffolding.WaitFor(context2.DisposeAsync());
    }

    [Fact]
    public void Context_Creation_Returns_Null_With_Vulkan_Compositor()
    {
        if (!MesaSoftwareRenderer.VulkanEnabled)
            return;
        using var scaffolding = new CompositorScaffolding(new Border { Width = 100, Height = 100 }, MesaSoftwareRenderer.Vulkan, new PixelSize(100, 100));

        // lavapipe Vulkan backend has no OpenGL sharing feature and no D3D11 shared memory support
        var context = scaffolding.WaitFor(scaffolding.Compositor.TryCreateCompatibleGlContextAsync());
        Assert.Null(context);
    }

    [Fact]
    public void Presented_Texture_Is_Composited()
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        var host = new Border { Width = 100, Height = 100, Background = Brushes.Black };
        using var scaffolding = new CompositorScaffolding(host, MesaSoftwareRenderer.Gl, new PixelSize(100, 100));
        var context = scaffolding.CreateGlContext();
        var surface = scaffolding.Compositor.CreateDrawingSurface();
        AttachSurfaceVisual(scaffolding, host, surface, new Size(100, 100));

        var texture = context.CreateTexture(surface, new PixelSize(100, 100));
        Assert.True(texture.IsReadyForDraw);
        ClearTexture(context, texture, 1, 0, 0, 1, out var present);

        scaffolding.WaitFor(present);
        Assert.True(texture.IsReadyForDraw);
        Assert.Equal(((byte)255, (byte)0, (byte)0, (byte)255), scaffolding.GetPixel(50, 50));

        // The texture is reusable, present another frame
        ClearTexture(context, texture, 0, 0, 1, 1, out present);
        scaffolding.WaitFor(present);
        Assert.Equal(((byte)0, (byte)0, (byte)255, (byte)255), scaffolding.GetPixel(50, 50));

        scaffolding.WaitFor(texture.DisposeAsync());
        surface.Dispose();
        scaffolding.WaitFor(context.DisposeAsync());
    }

    [Fact]
    public void Lease_State_Machine_Is_Enforced()
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        using var scaffolding = new CompositorScaffolding(new Border { Width = 100, Height = 100 }, MesaSoftwareRenderer.Gl, new PixelSize(100, 100));
        var context = scaffolding.CreateGlContext();
        var surface = scaffolding.Compositor.CreateDrawingSurface();

        var texture = context.CreateTexture(surface, new PixelSize(64, 64));

        // Draw session is exclusive
        var lease = texture.BeginDraw();
        Assert.False(texture.IsReadyForDraw);
        Assert.NotEqual(0, lease.Texture.TextureId);
        Assert.Throws<InvalidOperationException>(() => texture.BeginDraw());

        // Dispose without present discards the frame and makes the texture immediately reusable
        lease.Dispose();
        Assert.True(texture.IsReadyForDraw);
        Assert.Throws<ObjectDisposedException>(() => lease.Texture);

        // While a present is pending the texture is busy. Commits are only processed when the event
        // loop is pumped, so the present can't have been completed at this point.
        ClearTexture(context, texture, 1, 1, 1, 1, out var present);
        Assert.False(present.IsCompleted);
        Assert.False(texture.IsReadyForDraw);
        Assert.Throws<InvalidOperationException>(() => texture.BeginDraw());

        scaffolding.WaitFor(present);
        Assert.True(texture.IsReadyForDraw);

        // Present is terminal for the lease
        var lease2 = texture.BeginDraw();
        var present2 = lease2.PresentAsync();
        Assert.IsType<ObjectDisposedException>(Record.Exception(() => { _ = lease2.PresentAsync(); }));
        Assert.Throws<ObjectDisposedException>(() => lease2.Texture);
        lease2.Dispose(); // no-op after present
        scaffolding.WaitFor(present2);

        scaffolding.WaitFor(texture.DisposeAsync());
        Assert.Throws<ObjectDisposedException>(() => texture.BeginDraw());
        surface.Dispose();
        scaffolding.WaitFor(context.DisposeAsync());
    }

    [Fact]
    public void Entry_Points_Verify_Compositor_Thread_Access()
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        using var scaffolding = new CompositorScaffolding(new Border { Width = 100, Height = 100 }, MesaSoftwareRenderer.Gl, new PixelSize(100, 100));
        var context = scaffolding.CreateGlContext();
        var surface = scaffolding.Compositor.CreateDrawingSurface();
        var texture = context.CreateTexture(surface, new PixelSize(64, 64));

        Assert.IsType<InvalidOperationException>(
            RunOnOtherThread(() => context.CreateTexture(surface, new PixelSize(64, 64))),
            exactMatch: false);
        Assert.IsType<InvalidOperationException>(
            RunOnOtherThread(() => texture.BeginDraw()),
            exactMatch: false);

        scaffolding.WaitFor(texture.DisposeAsync());
        surface.Dispose();
        scaffolding.WaitFor(context.DisposeAsync());
    }

    [Fact]
    public void Create_Texture_Validates_Arguments()
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        using var scaffolding = new CompositorScaffolding(new Border { Width = 100, Height = 100 }, MesaSoftwareRenderer.Gl, new PixelSize(100, 100));
        var context = scaffolding.CreateGlContext();
        var surface = scaffolding.Compositor.CreateDrawingSurface();

        Assert.Throws<ArgumentOutOfRangeException>(() => context.CreateTexture(surface, new PixelSize(0, 0)));

        // A surface from a different compositor is rejected.
        // This compositor has no GPU backend and no started renderer, so there is nothing to dispose deterministically.
        var foreignCompositor = new Compositor(RenderLoop.FromTimer(new ManualRenderTimer()), null, true,
            new DispatcherCompositorScheduler(), true, Dispatcher.UIThread);
        var foreignSurface = foreignCompositor.CreateDrawingSurface();
        Assert.Throws<InvalidOperationException>(() => context.CreateTexture(foreignSurface, new PixelSize(64, 64)));
        foreignSurface.Dispose();

        surface.Dispose();
        scaffolding.WaitFor(context.DisposeAsync());
    }

    [Fact]
    public void Disposing_Context_Disposes_Textures()
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        using var scaffolding = new CompositorScaffolding(new Border { Width = 100, Height = 100 }, MesaSoftwareRenderer.Gl, new PixelSize(100, 100));
        var context = scaffolding.CreateGlContext();
        var surface = scaffolding.Compositor.CreateDrawingSurface();

        var texture = context.CreateTexture(surface, new PixelSize(64, 64));
        ClearTexture(context, texture, 1, 0, 1, 1, out var present);
        scaffolding.WaitFor(present);

        scaffolding.WaitFor(context.DisposeAsync());
        Assert.False(context.IsValidForInterop);
        Assert.False(texture.IsReadyForDraw);
        Assert.Throws<ObjectDisposedException>(() => texture.BeginDraw());
        surface.Dispose();
    }

    private class ClearColorGlControl : OpenGlControlBase
    {
        public int RenderCount;

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            RenderCount++;
            gl.ClearColor(0, 1, 0, 1);
            gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        }
    }

    [Fact]
    public void OpenGlControlBase_Renders_Through_Composition_Interop()
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        var control = new ClearColorGlControl { Width = 100, Height = 100 };
        using var scaffolding = new CompositorScaffolding(control, MesaSoftwareRenderer.Gl, new PixelSize(100, 100));

        // The control initializes asynchronously after being attached
        (byte, byte, byte, byte) pixel = default;
        for (var c = 0; c < 100; c++)
        {
            scaffolding.Pump();
            pixel = scaffolding.GetPixel(50, 50);
            if (pixel == ((byte)0, (byte)255, (byte)0, (byte)255))
                break;
        }

        Assert.Equal(((byte)0, (byte)255, (byte)0, (byte)255), pixel);
        Assert.True(control.RenderCount > 0);

        // Detach triggers the cleanup path
        scaffolding.Root.Child = null;
        for (var c = 0; c < 10; c++)
            scaffolding.Pump();
    }
}
#endif
