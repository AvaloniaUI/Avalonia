using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland.Clipboard;

/// <summary>
/// Implements <see cref="IPlatformDragSource"/> for the Wayland backend.
/// Delegates to <see cref="WaylandOutgoingTransfer.SetAsDrag"/> to create a
/// <c>wl_data_source</c> and call <c>wl_data_device.start_drag</c>.
/// </summary>
class WaylandDragSource : IPlatformDragSource
{
    public Task<DragDropEffects> DoDragDropAsync(
        PointerPressedEventArgs triggerEvent,
        IDataTransfer dataTransfer,
        DragDropEffects allowedEffects)
    {
        triggerEvent.Pointer.Capture(null);
        if (dataTransfer is not IAsyncDataTransfer asyncTransfer)
            return Task.FromResult(DragDropEffects.None);
        if (triggerEvent.PlatformInputEventCookie is not WaylandInputEventCookie inputCookie)
            return Task.FromResult(DragDropEffects.None);
        var outgoing = new WaylandOutgoingTransfer(asyncTransfer);
        return outgoing.SetAsDrag(inputCookie, allowedEffects);
    }
}
