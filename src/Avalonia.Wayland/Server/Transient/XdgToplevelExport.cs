using System;
using System.Threading.Tasks;
using Avalonia.Wayland.Server.Persistent;
using NWayland;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgForeignUnstableV2;

namespace Avalonia.Wayland.Server.Transient;

/// <summary>
/// Worker-thread handle to a single <c>zxdg_exported_v2</c> export of a wayland
/// toplevel. Implements the UI-facing <see cref="IWaylandXdgTopLevelExport"/>
/// directly so it can be returned through the cross-thread proxy: <see cref="IWaylandXdgTopLevelExport.HandleTask"/>
/// is just a TCS that the worker completes when the compositor delivers the handle event,
/// and Dispose hops back to the worker thread for the actual destroy.
/// </summary>
internal sealed class XdgToplevelExport : IDisposable
{
    private readonly WXdgTopLevel _owner;
    private readonly WaylandWorker _worker;
    private readonly TaskCompletionSource<string?> _handleTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private ZxdgExportedV2? _exported;
    private bool _alive = true;

    internal XdgToplevelExport(WXdgTopLevel owner, WaylandWorker worker, ZxdgExporterV2 exporter,
        WlSurface surface, WlEventQueue queue)
    {
        _owner = owner;
        _worker = worker;
        _exported = exporter.ExportToplevel(surface, new ExportListener(this), queue);
    }

    public IWaylandXdgTopLevelExport GetUiThreadHandle() => new UiThreadHandle(this);
    
    class UiThreadHandle(XdgToplevelExport parent) : IWaylandXdgTopLevelExport
    {
        public Task<string?> HandleTask => parent._handleTcs.Task;
        
        public void Dispose() => parent._worker.PostOob(parent.Dispose);
    }

    public void Dispose()
    {
        if (!_alive)
            return;
        _alive = false;
        _exported?.Destroy();
        _exported = null;
        _owner.RemoveExport(this);
        // Unblock any awaiter that's still waiting on a handle event we'll never get.
        _handleTcs.TrySetResult(null);
    }

    private void OnHandle(string handle)
    {
        if (!_alive)
            return;
        _handleTcs.TrySetResult(handle);
    }

    private sealed class ExportListener(XdgToplevelExport self) : ZxdgExportedV2.Listener
    {
        protected override void Handle(ZxdgExportedV2 eventSender, string handle) => self.OnHandle(handle);
    }
}
