using System;
using Avalonia.Controls.Presenters;
using Avalonia.Threading;
using Avalonia.Wayland.Embedding.Compositor;

namespace Avalonia.Wayland.Embedding.Hosting;

/// <summary>
/// Scenario 5: places <b>Avalonia</b> content INSIDE an embedded toolkit window, at a toolkit-driven rect. It is
/// not a <see cref="Avalonia.Controls.Control"/> itself — it inserts <see cref="Content"/> as a positioned child
/// in the target host control's content layer; rendering is pure Avalonia (no wl_surface is involved for the
/// content). The target host is resolved from the toolkit's container surface via
/// <c>avalonia_embed.mark_content_surface(surface, cookie)</c>, so this content host does not know it up front.
/// UI-thread-affined. The glue OWNS this object and is expected to <see cref="Dispose"/> it when it no longer needs
/// the content embedded; the finalizer is only a GC backstop for a glue that forgets.
/// </summary>
public class WaylandSubcompositorAvaloniaContentHost : AvaloniaObject, IDisposable
{
    private readonly ContentPresenter _presenter = new();
    private WaylandSubcompositorControlHost? _target;
    private Rect _rect;
    private Rect? _clip;
    private object? _content;
    private string? _cookie; // the live cookie binding this host (durable until Detach/Dispose; null if never minted)

    /// <summary>The Avalonia content rendered over the toolkit content, hosted by an internal presenter.</summary>
    public object? Content
    {
        get => _content;
        set
        {
            _content = value;
            _presenter.Content = value;
        }
    }

    /// <summary>True once bound to a host control (via the cookie protocol or <see cref="AttachTo"/>).</summary>
    public bool IsResolved { get; private set; }

    /// <summary>Raised when bound to a host control.</summary>
    public event EventHandler? Resolved;

    /// <summary>Raised when the binding is dropped — explicitly (<see cref="Detach"/>) or because the toolkit
    /// window hosting the content went away. Lets glue re-mint a cookie and re-attach.</summary>
    public event EventHandler? Detached;

    /// <summary>
    /// Mint a cookie identifying THIS content host and round-trip the compositor so it is live before returning.
    /// Hand it to the toolkit, which tags its container surface (its own window's wl_surface, not a placeholder)
    /// via <c>avalonia_embed.mark_content_surface(surface, cookie)</c>; the target host control is then resolved
    /// from that surface.
    /// </summary>
    public string CreateAttachmentCookie()
    {
        Dispatcher.UIThread.VerifyAccess();
        var cookie = Guid.NewGuid().ToString("N");
        _cookie = cookie;
        WaylandHosting.RegisterContentCookie(cookie, this);
        WaylandEmbeddingSubcompositor.Api.RegisterContentCookie(cookie);
        WaylandEmbeddingSubcompositor.Roundtrip(); // cookie live compositor-side before the toolkit marks its surface
        return cookie;
    }

    /// <summary>Shortcut when the glue already holds the target host (e.g. it created it in scenario 1): attach
    /// directly, skipping the cookie protocol + roundtrip.</summary>
    public void AttachTo(WaylandSubcompositorControlHost host)
    {
        Dispatcher.UIThread.VerifyAccess();
        ResolveTo(host);
    }

    /// <summary>
    /// Scenario 5 glue helper: emit <c>avalonia_embed.mark_content_surface(containerSurface, cookie)</c> over
    /// <paramref name="connection"/> (the toolkit's own connection, with the embedder bound once). Pass the embedded
    /// window's own <c>wl_surface*</c>. Mints a cookie if one wasn't already created. Returns once accepted, throws
    /// on <c>rejected</c>.
    /// </summary>
    public void AttachToClientSurface(WaylandEmbedderConnection connection, IntPtr containerWlSurfacePtr)
    {
        Dispatcher.UIThread.VerifyAccess();
        var cookie = _cookie ?? CreateAttachmentCookie();
        var outcome = connection.MarkContentSurface(containerWlSurfacePtr, cookie);
        if (outcome != EmbedOutcome.Bound)
            throw new InvalidOperationException($"avalonia_embed.mark_content_surface did not bind ({outcome}).");
    }

