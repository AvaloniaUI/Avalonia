using System;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Wayland.Embedding.Compositor;
using Avalonia.Wayland.Embedding.Hosting;

namespace Avalonia.Wayland.Embedding;

/// <summary>
/// A live publication of an Avalonia <see cref="Window"/> as an xdg-foreign handle (scenario 3). Hand
/// <see cref="Handle"/> to a toolkit (out of band) so it can <c>zxdg_importer_v2.import_toplevel(handle)</c> +
/// <c>set_parent_of(itsDialogSurface)</c> to parent its dialog to the window. Dispose — or let the window close —
/// to revoke; a later import of the handle is then inert. UI-thread-affined; <see cref="Dispose"/> is idempotent.
/// </summary>
public sealed class WaylandForeignExport : IDisposable
{
    private readonly Window _window;
    private bool _revoked;

    internal WaylandForeignExport(string handle, Window window)
    {
        Handle = handle;
        _window = window;
        _window.Closed += OnWindowClosed; // closing the exported window revokes the publication
    }

    /// <summary>The opaque foreign handle string to hand to the toolkit.</summary>
    public string Handle { get; }

    private void OnWindowClosed(object? sender, EventArgs e) => Dispose();

    public void Dispose()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (_revoked)
            return;
        _revoked = true;
        _window.Closed -= OnWindowClosed;
        WaylandHosting.RevokeExportedWindow(Handle);
        WaylandEmbeddingSubcompositor.Api.RevokeHostWindowExport(Handle);
    }
}
