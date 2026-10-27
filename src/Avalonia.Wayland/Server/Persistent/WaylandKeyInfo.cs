using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.Wayland.Server.Persistent;

/// <summary>
/// A key resolved against the xkb state on the worker thread.
/// </summary>
internal sealed record WaylandKeyInfo(
    Key Key,
    PhysicalKey PhysicalKey,
    string? KeySymbol,
    string? KeyText,
    string? KeyTextIgnoringModifiers,
    RawInputModifiers? ConsumedModifiers,
    NativeKeyInfo NativeKeyInfo);
