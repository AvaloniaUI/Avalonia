using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.SourceGenerator;
using Avalonia.Wayland.Server.Transient;

namespace Avalonia.Wayland.Server.Persistent;

/// <summary>
/// UI→worker proxy interface for the cross-thread surface API common to all WSurfaces.
/// </summary>
[GenerateCrossThreadProxy(
    typeof(WaylandDispatchPriority),
    "Avalonia.Wayland.Server.WaylandDispatchPriority.Normal",
    GeneratedClassName = "WSurfaceProxy")]
internal interface IWSurface
{
    /// <summary>Tears down all wayland-side resources and unregisters this surface.</summary>
    void Disconnect();

    /// <summary>Sets the desired cursor type and asks the input dispatcher to refresh the pointer cursor if this surface is focused.</summary>
    void SetCursor(StandardCursorType cursorType);

    /// <summary>
    /// Registers the UI-thread sink to receive worker→UI text-input v3 events
    /// (preedit/commit/delete-surrounding). Idempotent. Does nothing on
    /// compositors that lack zwp_text_input_manager_v3.
    /// </summary>
    void RegisterTextInputSink(Avalonia.Wayland.Server.Transient.WaylandTextInputV3EventsProxy sink);

    /// <summary>
    /// Toggles the text-input client. Sets the compose-key gate
    /// (XKB-compose runs only when a client is attached) and, when v3 is
    /// available, drives enable/disable on all entered seats. The
    /// <paramref name="sessionToken"/> stamps every subsequent worker→UI
    /// IME callback so the UI broker can drop callbacks emitted for a
    /// previous client.
    /// </summary>
    void SetTextInputActive(bool hasClient, bool supportsPreedit, bool supportsSurroundingText, int sessionToken);

    /// <summary>Aborts any in-flight IME composition (disable+commit on entered seats).</summary>
    void AbortTextInputComposition();

    /// <summary>Updates the IME cursor rectangle (logical, surface-local coordinates).</summary>
    void SetTextInputCursorRect(Rect rect);

    /// <summary>Updates the IME content-type (hint+purpose).</summary>
    void SetTextInputOptions(TextInputOptions options);

    /// <summary>
    /// Updates surrounding text + selection. <paramref name="cursorChar"/> /
    /// <paramref name="anchorChar"/> are UTF-16 code-unit offsets into the
    /// supplied string (.NET-native indexing).
    /// </summary>
    void SetTextInputSurroundingText(string text, int cursorChar, int anchorChar);

    /// <summary>Resets the IME state (clears any pending preedit/commit).</summary>
    void ResetTextInput();
}
