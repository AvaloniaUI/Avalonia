using System;
using Avalonia.Threading;
using Avalonia.Wayland.Server.Interop;
using NWayland.Protocols.XdgSessionManagementV1;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland.Server.Transient;

/// <summary>
/// The compositor session keeping named toplevels' window management state, for one connection.
/// </summary>
class XdgSession
{
    private readonly XdgSessionV1 _session;
    private readonly WaylandConnection _connection;
    // set once another client takes the session over, which makes it inert
    private bool _replaced;

    public XdgSession(XdgSessionManagerV1 manager, WaylandConnection connection, WaylandWorker worker,
        WaylandPlatformOptions options)
    {
        _connection = connection;
        // a session already confirmed on an earlier connection is being recovered after a reconnect
        var requested = worker.SessionId ?? options.SessionId;
        var reason = worker.SessionId is null
            ? XdgSessionManagerV1.ReasonEnum.Launch
            : XdgSessionManagerV1.ReasonEnum.Recover;
        _session = manager.GetSession(reason, requested, new Listener(this, worker, requested, options.SessionCreated),
            connection.Queue);
    }

    /// <summary>
    /// Joins a toplevel that has not been committed yet to the session under <paramref name="name"/>,
    /// restoring the state the compositor kept for that name; <c>null</c> when the session was replaced.
    /// <paramref name="restored"/> is called on the worker thread, before the initial configure, when it does.
    /// </summary>
    public XdgToplevelSessionV1? Restore(XdgToplevel toplevel, string name, Action restored) =>
        _replaced ? null : _session.RestoreToplevel(toplevel, name, new ToplevelListener(restored), _connection.Queue);

    private class Listener(XdgSession p, WaylandWorker worker, string? requested, Action<string>? created)
        : XdgSessionV1.Listener
    {
        protected override void Created(XdgSessionV1 eventSender, string sessionId)
        {
            worker.SessionId = sessionId;
            if (created != null)
                Dispatcher.UIThread.Post(() => created(sessionId));
        }

        protected override void Restored(XdgSessionV1 eventSender) => worker.SessionId = requested;

        protected override void Replaced(XdgSessionV1 eventSender) => p._replaced = true;
    }

    private class ToplevelListener(Action restored) : XdgToplevelSessionV1.Listener
    {
        protected override void Restored(XdgToplevelSessionV1 eventSender) => restored();
    }
}
