namespace Avalonia.Wayland.Server.Persistent;

/// <summary>
/// Worker-side accumulator for a single xdg_popup configure batch, sealed by
/// the wrapping xdg_surface.configure(serial). Mirrors <see cref="XdgConfigureBatch"/>
/// but carries the popup-specific (x, y, width, height) payload.
/// </summary>
internal class XdgPopupConfigureBatch
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public uint Serial;
}
