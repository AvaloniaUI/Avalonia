namespace Avalonia.Wayland.Server.Persistent;

/// <summary>
/// Worker-side accumulator for a single xdg_popup configure batch, sealed by
/// the wrapping xdg_surface.configure(serial). Mirrors <see cref="XdgConfigureBatch"/>
/// but carries the popup-specific (x, y, width, height) payload.
/// </summary>
internal class XdgPopupConfigureBatch
{
    /// <summary>
    /// Raw xdg_popup.configure payload in logical pixels: the popup's window geometry,
    /// positioned relative to the parent's window geometry. Null when the batch is a bare
    /// xdg_surface.configure. Translation to buffer-relative coordinates happens UI-side,
    /// against the UI's own (possibly not yet committed) shadow extents.
    /// </summary>
    public PixelRect? Geometry { get; set; }
    public uint Serial;
}
