using System;
using System.Threading;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

public class CompositorControllerTests
{
    [Fact]
    public void Controller_Compositor_Shares_Server_And_Commits_Via_Posted_Job()
    {
        using var s = new CompositorTestServices();
        var controller = new CompositorController(s.Compositor, Dispatcher.UIThread);
        Assert.Same(s.Compositor.Server, controller.Compositor.Server);

        var batch = controller.Compositor.RequestCompositionBatchCommitAsync();
        Assert.False(batch.Processed.IsCompleted);
        s.RunJobs();
        Assert.True(batch.Processed.IsCompleted);
    }

    [Fact]
    public void Manual_Commit_Submits_Synchronously_And_Scheduled_Job_Becomes_Noop()
    {
        using var s = new CompositorTestServices();
        var controller = new CompositorController(s.Compositor, Dispatcher.UIThread);
        var compositor = controller.Compositor;
        var commitCount = 0;
        compositor.AfterCommit += () => commitCount++;

        var batch = compositor.RequestCompositionBatchCommitAsync();
        Assert.Same(batch, controller.Commit());
        Assert.False(compositor.HasPendingCommit);
        Assert.Equal(1, commitCount);

        // The batch is already submitted, so a render pass processes it without pumping the dispatcher
        s.Timer.TriggerTick();
        Assert.True(batch.Processed.IsCompleted);

        // The still-scheduled commit job must not submit an empty batch
        s.RunJobs();
        Assert.Equal(1, commitCount);
    }

    [Fact]
    public void Manual_Commit_Of_Empty_Batch_Provides_A_Render_Thread_Fence()
    {
        using var s = new CompositorTestServices();
        var controller = new CompositorController(s.Compositor, Dispatcher.UIThread);

        var batch = controller.Commit();
        Assert.False(batch.Processed.IsCompleted);
        s.Timer.TriggerTick();
        Assert.True(batch.Processed.IsCompleted);
    }

    [Fact]
    public void Commit_Requested_While_A_Batch_Is_In_Flight_Is_Deferred_Until_It_Is_Processed()
    {
        using var s = new CompositorTestServices();
        var controller = new CompositorController(s.Compositor, Dispatcher.UIThread);
        var compositor = controller.Compositor;
        var commitCount = 0;
        compositor.AfterCommit += () => commitCount++;

        var batch1 = compositor.RequestCompositionBatchCommitAsync();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, commitCount);
        Assert.False(batch1.Processed.IsCompleted);

        var batch2 = compositor.RequestCompositionBatchCommitAsync();
        Assert.NotSame(batch1, batch2);
        // The second batch is not committed while the first one is still in flight
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, commitCount);

        s.Timer.TriggerTick();
        Assert.True(batch1.Processed.IsCompleted);
        Assert.False(batch2.Processed.IsCompleted);

        // Processing the first batch re-triggers the commit request
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, commitCount);
        s.Timer.TriggerTick();
        Assert.True(batch2.Processed.IsCompleted);
    }

    [Fact]
    public void Dispatcher_Shutdown_Flushes_The_Pending_Commit()
    {
        using var s = new CompositorTestServices();
        CompositionBatch? batch = null;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                var controller = new CompositorController(s.Compositor, dispatcher);
                batch = controller.Compositor.RequestCompositionBatchCommitAsync();
                dispatcher.InvokeShutdown();
            }
            catch (Exception e)
            {
                error = e;
            }
        });
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)));
        Assert.Null(error);

        // The batch was submitted by the shutdown flush without the worker dispatcher ever being pumped
        Assert.False(batch!.Processed.IsCompleted);
        s.Timer.TriggerTick();
        Assert.True(batch.Processed.IsCompleted);
    }

    [Fact]
    public void Proxy_Surface_Shares_Server_Object_And_Belongs_To_Target_Compositor()
    {
        using var s = new CompositorTestServices();
        var controller = new CompositorController(s.Compositor, Dispatcher.UIThread);
        var surface = s.Compositor.CreateDrawingSurface();

        var proxy = surface.CreateProxyForCompositor(controller.Compositor);
        Assert.Same(surface.Server, proxy.Server);
        Assert.Same(controller.Compositor, proxy.Compositor);

        proxy.Dispose();
        surface.Dispose();
        s.RunJobs();
    }

    [Fact]
    public void Proxy_Creation_Validates_Surface_State_And_Server_Affinity()
    {
        using var s = new CompositorTestServices();
        var controller = new CompositorController(s.Compositor, Dispatcher.UIThread);
        var surface = s.Compositor.CreateDrawingSurface();

        Assert.Throws<InvalidOperationException>(() => surface.CreateProxyForCompositor(s.Compositor));

        var foreignCompositor = new Compositor(RenderLoop.FromTimer(new CompositorTestServices.ManualRenderTimer()), null, true,
            new DispatcherCompositorScheduler(), true, Dispatcher.UIThread);
        Assert.Throws<InvalidOperationException>(() => surface.CreateProxyForCompositor(foreignCompositor));
        surface.Dispose();

        var disposedSurface = s.Compositor.CreateDrawingSurface();
        disposedSurface.Dispose();
        Assert.Throws<ObjectDisposedException>(() => disposedSurface.CreateProxyForCompositor(controller.Compositor));
    }

    [Fact]
    public void Imported_Objects_From_A_Different_Compositor_Are_Rejected()
    {
        using var s = new CompositorTestServices();
        var controller = new CompositorController(s.Compositor, Dispatcher.UIThread);
        var surface = s.Compositor.CreateDrawingSurface();
        var proxy = surface.CreateProxyForCompositor(controller.Compositor);

        var foreignImage = new CompositionImportedGpuImage(s.Compositor, null!, null!, () => null!, null);
        // The ownership check throws synchronously, before any server job is scheduled
        Assert.Throws<InvalidOperationException>(() => { _ = proxy.UpdateAsync(foreignImage); });

        proxy.Dispose();
        surface.Dispose();
        s.RunJobs();
    }

    [Fact]
    public void Server_Surface_Is_Released_When_The_Last_Reference_Is_Disposed()
    {
        using var s = new CompositorTestServices();
        var server = new ServerCompositionDrawingSurface(s.Compositor.Server);

        server.AddRef();
        server.Dispose();
        // Not released yet, adding a reference is still valid
        server.AddRef();
        server.Dispose();
        server.Dispose();
        Assert.Throws<ObjectDisposedException>(() => server.AddRef());
        // A failed AddRef must not resurrect the surface
        Assert.Throws<ObjectDisposedException>(() => server.AddRef());
    }
}
