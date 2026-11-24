using System;
using Avalonia;

namespace Avalonia.Wayland;

/// <summary>
/// Layer to place a <c>zwlr_layer_surface_v1</c> in. Mirrors the protocol's
/// <c>zwlr_layer_shell_v1.layer</c> enum.
/// </summary>
public enum LayerShellLayer
{
    Background = 0,
    Bottom = 1,
    Top = 2,
    Overlay = 3,
}

/// <summary>
/// Edges a layer surface is anchored to. Mirrors the protocol's
/// <c>zwlr_layer_surface_v1.anchor</c> bitmask.
/// </summary>
[Flags]
public enum LayerShellAnchor
{
    None = 0,
    Top = 1,
    Bottom = 2,
    Left = 4,
    Right = 8,
}

/// <summary>
/// Keyboard-interactivity mode for a layer surface. Mirrors the protocol's
/// <c>zwlr_layer_surface_v1.keyboard_interactivity</c> enum.
/// </summary>
public enum LayerShellKeyboardInteractivity
{
    None = 0,
    Exclusive = 1,
    OnDemand = 2,
}

/// <summary>
/// Describes the layer surface that
/// <see cref="WaylandLayerShell.CreateWindowImpl"/> should create.
/// </summary>
/// <remarks>
/// <para>
/// Width and height are in logical (surface-local) pixels; they are passed
/// to <c>zwlr_layer_surface_v1.set_size</c>. Both must be greater than zero.
/// </para>
/// <para>
/// With <see cref="Anchor"/> set to <see cref="LayerShellAnchor.Top"/>, the
/// compositor displays the surface centred horizontally on the output's top
/// edge, so a pill-sized layer surface stays top-centre without any manual
/// screen-width calculation. Adding left/right anchors makes KWin 6.8.3
/// left-align the surface instead, so top-centre surfaces should use
/// <see cref="LayerShellAnchor.Top"/> alone.
/// </para>
/// </remarks>
public sealed record LayerShellOptions
{
    /// <summary>Layer to place the surface in. Defaults to <see cref="LayerShellLayer.Overlay"/>.</summary>
    public LayerShellLayer Layer { get; init; } = LayerShellLayer.Overlay;

    /// <summary>
    /// Edges to anchor the surface to. Defaults to <see cref="LayerShellAnchor.Top"/>,
    /// which the compositor centres horizontally on the output's top edge.
    /// Anchoring to left and right as well makes KWin align the surface to
    /// the left edge instead of centring it (observed with KWin 6.8.3), so
    /// top-centre surfaces should use <see cref="LayerShellAnchor.Top"/>.
    /// </summary>
    public LayerShellAnchor Anchor { get; init; } = LayerShellAnchor.Top;

    /// <summary>
    /// Exclusive zone in logical pixels. <c>-1</c> means "ignore this surface
    /// for the purposes of positioning other surfaces" (the default), which
    /// is what an overlay wants.
    /// </summary>
    public int ExclusiveZone { get; init; } = -1;

    /// <summary>
    /// Margin between the anchored edges and the surface, in logical pixels.
    /// Only the margins for edges present in <see cref="Anchor"/> are used by
    /// the compositor.
    /// </summary>
    public Thickness Margin { get; init; }

    /// <summary>
    /// Whether the surface accepts keyboard focus. Defaults to
    /// <see cref="LayerShellKeyboardInteractivity.None"/> so a passive
    /// surface never steals focus from the active application.
    /// </summary>
    public LayerShellKeyboardInteractivity KeyboardInteractivity { get; init; } =
        LayerShellKeyboardInteractivity.None;

    /// <summary>
    /// Namespace advertised to the compositor. Useful for debugging and for
    /// compositor rules.
    /// </summary>
    public string Namespace { get; init; } = "avalonia";

    /// <summary>
    /// Initial surface width in logical pixels. Must be greater than zero.
    /// </summary>
    public double Width { get; init; }

    /// <summary>
    /// Initial surface height in logical pixels. Must be greater than zero.
    /// </summary>
    public double Height { get; init; }

    internal void Validate()
    {
        if (!double.IsFinite(Width) || Width <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(Width), Width, "Width must be a finite, positive number.");

        if (!double.IsFinite(Height) || Height <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(Height), Height, "Height must be a finite, positive number.");

        if (string.IsNullOrWhiteSpace(Namespace))
            throw new ArgumentException(
                "Namespace must not be empty.", nameof(Namespace));
    }
}
