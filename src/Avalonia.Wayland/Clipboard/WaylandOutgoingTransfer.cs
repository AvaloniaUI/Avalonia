using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Persistent;
using Avalonia.Wayland.Server.Transient;
using Avalonia.Wayland.Server.Transient.Clipboard;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Clipboard;

/// <summary>
/// Manages the outgoing side of a data transfer (clipboard set or DnD initiate).
/// Created on the UI thread; coordinates between UI-thread <see cref="IAsyncDataTransfer"/>
/// and Wayland-thread <see cref="WaylandDataSource"/>.
/// <para>
/// The <c>send</c> callback dispatches to the UI thread via <see cref="Dispatcher.UIThread"/>
/// so that user-provided data transfer methods run on the correct thread.
/// </para>
/// </summary>
class WaylandOutgoingTransfer
{
    private const string InProcessMimePrefix = "application/x-avalonia-inproc-dnd-";

    /// <summary>
    /// Unique identifier for this application instance, used as part of the sentinel MIME type
    /// to distinguish our own drags from drags originating in other Avalonia processes.
    /// </summary>
    private static readonly string s_instanceId = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Full MIME prefix for this app instance: <c>application/x-avalonia-inproc-dnd-{instance}-</c>.
    /// </summary>
    private static readonly string s_instancePrefix = InProcessMimePrefix + s_instanceId + "-";

    /// <summary>
    /// Maps in-process DnD operation keys to the original <see cref="IDataTransfer"/>.
    /// Accessed exclusively from the Wayland thread.
    /// When the receive side sees a sentinel MIME type matching this instance's prefix,
    /// it uses the original transfer directly instead of reading through Wayland pipes,
    /// which would deadlock (the source's <c>send</c> handler dispatches to the UI thread).
    /// </summary>
    private static readonly Dictionary<string, IDataTransfer> s_inProcessDrags = new();

    private readonly IAsyncDataTransfer _transfer;
    private readonly List<string> _mimeTypes;

    /// <summary>
    /// Called on the UI thread when the data source is cancelled (lost ownership or DnD ended).
    /// </summary>
    public Action? Cancelled { get; set; }

    public WaylandOutgoingTransfer(IAsyncDataTransfer transfer)
    {
        _transfer = transfer;
        _mimeTypes = new List<string>();
        foreach (var format in transfer.Formats)
            foreach (var mime in WaylandMimeMapper.ToMimeTypes(format))
                _mimeTypes.Add(mime);
    }

    /// <summary>
    /// Creates a <see cref="WaylandDataSource"/> on the Wayland thread, configures it,
    /// and sets it as the clipboard selection.
    /// Returns a task that completes when the selection has been set.
    /// </summary>
    public Task SetAsSelection(WaylandWorker worker)
    {
        return worker.InvokeOobAsync(() =>
        {
            var globals = worker.Globals;
            if (globals == null)
                return;

            var device = globals.InputDispatcher.GetDataDevice();
            if (device == null)
                return;

            var source = CreateSource(globals);
            if (source == null)
                return;

            source.OnCancelled = () =>
            {
                device.ActiveSource = null;
                source.Dispose();
                Dispatcher.UIThread.Post(() => Cancelled?.Invoke());
            };

            device.SetSelection(source);
        });
    }

