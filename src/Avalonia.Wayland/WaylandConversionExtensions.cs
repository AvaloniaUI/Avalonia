using System;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Wayland.Server.Persistent;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland;

/// <summary>
/// Conversions between Avalonia and Wayland-protocol types. Mirrors the
/// per-platform <c>ToAvalonia</c>/<c>ToWayland</c>/<c>ToWin32</c>/etc.
/// extension-method pattern used by the other backends.
/// </summary>
internal static class WaylandConversionExtensions
{
    /// <summary>
    /// Translates Avalonia's <see cref="PopupPositionerParameters"/> (used
    /// by <see cref="IPopupPositioner.Update"/>) into the worker-side
    /// <see cref="XdgPopupPositionerParams"/>.
    ///
    /// Coordinate convention: <see cref="PopupPositionerParameters.AnchorRectangle"/>
    /// arrives in the parent's client-area logical coordinates (consistent with
    /// <see cref="ManagedPopupPositioner"/>'s usage of "parent client area"). On
    /// Wayland CSD surfaces the entire buffer is the "client area" — including
    /// the shadow margins — so the rect is passed through as buffer-relative
    /// logical coords. The worker-side <see cref="WXdgPopup"/> performs the
    /// buffer→geometry origin shift and clamps the rect into the parent's
    /// window-geometry rectangle (see <see cref="XdgPopupPositionerParams"/>).
    /// </summary>
    public static XdgPopupPositionerParams ToWayland(this PopupPositionerParameters parameters) =>
        new(
            Size: parameters.Size,
            AnchorRect: parameters.AnchorRectangle,
            Anchor: parameters.Anchor.ToWayland(),
            Gravity: parameters.Gravity.ToWayland(),
            ConstraintAdjustment: parameters.ConstraintAdjustment.ToWayland(),
            Offset: parameters.Offset);

    /// <summary>
    /// <see cref="PopupAnchor"/> is a bitfield (Top=1, Bottom=2, Left=4, Right=8,
    /// corners are bit combinations); the protocol's <see cref="XdgPositioner.AnchorEnum"/>
    /// is a dense enum (None=0..BottomRight=8). Translate explicitly per
    /// combination rather than reinterpret-casting.
    /// </summary>
    public static XdgPositioner.AnchorEnum ToWayland(this PopupAnchor a) => a switch
    {
        PopupAnchor.None => XdgPositioner.AnchorEnum.None,
        PopupAnchor.Top => XdgPositioner.AnchorEnum.Top,
        PopupAnchor.Bottom => XdgPositioner.AnchorEnum.Bottom,
        PopupAnchor.Left => XdgPositioner.AnchorEnum.Left,
        PopupAnchor.Right => XdgPositioner.AnchorEnum.Right,
        PopupAnchor.TopLeft => XdgPositioner.AnchorEnum.TopLeft,
        PopupAnchor.TopRight => XdgPositioner.AnchorEnum.TopRight,
        PopupAnchor.BottomLeft => XdgPositioner.AnchorEnum.BottomLeft,
        PopupAnchor.BottomRight => XdgPositioner.AnchorEnum.BottomRight,
        _ => throw new ArgumentOutOfRangeException(
            nameof(a), a, $"Unsupported PopupAnchor combination: {a}"),
    };

    /// <summary>
    /// Same shape concern as <see cref="ToWayland(PopupAnchor)"/>:
    /// <see cref="PopupGravity"/> is a bitfield, <see cref="XdgPositioner.GravityEnum"/>
    /// is dense.
    /// </summary>
    public static XdgPositioner.GravityEnum ToWayland(this PopupGravity g) => g switch
    {
        PopupGravity.None => XdgPositioner.GravityEnum.None,
        PopupGravity.Top => XdgPositioner.GravityEnum.Top,
        PopupGravity.Bottom => XdgPositioner.GravityEnum.Bottom,
        PopupGravity.Left => XdgPositioner.GravityEnum.Left,
        PopupGravity.Right => XdgPositioner.GravityEnum.Right,
        PopupGravity.TopLeft => XdgPositioner.GravityEnum.TopLeft,
        PopupGravity.TopRight => XdgPositioner.GravityEnum.TopRight,
        PopupGravity.BottomLeft => XdgPositioner.GravityEnum.BottomLeft,
        PopupGravity.BottomRight => XdgPositioner.GravityEnum.BottomRight,
        _ => throw new ArgumentOutOfRangeException(
            nameof(g), g, $"Unsupported PopupGravity combination: {g}"),
    };

    /// <summary>
    /// <see cref="PopupPositionerConstraintAdjustment"/> currently has the
    /// same numeric layout as <see cref="XdgPositioner.ConstraintAdjustmentEnum"/>,
    /// but that's not a contractual guarantee — translate flag-by-flag
    /// rather than reinterpret-casting.
    /// </summary>
    public static XdgPositioner.ConstraintAdjustmentEnum ToWayland(
        this PopupPositionerConstraintAdjustment a)
    {
        var r = XdgPositioner.ConstraintAdjustmentEnum.None;
        if ((a & PopupPositionerConstraintAdjustment.SlideX) != 0)
            r |= XdgPositioner.ConstraintAdjustmentEnum.SlideX;
        if ((a & PopupPositionerConstraintAdjustment.SlideY) != 0)
            r |= XdgPositioner.ConstraintAdjustmentEnum.SlideY;
        if ((a & PopupPositionerConstraintAdjustment.FlipX) != 0)
            r |= XdgPositioner.ConstraintAdjustmentEnum.FlipX;
        if ((a & PopupPositionerConstraintAdjustment.FlipY) != 0)
            r |= XdgPositioner.ConstraintAdjustmentEnum.FlipY;
        if ((a & PopupPositionerConstraintAdjustment.ResizeX) != 0)
            r |= XdgPositioner.ConstraintAdjustmentEnum.ResizeX;
        if ((a & PopupPositionerConstraintAdjustment.ResizeY) != 0)
            r |= XdgPositioner.ConstraintAdjustmentEnum.ResizeY;
        return r;
    }
}
