using System;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Threading;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks;

public class CompositionTargetUpdateOnly : IDisposable
{
    private readonly bool _includeRender;
    private readonly IDisposable _app;
    private readonly Compositor _compositor;
    private readonly CompositionTarget _target;

    class Timer : IRenderTimer
    {
        public event Action<TimeSpan> Tick;
        public bool RunsInBackground { get; }
    }

    class ManualScheduler : ICompositorScheduler
    {
        public void CommitRequested(Compositor compositor)
        {
            
        }
    }

    class NullFramebuffer : IFramebufferPlatformSurface
    {
        private static readonly IntPtr Buffer = Marshal.AllocHGlobal(4);
        public IFramebufferRenderTarget CreateFramebufferRenderTarget() =>
            new FuncFramebufferRenderTarget(() => new LockedFramebuffer(Buffer, new PixelSize(1, 1), 4, new Vector(96, 96), PixelFormat.Rgba8888,
                null));
    }


    public CompositionTargetUpdateOnly() : this(false)
    {
        
    }
    
    protected CompositionTargetUpdateOnly(bool includeRender)
    {
        _includeRender = includeRender;
        _app = UnitTestApplication.Start(TestServices.StyledWindow);
        _compositor = new Compositor(new RenderLoop(new Timer()), null, true, new ManualScheduler(), true,
            Dispatcher.UIThread, null);
        _target = _compositor.CreateCompositionTarget(() => [new NullFramebuffer()]);
        _target.PixelSize = new PixelSize(1000, 1000);
        _target.Scaling = 1;
        var root = _compositor.CreateContainerVisual();
        root.Size = new Vector(1000, 1000);
        _target.Root = root;
        if (includeRender)
            _target.IsEnabled = true;
        CreatePyramid(root, 7);
        _compositor.Commit();
    }

    void CreatePyramid(CompositionContainerVisual visual, int depth)
    {
        for (var c = 0; c < 4; c++)
        {
            var child = new CompositionDrawListVisual(visual.Compositor,
                new ServerCompositionDrawListVisual(visual.Compositor.Server, null!), null!);
            double right = c == 1 || c == 3 ? 1 : 0;
            double bottom = c > 1 ? 1 : 0;

            var rect = new Rect(
                visual.Size.X / 2 * right,
                visual.Size.Y / 2 * bottom,
                visual.Size.X / 2,
                visual.Size.Y / 2
            );
            child.Offset = new(rect.X, rect.Y, 0);
            child.Size = new Vector(rect.Width, rect.Height);
            
            var ctx = new RenderDataDrawingContext(child.Compositor);
            ctx.DrawRectangle(Brushes.Aqua, null, new Rect(rect.Size));
            child.DrawList = ctx.GetRenderResults();
            child.Visible = true;
            visual.Children.Add(child);
            if (depth > 0)
                CreatePyramid(child, depth - 1);
        }
    }

    [Benchmark]
    public void TargetUpdate()
    {
        _target.Root.Offset = new Vector3D(_target.Root.Offset.X == 0 ? 1 : 0, 0, 0);
        _compositor.Commit();
        _compositor.Server.Render();
        if (!_includeRender)
            _target.Server.Update();

    }


    public void Dispose()
    {
        _app.Dispose();
        
    }
}

public class CompositionTargetUpdateWithRender : CompositionTargetUpdateOnly
{
    public CompositionTargetUpdateWithRender() : base(true)
    {
    }
}