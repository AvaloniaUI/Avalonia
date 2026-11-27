using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland.Server.Persistent;

/// <summary>
/// Worker-side surface that can parent an <c>xdg_popup</c>. An xdg toplevel
/// and a wlr layer surface both qualify, but they attach the popup with
/// different protocol calls: the toplevel passes itself as the popup's
/// parent, while a layer surface creates the popup with a null parent and
/// then calls <c>zwlr_layer_surface_v1.get_popup</c> before the popup's
/// first commit.
/// </summary>
internal interface IWXdgPopupParent
{
    /// <summary>True once the parent has committed its first frame.</summary>
    bool IsMapped { get; }

    /// <summary>
    /// True when the concrete parent object still exists and is mapped, so a
    /// popup can be attached to it right now.
    /// </summary>
    bool IsPopupParentReady { get; }

    /// <summary>
    /// Parent geometry the positioner anchor rect is clamped against, in
    /// surface-local logical pixels. A layer surface has no separate window
    /// geometry, so it reports its whole configured surface.
    /// </summary>
    (int Left, int Top, int Width, int Height)? PopupParentGeometry { get; }

    /// <summary>
    /// Creates the protocol popup and attaches it to this parent. The caller
    /// owns committing the popup's <c>wl_surface</c>.
    /// </summary>
    XdgPopup CreatePopup(
        XdgSurface popupSurface,
        XdgPositioner positioner,
        XdgPopup.Listener listener);

    void RegisterPendingChildPopup(WXdgPopup popup);
    void UnregisterPendingChildPopup(WXdgPopup popup);
}
