using System;
using System.Threading.Tasks;
using Avalonia.Input.Raw;
using Avalonia.Rendering.Composition;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland.Server;

/// <summary>
/// UI-thread-safe interface to the wayland worker.
/// UI thread code should use this instead of accessing WaylandWorker directly.
/// </summary>
class WaylandWorkerClient
{
    private readonly WaylandWorker _worker;

    internal WaylandWorkerClient(WaylandWorker worker)
    {
        _worker = worker;
        Compositor = worker.Compositor;
        InputDispatchQueue = worker.InputDispatchQueue;
        AnyThreadWakeupRenderLoop = worker.AnyThreadWakeupRenderLoop;
        Marshaller = (action, priority) =>
        {
            if (priority == WaylandDispatchPriority.Oob)
                worker.PostOob(action);
            else
                worker.PostWithCommit(action);
        };
    }

    public Compositor Compositor { get; }

    /// <summary>Shared platform-wide input-dispatch queue (any-thread safe).</summary>
    public IRawEventGrouperDispatchQueue InputDispatchQueue { get; }

    /// <summary>
    /// Wakes the wayland worker's render loop. Documented as safe to call
    /// from any thread (see <see cref="WaylandWorker.AnyThreadWakeupRenderLoop"/>).
    /// </summary>
    public Action AnyThreadWakeupRenderLoop { get; }

    /// <summary>
    /// Marshaller for UI→worker cross-thread proxies (see
    /// <c>CrossThreadProxyGenerator</c>). Bound to this worker instance —
    /// no global state.
    /// </summary>
    public Action<Action, WaylandDispatchPriority> Marshaller { get; }

    /// <summary>
    /// Posts a rare out-of-band callback directly to the wayland thread queue.
    /// </summary>
    public void PostOob(Action cb) => _worker.PostOob(cb);

    /// <summary>
    /// Posts a callback to the wayland thread batched with the current compositor commit.
    /// </summary>
    public void PostWithCommit(Action cb) => _worker.PostWithCommit(cb);

    /// <summary>
    /// Posts a callback to the wayland thread queue and returns a task that indicates its completion.
    /// </summary>
    public Task<T> InvokeAsync<T>(Func<T> cb) => _worker.InvokeAsync(cb);

    /// <summary>
    /// Creates a new xdg_toplevel surface and returns the bundle of UI-thread
    /// accessors needed to drive it. The underlying <see cref="WXdgTopLevel"/>
    /// is worker-thread state and is intentionally not exposed.
    /// </summary>
    public WaylandSurfaceCreateResult<WXdgTopLevelProxy> CreateTopLevelHandle(WXdgTopLevelEventSinkProxy sink)
    {
        var topLevel = new WXdgTopLevel(_worker, sink);
        var proxy = new WXdgTopLevelProxy(topLevel, Marshaller);
        return new WaylandSurfaceCreateResult<WXdgTopLevelProxy>(
            Proxy: proxy,
            GetRenderSurfaces: () => topLevel.RenderSurfaces,
            BasicInitCompleted: topLevel.BasicInitCompleted);
    }

    /// <summary>
    /// Creates a new xdg_popup surface parented to <paramref name="parent"/>
    /// (a <see cref="WXdgTopLevelProxy"/> / <see cref="WXdgPopupProxy"/> /
    /// other <see cref="WXdgShellSurfaceProxy"/> belonging to this worker).
    /// Throws if <paramref name="parent"/> doesn't unwrap to a known
    /// worker-side parent surface. The popup isn't actually mapped until
    /// the UI side calls <see cref="IWXdgPopup.UpdatePositioner"/> at least
    /// once and a buffer is attached.
    /// </summary>
    public WaylandSurfaceCreateResult<WXdgPopupProxy> CreatePopupHandle(
        WXdgPopupEventSinkProxy sink,
        WXdgShellSurfaceProxy parent)
    {
        var popup = new WXdgPopup(_worker, sink, (WXdgShellSurface)parent.ProxyTarget);
        var proxy = new WXdgPopupProxy(popup, Marshaller);
        // Popups don't currently surface a "basic init" task to the UI
        // side: the UI driver waits on the first OnPopupConfigure delivered
        // through the event sink instead. We therefore complete the task
        // immediately with an empty configure batch placeholder so the
        // shared WaylandSurfaceCreateResult<T> shape is preserved.
        return new WaylandSurfaceCreateResult<WXdgPopupProxy>(
            Proxy: proxy,
            GetRenderSurfaces: () => popup.RenderSurfaces,
            BasicInitCompleted: Task.FromResult(new XdgConfigureBatch()));
    }
}
