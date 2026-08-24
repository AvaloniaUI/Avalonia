using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.SourceGenerator;
using Avalonia.Threading;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Transient.Clipboard;

namespace Avalonia.Wayland.Server.Persistent;

/// <summary>
/// Worker → UI sink for surface-role-agnostic events (input, scale changes,
/// keyboard focus loss, drag-and-drop). Both top-levels and popups receive
/// these. Surface-role-specific events (xdg_toplevel configure/close,
/// xdg_popup configure/done) live on the derived sink interfaces.
/// </summary>
[GenerateCrossThreadProxy(
    typeof(DispatcherPriority),
    "default",
    GeneratedClassName = "WSurfaceEventSinkProxy")]
internal interface IWSurfaceEventSink
{
    void OnPointerEnter(ulong timestamp, uint serial, Point position);
    void OnPointerLeave(uint serial);
    void OnPointerMotion(ulong timestamp, Point position, RawInputModifiers modifiers);
    void OnPointerButton(ulong timestamp, uint serial, RawPointerEventType type, RawInputModifiers modifiers, Point position, object? platformCookie);
    void OnPointerAxis(ulong timestamp, Vector delta, RawInputModifiers modifiers, Point position);

    void OnTouchDown(ulong timestamp, int touchId, Point position, object? platformCookie);
    void OnTouchMove(ulong timestamp, int touchId, Point position);
    void OnTouchUp(ulong timestamp, int touchId, Point position);
    void OnTouchCancel(int touchId, Point position);

    void OnKeyDown(ulong timestamp, Key key, RawInputModifiers modifiers, PhysicalKey physicalKey, string? keySymbol);
    void OnKeyUp(ulong timestamp, Key key, RawInputModifiers modifiers, PhysicalKey physicalKey, string? keySymbol);
    void OnKeyboardLeave();
    void OnKeyRepeatInfo(int rate, int delay);

    void OnDragEnter(Point position, string[] mimeTypes, WaylandOfferCookie offerCookie, DragDropEffects sourceEffects, RawInputModifiers modifiers);
    void OnDragMotion(Point position, RawInputModifiers modifiers);
    void OnDragLeave();
    void OnDrop(Point position, RawInputModifiers modifiers);

    void OnScaleChanged(double scale);

    /// <summary>
    /// Pushed from the worker whenever this surface's set of
    /// <c>wl_output</c> enters / leaves changes (also on disconnect).
    /// The list contains the opaque identity objects used as
    /// <see cref="Avalonia.Wayland.Screens.WaylandOutputSnapshot.Id"/>;
    /// ordering is insertion (last-entered last), which the UI side uses
    /// to pick "the" screen for the window.
    /// </summary>
    void OnSurfaceOutputsChanged(System.Collections.Generic.IReadOnlyList<object> outputIds);
}

/// <summary>
/// Worker → UI sink for xdg_toplevel-specific events. Implemented on the UI
/// thread by <see cref="WindowImpl.Sink"/>; the worker holds a generated
/// <see cref="WXdgTopLevelEventSinkProxy"/> that marshals every call to the
/// UI dispatcher.
/// </summary>
[GenerateCrossThreadProxy(
    typeof(DispatcherPriority),
    "default",
    GeneratedClassName = "WXdgTopLevelEventSinkProxy")]
internal interface IWXdgTopLevelEventSink : IWSurfaceEventSink
{
    void OnConfigure(XdgConfigureBatch batch);
    void OnClose();
    /// <summary>
    /// The compositor changed the effective decoration mode for this
    /// toplevel via <c>zxdg_toplevel_decoration_v1.configure</c>.
    /// Delivered <em>after</em> the initial-handshake mode (which the
    /// UI already sees via <see cref="XdgConfigureBatch.InitialDecorationMode"/>),
    /// for every subsequent <c>decoration.configure</c> event. The
    /// client is required by the protocol to obey the new mode.
    /// </summary>
    void OnDecorationModeChanged(DecorationMode mode);
}

/// <summary>
/// Worker → UI sink for xdg_popup-specific events. Implemented on the UI
/// thread by <c>PopupImpl.Sink</c>; the worker holds a generated
/// <c>WXdgPopupEventSinkProxy</c> that marshals every call to the UI
/// dispatcher.
/// </summary>
[GenerateCrossThreadProxy(
    typeof(DispatcherPriority),
    "default",
    GeneratedClassName = "WXdgPopupEventSinkProxy")]
internal interface IWXdgPopupEventSink : IWSurfaceEventSink
{
    /// <summary>
    /// Compositor delivered a new popup configure (x, y, width, height),
    /// sealed by the wrapping xdg_surface.configure(serial).
    /// </summary>
    void OnPopupConfigure(XdgPopupConfigureBatch batch);

    /// <summary>
    /// Compositor dismissed the popup (xdg_popup.popup_done). The popup
    /// must be torn down; after this no further events arrive.
    /// </summary>
    void OnPopupDone();
}

