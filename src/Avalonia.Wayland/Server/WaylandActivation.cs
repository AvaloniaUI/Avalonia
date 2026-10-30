using System;
using System.Collections.Generic;
using Avalonia.Logging;

namespace Avalonia.Wayland.Server;

/// <summary>
/// Latest-token activation. Only the opaque UI connection identity crosses threads;
/// token storage, target validation and protocol submission belong to the worker.
/// </summary>
internal sealed class WaylandActivation(
    Action<Action> postWorker, Action<Action> postUI, Func<bool> protocolAvailable)
{
    private object? _uiConnection; // UI thread only
    private object? _connection; // Worker thread only, including all state below
    private string? _token;
    private readonly Dictionary<Target, string> _pending = new();

    // A target lives for one Show/Hide lifecycle. Reconnects retain the target but
    // change its connection and reset mapping. Delegates run only on the worker.
    internal sealed class Target(Func<object?> connection, Action<string> submit)
    {
        internal readonly Func<object?> Connection = connection;
        internal readonly Action<string> Submit = submit;
        internal bool Mapped;
        internal bool Cancelled;
    }

    // Called on UI after the initial worker handshake, before the dispatcher runs.
    public void Initialize(object connection) => _uiConnection = connection;

    public void OnConnected(object connection, bool notifyUI)
    {
        _connection = connection;
        if (notifyUI)
            postUI(() => _uiConnection = connection);
    }

    public void OnDisconnected()
    {
        var old = _connection;
        _connection = null;
        Clear();
        postUI(() =>
        {
            if (ReferenceEquals(_uiConnection, old))
                _uiConnection = null;
        });
    }

    // These three UI entry points capture identity BEFORE enqueueing. The OOB
    // queue survives reconnects, so checking identity only at delivery is too late.
    public void Deliver(string? token)
    {
        var connection = _uiConnection;
        postWorker(() =>
        {
            if (!IsCurrent(connection) || !protocolAvailable())
                return;
            if (_token != null)
                Log("Replacing the pending tray activation token.");
            _token = string.IsNullOrEmpty(token) ? null : token;
        });
    }

    public void Activate(Target target)
    {
        var connection = _uiConnection;
        postWorker(() =>
        {
            if (!IsCurrent(connection) || !CanActivate(target) || !protocolAvailable())
                return;
            if (_token is { } token)
            {
                _token = null;
                _pending[target] = token;
                Log("Consuming a tray activation token for a window.");
            }
            Submit(target);
        });
    }

    public void Cancel(Target target)
    {
        var connection = _uiConnection;
        postWorker(() =>
        {
            if (!IsCurrent(connection))
                return;
            target.Cancelled = true;
            if (_pending.Remove(target))
                Log("Suppressing a pending activation for a hidden or closed window.");
        });
    }

    // Called only AFTER an actual buffer commit, never from the before-attach
    // hook. The renderer also checks that the committed wl_surface is still live.
    public void BufferCommitted(Target target, object? connection)
    {
        if (!IsCurrent(connection) || !CanActivate(target))
            return;
        target.Mapped = true;
        Submit(target);
    }

    public void SurfaceDisconnected(Target target)
    {
        _pending.Remove(target);
        target.Mapped = false;
    }

    // Protocol removal clears managed requests without inventing protocol epochs.
    public void Clear()
    {
        _token = null;
        _pending.Clear();
    }

    private bool IsCurrent(object? connection) =>
        connection != null && ReferenceEquals(connection, _connection);

    private bool CanActivate(Target target) => !target.Cancelled && IsCurrent(target.Connection());

    private void Submit(Target target)
    {
        if (target.Mapped && CanActivate(target) && protocolAvailable()
            && _pending.Remove(target, out var token))
            target.Submit(token);
    }

    private void Log(string message) =>
        Logger.TryGet(LogEventLevel.Information, LogArea.FreeDesktopPlatform)?.Log(this, message);
}
