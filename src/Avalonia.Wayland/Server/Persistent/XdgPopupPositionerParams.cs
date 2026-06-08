using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland.Server.Persistent;

/// <summary>
/// Flat, immutable description of an xdg_positioner request. Built UI-side
/// in the parent's <b>buffer-relative</b> logical coordinate system —
/// i.e. relative to the true top-left of the parent surface, including any
/// CSD shadow margins. The worker is responsible for translating into the
/// parent's xdg_surface window-geometry coordinate system (subtracting the
/// parent's shadow extents) and clamping the anchor rectangle to that
/// geometry, since the protocol requires that <c>"the anchor rectangle may
/// not extend outside the window geometry of the positioned child's parent
/// surface"</c>. Doing the translation worker-side keeps the UI oblivious
/// to the wayland buffer-vs-geometry distinction.
/// </summary>
/// <param name="Size">The size of the popup, in logical surface-local coordinates. Both width and height must be positive.</param>
/// <param name="AnchorRect">The anchor rectangle in the parent's <b>buffer-relative</b> logical coordinates (true top-left, includes shadow).</param>
/// <param name="Anchor">The edge/corner of the anchor rect that the popup is anchored to. Uses the NWayland-provided wire enum directly — Avalonia's bitfield <see cref="Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor"/> must be translated explicitly UI-side.</param>
/// <param name="Gravity">The direction the popup expands from the anchor point. Uses the NWayland-provided wire enum directly — same translation note as <paramref name="Anchor"/>.</param>
/// <param name="ConstraintAdjustment">How the compositor may adjust the popup if it doesn't fit on screen. NWayland-provided <c>[Flags]</c> wire enum; Avalonia's <see cref="Avalonia.Controls.Primitives.PopupPositioning.PopupPositionerConstraintAdjustment"/> must be translated flag-by-flag UI-side, never reinterpret-cast.</param>
/// <param name="Offset">Additional offset applied to the resolved popup position (after anchor + gravity have been resolved). Already relative to the popup itself, not the parent — no shadow translation applies.</param>
internal readonly record struct XdgPopupPositionerParams(
    Size Size,
    Rect AnchorRect,
    XdgPositioner.AnchorEnum Anchor,
    XdgPositioner.GravityEnum Gravity,
    XdgPositioner.ConstraintAdjustmentEnum ConstraintAdjustment,
    Point Offset);