    /// <summary>
    /// Creates a <see cref="WaylandDataSource"/> on the Wayland thread, configures it,
    /// and starts a drag-and-drop operation using <c>wl_data_device.start_drag</c>.
    /// The <paramref name="inputCookie"/> carries the globals captured at input-event time,
    /// ensuring reconnect-safe access to transient state.
    /// Returns a task that resolves to the resulting <see cref="DragDropEffects"/> when
    /// the DnD operation completes (finished, cancelled, or failed to start).
    /// </summary>
    public Task<DragDropEffects> SetAsDrag(WaylandInputEventCookie inputCookie, DragDropEffects allowedEffects)
    {
        var tcs = new TaskCompletionSource<DragDropEffects>();
        DragDropEffects negotiatedEffects = DragDropEffects.None;

        inputCookie.PostOob(globals =>
        {
            var device = globals.InputDispatcher.GetDataDevice();
            if (device == null)
            {
                tcs.TrySetResult(DragDropEffects.None);
                return;
            }

            var source = CreateSource(globals);
            if (source == null)
            {
                tcs.TrySetResult(DragDropEffects.None);
                return;
            }

            // Register for in-process detection so the receive side can use
            // the original IDataTransfer directly instead of reading through
            // Wayland pipes (which would deadlock).
            var operationKey = Guid.NewGuid().ToString("N");
            source.Offer(s_instancePrefix + operationKey);
            s_inProcessDrags[operationKey] = (IDataTransfer)_transfer;

            var allowedActions = WaylandDataDevice.EffectsToActions(allowedEffects);

            source.OnAction = action =>
            {
                negotiatedEffects = WaylandDataDevice.ActionsToEffects(action);
                var cursorType = action switch
                {
                    WlDataDeviceManager.DndActionEnum.Copy => StandardCursorType.DragCopy,
                    WlDataDeviceManager.DndActionEnum.Move => StandardCursorType.DragMove,
                    WlDataDeviceManager.DndActionEnum.Ask => StandardCursorType.DragLink,
                    _ => StandardCursorType.No
                };
                globals.InputDispatcher.SetDndCursor(cursorType);
            };

            source.OnCancelled = () =>
            {
                s_inProcessDrags.Remove(operationKey);
                source.Dispose();
                Dispatcher.UIThread.Post(() =>
                {
                    Cancelled?.Invoke();
                    tcs.TrySetResult(DragDropEffects.None);
                });
            };

            source.OnDndFinished = () =>
            {
                s_inProcessDrags.Remove(operationKey);
                source.Dispose();
                var result = negotiatedEffects;
                Dispatcher.UIThread.Post(() => tcs.TrySetResult(result));
            };

            source.OnDndDropPerformed = () =>
            {
                // Drop performed by user but destination may still reject —
                // wait for DndFinished or Cancelled to resolve the TCS.
            };

            if (!device.StartDrag(source, inputCookie, allowedActions))
            {
                s_inProcessDrags.Remove(operationKey);
                source.Dispose();
                tcs.TrySetResult(DragDropEffects.None);
            }
        });

        return tcs.Task;
    }

    /// <summary>
    /// Creates and configures a <see cref="WaylandDataSource"/> with MIME type offers
    /// and a send handler that dispatches to the UI thread.
    /// </summary>
    private WaylandDataSource? CreateSource(WaylandGlobals globals)
    {
        var manager = globals.DataDeviceManager;
        if (manager == null)
            return null;

        var sourceListener = new WaylandDataSourceListener();
        var wlSource = manager.CreateDataSource(sourceListener);
        var source = new WaylandDataSource(wlSource);
        sourceListener.SetWrapper(source);

        foreach (var mime in _mimeTypes)
            source.Offer(mime);

        source.OnSend = HandleSend;

        return source;
    }

    /// <summary>
    /// Called on the Wayland thread when the compositor requests data.
    /// Dispatches to the UI thread to read from the user's data transfer and write to the pipe fd.
    /// </summary>
    private void HandleSend(string mime, int fd)
    {
        var transfer = _transfer;
        Dispatcher.UIThread.Post(async () =>
        {
            await using var stream = new Pipe2Stream(fd, PipeDirection.Out);
            try
            {
                var data = await WaylandClipboardImpl.SerializeForMimeAsync(transfer, mime);
                if (data != null)
                    await stream.WriteAsync(data);
            }
            catch
            {
                // Stream disposal closes the fd
            }
        });
    }

    /// <summary>
    /// Checks whether the offered MIME types contain an in-process sentinel from this app instance.
    /// If so, returns the original <see cref="IDataTransfer"/> directly, avoiding
    /// pipe-based reads that would deadlock on self-drags.
    /// If the sentinel matches our instance but the operation key is not found
    /// (e.g., source already cleaned up), returns an empty <see cref="DataTransfer"/>
    /// as a safe dummy — we cannot interact with our own data offer via pipes.
    /// Called from the Wayland thread during <c>data_device.enter</c>.
    /// </summary>
    internal static IDataTransfer? TryGetInProcessTransfer(string[] mimeTypes)
    {
        foreach (var mime in mimeTypes)
        {
            if (!mime.StartsWith(s_instancePrefix, StringComparison.Ordinal))
                continue;

            var operationKey = mime.Substring(s_instancePrefix.Length);
            // Found our instance prefix — either return the real transfer or a safe dummy
            return s_inProcessDrags.TryGetValue(operationKey, out var transfer)
                ? transfer
                : new DataTransfer();
        }
        return null;
    }
}
