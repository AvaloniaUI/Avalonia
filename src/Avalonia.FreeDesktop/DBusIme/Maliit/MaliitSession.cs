using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop.DBusIme.Maliit;

/// <summary>Managed peer DBus transport; no Qt library, shell command or global IME replacement.</summary>
internal sealed class MaliitSession : IMaliitSession, IPathMethodHandler
{
    private const string ServerPath = "/com/meego/inputmethod/uiserver1";
    private const string ServerInterface = "com.meego.inputmethod.uiserver1";
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(4);
    private readonly CancellationTokenSource _closed = new();
    private readonly SemaphoreSlim _send = new(1);
    private DBusConnection? _peer;
    private MaliitWidgetState? _state;
    private bool _disposed;
    private Task? _disconnectWatcher;
    public Func<MaliitEvent, Task<MaliitWidgetState?>>? DispatchAsync { get; set; }
    public bool IsConnected { get; private set; }
    public string Path => "/com/meego/inputmethod/inputcontext";
    public bool HandlesChildPaths => false;

    public async Task OpenAsync(MaliitWidgetState state, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_peer is not null) throw new InvalidOperationException("A focus session can only be opened once.");
        _state = state;
        using var bus = new DBusConnection(DBusAddress.Session!);
        await CompleteAsync(bus.ConnectAsync().AsTask(), bus, cancellationToken).ConfigureAwait(false);
        var address = await CompleteAsync(bus.CallMethodAsync(CreateAddressQuery(bus), static (message, _) =>
            message.GetBodyReader().ReadVariantValue().GetString()), bus, cancellationToken).ConfigureAwait(false);
        // Discovery is local. Do not let a service redirect this input client to a TCP endpoint.
        if (!address.StartsWith("unix:", StringComparison.Ordinal) || address.Contains(';'))
            throw new InvalidOperationException("Maliit did not advertise a single local Unix endpoint.");
        var options = new MaliitPeerConnectionOptions(address);
        _peer = new DBusConnection(options);
        try
        {
            await CompleteAsync(_peer.ConnectAsync().AsTask(), _peer, cancellationToken).ConfigureAwait(false);
            _peer.AddMethodHandler(this);
            options.ReleaseCallbacks();
            IsConnected = true;
            _disconnectWatcher = WatchDisconnectAsync(_peer);
            await CallAsync("activateContext", cancellationToken).ConfigureAwait(false);
            await SendStateAsync(state, true, cancellationToken).ConfigureAwait(false);
            await ShowAsync(cancellationToken).ConfigureAwait(false);
        }
        catch { Dispose(); throw; }
    }

    private static MessageBuffer CreateAddressQuery(DBusConnection connection)
    {
        using var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(destination: "org.maliit.server", path: "/org/maliit/server/address",
            @interface: "org.freedesktop.DBus.Properties", member: "Get", signature: "ss");
        writer.WriteString("org.maliit.Server.Address"); writer.WriteString("address");
        return writer.CreateMessage();
    }

    public Task ShowAsync(CancellationToken cancellationToken) => CallAsync("showInputMethod", cancellationToken);
    public Task HideAsync(CancellationToken cancellationToken) => CallAsync("hideInputMethod", cancellationToken);
    public Task ResetAsync(CancellationToken cancellationToken) => CallAsync("reset", cancellationToken);
    public Task UpdateAsync(MaliitWidgetState state, CancellationToken cancellationToken) => SendStateAsync(state, false, cancellationToken);

    private async Task SendStateAsync(MaliitWidgetState state, bool focusChanged, CancellationToken token)
    {
        _state = state;
        await _send.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var peer = GetPeer();
            MessageBuffer message;
            using (var writer = peer.GetMessageWriter())
            {
                writer.WriteMethodCallHeader(path: ServerPath, @interface: ServerInterface, member: "updateWidgetInformation", signature: "a{sv}b");
                writer.WriteDictionary(state.ToProperties()); writer.WriteBool(focusChanged);
                message = writer.CreateMessage();
            }
            await CompleteAsync(peer.CallMethodAsync(message), peer, token).ConfigureAwait(false);
        }
        finally { _send.Release(); }
    }

    private async Task CallAsync(string method, CancellationToken token)
    {
        await _send.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var peer = GetPeer();
            MessageBuffer message;
            using (var writer = peer.GetMessageWriter())
            {
                writer.WriteMethodCallHeader(path: ServerPath, @interface: ServerInterface, member: method);
                message = writer.CreateMessage();
            }
            await CompleteAsync(peer.CallMethodAsync(message), peer, token).ConfigureAwait(false);
        }
        finally { _send.Release(); }
    }
    private DBusConnection GetPeer() => !_disposed && IsConnected && _peer is not null ? _peer : throw new InvalidOperationException("Maliit peer is disconnected.");

    private async Task CompleteAsync(Task task, DBusConnection connection, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _closed.Token);
        try { await task.WaitAsync(_timeout, linked.Token).ConfigureAwait(false); }
        catch
        {
            connection.Dispose();
            _ = task.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            throw;
        }
    }
    private async Task<T> CompleteAsync<T>(Task<T> task, DBusConnection connection, CancellationToken token)
    {
        await CompleteAsync((Task)task, connection, token).ConfigureAwait(false);
        return await task.ConfigureAwait(false);
    }
    private async Task WatchDisconnectAsync(DBusConnection peer)
    {
        try { await peer.DisconnectedAsync().ConfigureAwait(false); }
        catch { /* Disconnection itself is the diagnostic event. */ }
        IsConnected = false;
        if (!_disposed && DispatchAsync is { } dispatch)
        {
            try { await dispatch(new InputDisconnected()).ConfigureAwait(false); }
            catch { /* A disposed or failed UI consumer cannot handle disconnection. */ }
        }
    }

    public async ValueTask HandleMethodAsync(MethodContext context)
    {
        try
        {
            var input = ReadMethod(context);
            if (input is null) return;
            if (DispatchAsync is { } dispatch)
            {
                var state = await dispatch(input).ConfigureAwait(false);
                if (state is not null) _state = state;
                else if (input is InputSelectionQuery) _state = null;
            }
            using var reply = context.CreateReplyWriter(input is InputSelectionQuery ? "bs" : null);
            if (input is InputSelectionQuery)
            {
                reply.WriteBool(_state is { IsSensitive: false });
                reply.WriteString(_state?.SelectedText ?? "");
            }
            context.Reply(reply.CreateMessage());
        }
        catch (Exception error) { context.HandleException(error); }
    }

    private MaliitEvent? ReadMethod(MethodContext context)
    {
        if (_disposed) { context.ReplyError("org.freedesktop.DBus.Error.Disconnected", "Focus lease ended."); return default; }
        var message = context.Request;
        if (!message.Interface.SequenceEqual("com.meego.inputmethod.inputcontext1"u8))
        { context.ReplyUnknownMethodError(); return default; }
        var member = Encoding.UTF8.GetString(message.Member);
        var signature = Encoding.UTF8.GetString(message.Signature);
        try
        {
            var reader = message.GetBodyReader();
            MaliitEvent? input = null;
            switch ((member, signature))
            {
                case ("commitString", "siii"):
                    input = new CommitText(reader.ReadString(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()); break;
                case ("updatePreedit", "sa(iii)iii"):
                    var text = reader.ReadString();
                    var formats = reader.ReadArrayStart(DBusType.Struct);
                    var formatCount = 0;
                    while (reader.HasNext(formats))
                    { reader.AlignStruct(); _ = reader.ReadInt32(); _ = reader.ReadInt32(); _ = reader.ReadInt32(); formatCount++; }
                    input = new PreeditText(text, reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), formatCount); break;
                case ("keyEvent", "iiisbiy"):
                    input = new InputKey(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadString(), reader.ReadBool(), reader.ReadInt32(), reader.ReadByte()); break;
                case ("setSelection", "ii"): input = new InputSelection(reader.ReadInt32(), reader.ReadInt32()); break;
                case ("updateInputMethodArea", "iiii"):
                    input = new InputAreaChanged(new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32())); break;
                case ("imInitiatedHide", ""): input = new InputHidden(); break;
                case ("activationLostEvent", ""): input = new InputActivationLost(); break;
                // Refresh selection on the UI thread before replying, including sensitivity changes.
                case ("selection", ""): input = new InputSelectionQuery(); break;
                case ("preeditRectangle", ""):
                    using (var reply = context.CreateReplyWriter("biiii"))
                    { reply.WriteBool(false); for (var i = 0; i < 4; i++) reply.WriteInt32(0); context.Reply(reply.CreateMessage()); }
                    return default;
                // These settings do not change our direct editing path. Hardware-key redirection is not implemented.
                case ("setRedirectKeys", "b"):
                case ("setDetectableAutoRepeat", "b"):
                case ("setGlobalCorrectionEnabled", "b"):
                case ("setLanguage", "s"): break;
                default: context.ReplyUnknownMethodError(); return default;
            }
            if (input is null)
            {
                using var reply = context.CreateReplyWriter(null);
                context.Reply(reply.CreateMessage());
            }
            return input;
        }
        catch (Exception error) { context.HandleException(error); }
        return default;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; IsConnected = false;
        _closed.Cancel(); _peer?.Dispose();
        // WatchDisconnectAsync catches its errors and completes after Dispose closes the peer.
        _state = null;
        DispatchAsync = null;
    }
}