    /// <summary>Glue sets placement + optional clip within the host control (host coordinate space), on every
    /// toolkit size-allocate/scroll. There is no surface to inherit clipping from, so the glue reports the
    /// visible clipped rect itself; a null clip clips to <paramref name="rect"/>.</summary>
    public void UpdateContentRect(Rect rect, Rect? clip = null)
    {
        _rect = rect;
        _clip = clip;
        _target?.SetContentOverlayRect(rect, clip);
    }

    /// <summary>Permanently detach: clear the host's content layer AND drop the cookie, so a later mark/map for it
    /// is inert. The cookie is dropped BEFORE raising <see cref="Detached"/> so a handler may re-mint and re-attach
    /// without the just-minted cookie being clobbered.</summary>
    public void Detach()
    {
        ReleaseCookie();
        Unbind();
    }

    internal void ResolveTo(WaylandSubcompositorControlHost host)
    {
        if (ReferenceEquals(_target, host))
            return;
        // Re-targeting a still-bound content host (a repeated mark_content_surface of the same live cookie onto
        // another surface): unbind the old host but KEEP the cookie, so the relationship survives the move. (A
        // toolkit-window teardown is NOT this path — it goes through OnTargetUnmapped → Detach, releasing the
        // cookie.) The shared presenter has one visual parent, so it must leave the old host before joining the new.
        Unbind();
        _target = host;
        host.EmbeddedSurfaceUnmapped += OnTargetUnmapped; // the toolkit window going away unbinds us (below)
        host.SetContentOverlay(_presenter);
        host.SetContentOverlayRect(_rect, _clip);
        IsResolved = true;
        Resolved?.Invoke(this, EventArgs.Empty);
    }

    // The toolkit window hosting our content went away (host Stop()). Tear down fully — the cookie is one window's
    // container; a fresh window must re-mark to re-bind (the glue can re-mint on Detached, per its doc).
    private void OnTargetUnmapped(object? sender, EventArgs e) => Detach();

    // Drop the binding to the current host control (clears the overlay) WITHOUT touching the durable cookie.
    private void Unbind()
    {
        if (_target is not null)
        {
            _target.EmbeddedSurfaceUnmapped -= OnTargetUnmapped;
            _target.SetContentOverlay(null);
            _target = null;
        }
        if (IsResolved)
        {
            IsResolved = false;
            Detached?.Invoke(this, EventArgs.Empty);
        }
    }

    // Drop the cookie from both registries so it no longer binds this host (UI map entry + compositor registration).
    private void ReleaseCookie()
    {
        if (_cookie is null)
            return;
        WaylandHosting.RemoveContentCookie(_cookie);
        WaylandEmbeddingSubcompositor.Api.UnregisterContentCookie(_cookie);
        _cookie = null;
    }

    /// <summary>Deterministic teardown — the glue should Dispose this content host (on the UI thread) when it no
    /// longer needs the content embedded. Detaches from the host control, drops the cookie, and suppresses the
    /// finalizer backstop. Idempotent; do not reuse after disposing.</summary>
    public void Dispose()
    {
        Detach();
        GC.SuppressFinalize(this);
    }

    // GC backstop ONLY: if the glue drops this content host without Dispose()/Detach(), sever the durable cookie on
    // the compositor thread. Runs on the finalizer thread, so it ONLY dispatches through the cross-thread proxy
    // (thread-safe — it just posts) and reads the cookie string — it must NOT touch the UI-affined presenter/target
    // or the static UI registry (which holds us weakly and self-sweeps). A no-op if the cookie was already released.
    ~WaylandSubcompositorAvaloniaContentHost()
    {
        if (_cookie is { } cookie)
            WaylandEmbeddingSubcompositor.Api.UnregisterContentCookie(cookie);
    }
}
