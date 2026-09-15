using Avalonia.SourceGenerator;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>
/// Dispatch priority for UI→compositor cross-thread proxy calls. The subcompositor has a single worker
/// queue, so there is exactly one priority — the type exists only to satisfy the proxy generator's marshaller
/// signature.
/// </summary>
internal enum EmbedderDispatchPriority
{
    Normal
}

/// <summary>
/// UI→compositor cross-thread API. The generated <c>WaylandEmbedderApiProxy</c> marshals every call onto the
/// compositor worker thread (through <see cref="WaylandCompositorWorker"/>'s post queue); the implementation
/// runs there with direct access to <see cref="CompositorState"/>. Void methods are fire-and-forget; the two
/// request/response methods return a value, which the generator wraps into a <c>Task&lt;T&gt;</c> — the UI side
/// drives it to completion with <see cref="WaylandEmbeddingSubcompositor.Roundtrip"/> (see the hand-written
/// wrappers). Ordering against a following roundtrip is preserved because the proxy posts through the same queue.
/// </summary>
[GenerateCrossThreadProxy(
    typeof(EmbedderDispatchPriority),
    "Avalonia.Wayland.Embedding.Compositor.EmbedderDispatchPriority.Normal",
    GeneratedClassName = "WaylandEmbedderApiProxy")]
internal interface IWaylandEmbedderApi
{
    /// <summary>Send <c>wl_surface.preferred_buffer_scale</c> to the host's toplevel root (HiDPI hint, v6+).</summary>
    void SetHostScale(uint hostId, int scale);

    /// <summary>Send xdg_toplevel.close — the "user wants to close" hint.</summary>
    void CloseToplevel(uint hostId);

    /// <summary>Send xdg_popup.popup_done — dismiss the host's popup.</summary>
    void DismissPopup(uint hostId);

    /// <summary>Drive the host toplevel's <c>activated</c> state (follows the containing Window activation).</summary>
    void SetActivated(uint hostId, bool activated);

    /// <summary>Configure the host's client to re-lay-out at the given size.</summary>
    void ConfigureToplevel(uint hostId, int width, int height);

    /// <summary>Fire the deferred frame callbacks for the surfaces the host view just rendered.</summary>
    void FireFrameCallbacks(uint[] surfaceIds);

    /// <summary>Forward a pointer event to the host toplevel's wl_pointer.</summary>
    void DeliverPointer(PointerInputArgs input);

    /// <summary>Forward a keyboard focus/key event to the host toplevel's wl_keyboard.</summary>
    void DeliverKeyboard(KeyboardInputArgs input);

    /// <summary>Forward a text-input focus/commit/preedit event to the host toplevel's zwp_text_input_v3.</summary>
    void DeliverTextInput(TextInputArgs input);

    /// <summary>Register a host-side foreign handle referring to an exported Avalonia Window (scenario 3).</summary>
    void RegisterHostWindowExport(string handle);

    /// <summary>Revoke a previously registered host-window export handle.</summary>
    void RevokeHostWindowExport(string handle);

    /// <summary>Register a scenario-5 content cookie so a later mark_content_surface can validate it.</summary>
    void RegisterContentCookie(string cookie);

    /// <summary>Drop a content cookie that was registered but never marked.</summary>
    void UnregisterContentCookie(string cookie);

    /// <summary>
    /// Register a scenario-1 embedding token and return the host id the compositor allocated for it (which
    /// <c>embed_toplevel(token)</c> will resolve to). Echo-back: the generator returns this as a <c>Task&lt;uint&gt;</c>.
    /// </summary>
    uint RegisterEmbedTokenAsync(string token);

    /// <summary>
    /// Resolve a foreign-import handle (scenario 4) to the host id of the control hosting the exported toplevel,
    /// or 0 if the handle is unknown/unmapped. Echo-back: returned as a <c>Task&lt;uint&gt;</c>.
    /// </summary>
    uint ResolveForeignImportAsync(string handle);
}
