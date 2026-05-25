using System.Threading.Tasks;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Input.Platform;
using global::Avalonia.Logging;

namespace Avalonia.WinUI;

/// <summary>
/// <see cref="IPlatformDragSource"/> implementation that initiates native WinUI
/// drags (via <see cref="Microsoft.UI.Xaml.UIElement.StartDragAsync"/>) for
/// content hosted inside an <see cref="AvaloniaSwapChainPanel"/>. WinUI only
/// permits starting a drag with a <see cref="Microsoft.UI.Input.PointerPoint"/>
/// captured during an active pointer interaction; the panel keeps the most
/// recent one per pointer id, and we look it up by the triggering Avalonia
/// pointer's id.
/// </summary>
internal sealed class WinUIDragSource : IPlatformDragSource
{
    public async Task<DragDropEffects> DoDragDropAsync(
        PointerPressedEventArgs triggerEvent,
        IDataTransfer dataTransfer,
        DragDropEffects allowedEffects)
    {
        // Resolve the panel that hosts the visual that started the drag.
        var topLevel = TopLevel.GetTopLevel(triggerEvent.Source as Visual);
        var panel = topLevel?.PlatformImpl is { } impl
            ? AvaloniaSwapChainPanel.GetPanelFor(impl)
            : null;

        if (panel is null)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.WinUIPlatform)?.Log(this,
                "Could not resolve AvaloniaSwapChainPanel for drag source. " +
                "TopLevel={TopLevel}, PlatformImpl={PlatformImpl}.",
                topLevel?.GetType().Name ?? "null",
                topLevel?.PlatformImpl?.GetType().Name ?? "null");
            return DragDropEffects.None;
        }

        return await panel.StartOutgoingDragAsync(dataTransfer, allowedEffects);
    }
}
