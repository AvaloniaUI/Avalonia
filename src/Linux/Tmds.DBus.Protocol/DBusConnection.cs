using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks.Sources;

#pragma warning disable VSTHRD100 // Avoid "async void" methods

namespace Tmds.DBus.Protocol;

class DBusConnection : IDisposable
{
    private delegate void MessageReceivedHandler(Exception? exception, Message message, object? state);

    sealed class MyValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<T> _core;
        private volatile bool _continuationSet;

        public void SetResult(T result)
        {
            // Ensure we complete the Task from the read loop.
            SpinWait wait = new();
            while (!_continuationSet)
            {
                wait.SpinOnce();
            }
            _core.SetResult(result);
        }

        public void SetException(Exception exception) => _core.SetException(exception);

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _core.OnCompleted(continuation, state, token, flags);
            _continuationSet = true;
        }

        T IValueTaskSource<T>.GetResult(short token) => _core.GetResult(token);

        void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
    }

    enum ConnectionState
    {
        Created,
        Connecting,
        Connected,
        Disconnected
    }

    delegate void MessageHandlerDelegate(Exception? exception, Message message, object? state1, object? state2, object? state3);

    readonly struct MessageHandler
    {
        public MessageHandler(MessageHandlerDelegate handler, object? state1 = null, object? state2 = null, object? state3 = null)
        {
            _delegate = handler;
            _state1 = state1;
            _state2 = state2;
            _state3 = state3;
        }

        public void Invoke(Exception? exception, Message message)
        {
            _delegate(exception, message, _state1, _state2, _state3);
        }

        public bool HasValue => _delegate is not null;

        private readonly MessageHandlerDelegate _delegate;
        private readonly object? _state1;
        private readonly object? _state2;
        private readonly object? _state3;
    }

    delegate void MessageHandlerDelegate4(Exception? exception, Message message, object? state1, object? state2, object? state3, object? state4);

    readonly struct MessageHandler4
    {
        public MessageHandler4(MessageHandlerDelegate4 handler, object? state1 = null, object? state2 = null, object? state3 = null, object? state4 = null)
        {
            _delegate = handler;
            _state1 = state1;
            _state2 = state2;
            _state3 = state3;
            _state4 = state4;
        }

        public void Invoke(Exception? exception, Message message)
        {
            _delegate(exception, message, _state1, _state2, _state3, _state4);
        }

        public bool HasValue => _delegate is not null;

        private readonly MessageHandlerDelegate4 _delegate;
        private readonly object? _state1;
        private readonly object? _state2;
        private readonly object? _state3;
        private readonly object? _state4;
    }

    private readonly object _gate = new object();
    private readonly Connection _parentConnection;
    private readonly Dictionary<uint, MessageHandler> _pendingCalls;
    private readonly CancellationTokenSource _connectCts;
    private readonly Dictionary<string, MatchMaker> _matchMakers;
    private readonly List<Observer> _matchedObservers;
    private readonly PathNodeDictionary _pathNodes;
    private readonly string _machineId;

    private IMessageStream? _messageStream;
    private ConnectionState _state;
    private Exception? _disconnectReason;
    private string? _localName;
    private Message? _currentMessage;
    private Observer? _currentObserver;
    private SynchronizationContext? _currentSynchronizationContext;
    private TaskCompletionSource<Exception?>? _disconnectedTcs;
    private CancellationTokenSource _abortedCts;
    private bool _isMonitor;
    private Action<Exception?, DisposableMessage>? _monitorHandler;

    public string? UniqueName => _localName;

    public Exception DisconnectReason
    {
        get => _disconnectReason ?? new ObjectDisposedException(GetType().FullName);
        set => Interlocked.CompareExchange(ref _disconnectReason, value, null);
    }

    public bool RemoteIsBus => _localName is not null;

    public DBusConnection(Connection parent, string machineId)
    {
        _parentConnection = parent;
        _connectCts = new();
        _pendingCalls = new();
        _matchMakers = new();
        _matchedObservers = new();
        _pathNodes = new();
        _machineId = machineId;
        _abortedCts = new();
    }

    // For tests.
    internal void Connect(IMessageStream stream)
    {
        _messageStream = stream;

        stream.ReceiveMessages(
                    static (Exception? exception, Message message, DBusConnection connection) =>
                        connection.HandleMessages(exception, message), this);

        _state = ConnectionState.Connected;
    }

    public async ValueTask ConnectAsync(string address, string? userId, bool supportsFdPassing, CancellationToken cancellationToken)
    {
        _state = ConnectionState.Connecting;
        Exception? firstException = null;

        AddressParser.AddressEntry addr = default;
        while (AddressParser.TryGetNextEntry(address, ref addr))
        {
            Socket? socket = null;
            EndPoint? endpoint = null;
            Guid guid = default;

            if (AddressParser.IsType(addr, "unix"))
            {
                AddressParser.ParseUnixProperties(addr, out string path, out guid);
                socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                endpoint = new UnixDomainSocketEndPoint(path);
            }
            else if (AddressParser.IsType(addr, "tcp"))
            {
                AddressParser.ParseTcpProperties(addr, out string host, out int? port, out guid);
                if (!port.HasValue)
                {
                    throw new ArgumentException("port");
                }
                socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                endpoint = new DnsEndPoint(host, port.Value);
            }

            if (socket is null)
            {
                continue;
            }

            try
            {
                await socket.ConnectAsync(endpoint!, cancellationToken).ConfigureAwait(false);

                MessageStream stream;
                lock (_gate)
                {
                    if (_state != ConnectionState.Connecting)
                    {
                        throw new DisconnectedException(DisconnectReason);
                    }
                    _messageStream = stream = new MessageStream(socket);
                }

                await stream.DoClientAuthAsync(guid, userId, supportsFdPassing).ConfigureAwait(false);

                stream.ReceiveMessages(
                    static (Exception? exception, Message message, DBusConnection connection) =>
                        connection.HandleMessages(exception, message), this);

                lock (_gate)
                {
                    if (_state != ConnectionState.Connecting)
                    {
                        throw new DisconnectedException(DisconnectReason);
                    }
                    _state = ConnectionState.Connected;
                }

                _localName = await GetLocalNameAsync().ConfigureAwait(false);

                return;
            }
            catch (Exception exception)
            {
                socket.Dispose();
                firstException ??= exception;
            }
        }

        if (firstException is not null)
        {
            throw firstException;
        }

        throw new ArgumentException("No addresses were found", nameof(address));
    }

    private async Task<string?> GetLocalNameAsync()
    {
        MyValueTaskSource<string?> vts = new();

        await CallMethodAsync(
            message: CreateHelloMessage(),
            static (Exception? exception, Message message, object? state) =>
            {
                var vtsState = (MyValueTaskSource<string?>)state!;

                if (exception is not null)
                {
                    vtsState.SetException(exception);
                }
                else if (message.MessageType == MessageType.MethodReturn)
                {
                    vtsState.SetResult(message.GetBodyReader().ReadString().ToString());
                }
                else
                {
                    vtsState.SetResult(null);
                }
            }, vts).ConfigureAwait(false);

        return await new ValueTask<string?>(vts, token: 0).ConfigureAwait(false);

        MessageBuffer CreateHelloMessage()
        {
            using var writer = GetMessageWriter();

            writer.WriteMethodCallHeader(
                destination: "org.freedesktop.DBus",
                path: "/org/freedesktop/DBus",
                @interface: "org.freedesktop.DBus",
                member: "Hello");

            return writer.CreateMessage();
        }
    }

    private async void HandleMessages(Exception? exception, Message message)
    {
        if (exception is not null)
        {
            _parentConnection.Disconnect(exception, this);
        }
        else
        {
            try
            {
                bool returnMessageToPool = true;
                MessageHandler pendingCall = default;
                IEnumerable<IMethodHandler>? methodHandlers = null;
                Action<Exception?, DisposableMessage>? monitor = null;
                bool isMethodCall = message.MessageType == MessageType.MethodCall;
                MethodContext? methodContext = null;

                lock (_gate)
                {
                    if (_state == ConnectionState.Disconnected)
                    {
                        return;
                    }

                    monitor = _monitorHandler;

                    if (monitor is null)
                    {
                        if (message.ReplySerial.HasValue)
                        {
                            _pendingCalls.Remove(message.ReplySerial.Value, out pendingCall);
                        }

                        foreach (var matchMaker in _matchMakers.Values)
                        {
                            if (matchMaker.Matches(message))
                            {
                                _matchedObservers.AddRange(matchMaker.Observers);
                            }
                        }

                        if (isMethodCall)
                        {
                            methodContext = new MethodContext(_parentConnection, message, _abortedCts.Token); // TODO: pool.

                            if (message.PathIsSet)
                            {
                                if (_pathNodes.TryGetValue(message.PathAsString!, out PathNode? node))
                                {
                                    methodHandlers = node.MethodHandlers;

                                    bool isDBusIntrospect = message.Member.SequenceEqual("Introspect"u8) &&
                                                            message.Interface.SequenceEqual("org.freedesktop.DBus.Introspectable"u8);
                                    methodContext.IsDBusIntrospectRequest = isDBusIntrospect;
                                    if (isDBusIntrospect)
                                    {
                                        node.CopyChildNamesTo(methodContext);
                                    }
                                }
                            }
                        }
                    }
                }

                if (monitor is not null)
                {
                    lock (monitor)
                    {
                        if (_monitorHandler is not null)
                        {
                            returnMessageToPool = false;
                            monitor(null, new DisposableMessage(message));
                        }
                    }
                }
                else
                {
                    if (_matchedObservers.Count != 0)
                    {
                        foreach (var observer in _matchedObservers)
                        {
                            observer.Emit(message);
                        }
                        _matchedObservers.Clear();
                    }

                    if (pendingCall.HasValue)
                    {
                        pendingCall.Invoke(null, message);
                    }

                    if (isMethodCall)
                    {
                        Debug.Assert(methodContext is not null);
                        if (methodHandlers is not null)
                        {
// Suppress methodContext nullability warnings.
#if NETSTANDARD2_0
#pragma warning disable CS8604
#endif

                            foreach (var methodHandler in methodHandlers)
                            {
                                
                                bool runHandlerSynchronously = methodHandler.RunMethodHandlerSynchronously(message);
                                if (runHandlerSynchronously)
                                {
                                    await methodHandler.HandleMethodAsync(methodContext).ConfigureAwait(false);
                                    HandleNoReplySent(methodContext);
                                }
                                else
                                {
                                    returnMessageToPool = false;
                                    RunMethodHandler(methodHandler, methodContext);
                                }
                            }
                            
                        }
                        else
                        {
                            HandleNoReplySent(methodContext);
                        }
#if NETSTANDARD2_0
#pragma warning restore CS8604
#endif
                    }
                }

                if (returnMessageToPool)
                {
                    message.ReturnToPool();
                }
            }
            catch (Exception ex)
            {
                _parentConnection.Disconnect(ex, this);
            }
        }
    }

    private void HandleNoReplySent(MethodContext context)
    {
        if (context.ReplySent || context.NoReplyExpected)
        {
            return;
        }

        if (context.IsDBusIntrospectRequest)
        {
            context.ReplyIntrospectXml(interfaceXmls: []);
            return;
        }

        var request = context.Request;

        if (request.Interface.SequenceEqual("org.freedesktop.DBus.Peer"u8))
        {
            if (request.Member.SequenceEqual("Ping"u8))
            {
                using var writer = context.CreateReplyWriter(null);
                context.Reply(writer.CreateMessage());
                return;
            }
            else if (request.Member.SequenceEqual("GetMachineId"u8))
            {
                using var writer = context.CreateReplyWriter("s");
                writer.WriteString(_machineId);
                context.Reply(writer.CreateMessage());
                return;
            }
        }

        context.ReplyError("org.freedesktop.DBus.Error.UnknownMethod",
                           $"Method \"{request.MemberAsString}\" with signature \"{request.SignatureAsString}\" on interface \"{request.InterfaceAsString}\" doesn't exist");
    }

    private async void RunMethodHandler(IMethodHandler methodHandler, MethodContext context)
    {
        try
        {
            await methodHandler.HandleMethodAsync(context).ConfigureAwait(false);
            HandleNoReplySent(context);
            context.Request.ReturnToPool();
        }
        catch (Exception ex)
        {
            _parentConnection.Disconnect(ex, this);
        }
    }

    private void EmitOnSynchronizationContextHelper(Observer observer, SynchronizationContext synchronizationContext, Message message)
    {
        _currentMessage = message;
        _currentObserver = observer;
        _currentSynchronizationContext = synchronizationContext;

#pragma warning disable VSTHRD001 // Await JoinableTaskFactory.SwitchToMainThreadAsync() to switch to the UI thread instead of APIs that can deadlock or require specifying a priority.
        // note: Send blocks the current thread until the SynchronizationContext ran the delegate.
        synchronizationContext.Send(static o =>
        {
            SynchronizationContext? previousContext = SynchronizationContext.Current;
            try
            {
                DBusConnection conn = (DBusConnection)o!;
                SynchronizationContext.SetSynchronizationContext(conn._currentSynchronizationContext);
                conn._currentObserver!.InvokeHandler(conn._currentMessage!);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }, this);

        _currentMessage = null;
        _currentObserver = null;
        _currentSynchronizationContext = null;
    }

    public void UpdateMethodHandlers<T>(Action<IMethodHandlerDictionary, T> update, T state)
    {
        lock (_gate)
        {
            update(_pathNodes, state);
        }
    }

    public void Dispose()
    {
        Action<Exception?, DisposableMessage>? monitor = null;

        lock (_gate)
        {
            if (_state == ConnectionState.Disconnected)
            {
                return;
            }
            _state = ConnectionState.Disconnected;
            monitor = _monitorHandler;
        }

        Exception disconnectReason = DisconnectReason;

        _messageStream?.Close(disconnectReason);

        _abortedCts.Cancel();

        if (_pendingCalls is not null)
        {
            foreach (var pendingCall in _pendingCalls.Values)
            {
                pendingCall.Invoke(new DisconnectedException(disconnectReason), null!);
            }
            _pendingCalls.Clear();
        }

        foreach (var matchMaker in _matchMakers.Values)
        {
            foreach (var observer in matchMaker.Observers)
            {
                bool emitException = !object.ReferenceEquals(disconnectReason, Connection.DisposedException) ||
                                     observer.EmitOnConnectionDispose;
                Exception? exception = emitException ? new DisconnectedException(disconnectReason) : null;
                observer.Dispose(exception, removeObserver: false);
            }
        }
        _matchMakers.Clear();

        if (monitor is not null)
        {
            lock (monitor)
            {
                _monitorHandler = null;
                monitor(new DisconnectedException(disconnectReason), new DisposableMessage(null));
            }
        }

        _disconnectedTcs?.SetResult(GetWaitForDisconnectException());
    }

    private ValueTask CallMethodAsync(MessageBuffer message, MessageReceivedHandler returnHandler, object? state)
    {
        MessageHandlerDelegate fn = static (Exception? exception, Message message, object? state1, object? state2, object? state3) =>
        {
            ((MessageReceivedHandler)state1!)(exception, message, state2);
        };
        MessageHandler handler = new(fn, returnHandler, state);

        return CallMethodAsync(message, handler);
    }

    private async ValueTask CallMethodAsync(MessageBuffer message, MessageHandler handler)
    {
        bool messageSent = false;
        try
        {
            lock (_gate)
            {
                if (_state != ConnectionState.Connected)
                {
                    throw new DisconnectedException(DisconnectReason!);
                }
                if (_isMonitor)
                {
                    throw new InvalidOperationException("Cannot send messages on monitor connection.");
                }
                if ((message.MessageFlags & MessageFlags.NoReplyExpected) == 0)
                {
                    _pendingCalls.Add(message.Serial, handler);
                }
            }

            messageSent = await _messageStream!.TrySendMessageAsync(message).ConfigureAwait(false);
        }
        finally
        {
            if (!messageSent)
            {
                message.ReturnToPool();
            }
        }
    }

    public async Task<T> CallMethodAsync<T>(MessageBuffer message, MessageValueReader<T> valueReader, object? state = null)
    {
        MessageHandlerDelegate fn = static (Exception? exception, Message message, object? state1, object? state2, object? state3) =>
        {
            var valueReaderState = (MessageValueReader<T>)state1!;
            var vtsState = (MyValueTaskSource<T>)state2!;

            if (exception is not null)
            {
                vtsState.SetException(exception);
            }
            else if (message.MessageType == MessageType.MethodReturn)
            {
                try
                {
                    vtsState.SetResult(valueReaderState(message, state3));
                }
                catch (Exception ex)
                {
                    vtsState.SetException(ex);
                }
            }
            else if (message.MessageType == MessageType.Error)
            {
                vtsState.SetException(CreateDBusExceptionForErrorMessage(message));
            }
            else
            {
                vtsState.SetException(new ProtocolException($"Unexpected reply type: {message.MessageType}."));
            }
        };

        MyValueTaskSource<T> vts = new();
        MessageHandler handler = new(fn, valueReader, vts, state);

        await CallMethodAsync(message, handler).ConfigureAwait(false);

        return await new ValueTask<T>(vts, 0).ConfigureAwait(false);
    }

    public async Task CallMethodAsync(MessageBuffer message)
    {
        MyValueTaskSource<object?> vts = new();

        await CallMethodAsync(message,
            static (Exception? exception, Message message, object? state) => CompleteCallValueTaskSource(exception, message, state), vts).ConfigureAwait(false);

        await new ValueTask(vts, 0).ConfigureAwait(false);
    }

    private static void CompleteCallValueTaskSource(Exception? exception, Message message, object? vts)
    {
        var vtsState = (MyValueTaskSource<object?>)vts!;

        if (exception is not null)
        {
            vtsState.SetException(exception);
        }
        else if (message.MessageType == MessageType.MethodReturn)
        {
            vtsState.SetResult(null);
        }
        else if (message.MessageType == MessageType.Error)
        {
            vtsState.SetException(CreateDBusExceptionForErrorMessage(message));
        }
        else
        {
            vtsState.SetException(new ProtocolException($"Unexpected reply type: {message.MessageType}."));
        }
    }

    private static DBusException CreateDBusExceptionForErrorMessage(Message message)
    {
        string errorName = message.ErrorNameAsString ?? "<<No ErrorName>>.";
        string errMessage = errorName;
        if (message.SignatureIsSet && message.Signature.Length > 0 && (DBusType)message.Signature[0] == DBusType.String)
        {
            errMessage = message.GetBodyReader().ReadString();
        }
        return new DBusException(errorName, errMessage);
    }

    public async Task BecomeMonitorAsync(Action<Exception?, DisposableMessage> handler, IEnumerable<MatchRule>? rules)
    {
        Task reply;

        lock (_gate)
        {
            if (_state != ConnectionState.Connected)
            {
                throw new DisconnectedException(DisconnectReason!);
            }
            if (!RemoteIsBus)
            {
                throw new InvalidOperationException("The remote is not a bus.");
            }
            if (_matchMakers.Count != 0)
            {
                throw new InvalidOperationException("The connection has observers.");
            }
            if (_pendingCalls.Count != 0)
            {
                throw new InvalidOperationException("The connection has pending method calls.");
            }

            HashSet<string>? ruleStrings = null;
            if (rules is not null)
            {
                ruleStrings = new();
                foreach (var rule in rules)
                {
                    ruleStrings.Add(rule.ToString());
                }
            }

            reply = CallMethodAsync(CreateMessage(ruleStrings));
            _isMonitor = true;
        }

        try
        {
            await reply.ConfigureAwait(false);
            lock (_gate)
            {
                _messageStream!.BecomeMonitor();
                _monitorHandler = handler;
            }
        }
        catch
        {
            lock (_gate)
            {
                _isMonitor = false;
            }

            throw;
        }

        MessageBuffer CreateMessage(IEnumerable<string>? rules)
        {
            using var writer = GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Connection.DBusServiceName,
                path: Connection.DBusObjectPath,
                @interface: "org.freedesktop.DBus.Monitoring",
                signature: "asu",
                member: "BecomeMonitor");
            writer.WriteArray(rules ?? Array.Empty<string>());
            writer.WriteUInt32(0);
            return writer.CreateMessage();
        }
    }

    public ValueTask<IDisposable> AddMatchAsync<T>(SynchronizationContext? synchronizationContext, MatchRule rule, MessageValueReader<T> valueReader, Action<Exception?, T, object?, object?> valueHandler, object? readerState, object? handlerState, ObserverFlags flags)
    {
        MessageHandlerDelegate4 fn = static (Exception? exception, Message message, object? reader, object? handler, object? rs, object? hs) =>
        {
            var valueHandlerState = (Action<Exception?, T, object?, object?>)handler!;
            if (exception is not null)
            {
                valueHandlerState(exception, default(T)!, rs, hs);
            }
            else
            {
                var valueReaderState = (MessageValueReader<T>)reader!;
                T value = valueReaderState(message, rs);
                valueHandlerState(null, value, rs, hs);
            }
        };

        return AddMatchAsync(synchronizationContext, rule, new(fn, valueReader, valueHandler, readerState, handlerState), flags);
    }

    private async ValueTask<IDisposable> AddMatchAsync(SynchronizationContext? synchronizationContext, MatchRule rule, MessageHandler4 handler, ObserverFlags flags)
    {
        MatchRuleData data = rule.Data;
        MatchMaker? matchMaker;
        string ruleString;
        Observer observer;
        MessageBuffer? addMatchMessage = null;
        bool subscribe;

        lock (_gate)
        {
            if (_state != ConnectionState.Connected)
            {
                throw new DisconnectedException(DisconnectReason!);
            }
            if (!RemoteIsBus)
            {
                flags |= ObserverFlags.NoSubscribe;
            }
            if (_isMonitor)
            {
                throw new InvalidOperationException("Cannot add subscriptions on a monitor connection.");
            }

            ruleString = data.GetRuleString();

            if (!_matchMakers.TryGetValue(ruleString, out matchMaker))
            {
                matchMaker = new MatchMaker(this, ruleString, data);
                _matchMakers.Add(ruleString, matchMaker);
            }

            observer = new Observer(synchronizationContext, matchMaker, handler, flags);
            matchMaker.Observers.Add(observer);

            subscribe = observer.Subscribes;
            bool sendMessage = subscribe && matchMaker.AddMatchTcs is null;
            if (sendMessage)
            {
                addMatchMessage = CreateAddMatchMessage(matchMaker.RuleString);
                matchMaker.AddMatchTcs = new();

                MessageHandlerDelegate fn = static (Exception? exception, Message message, object? state1, object? state2, object? state3) =>
                {
                    var mm = (MatchMaker)state1!;
                    if (message.MessageType == MessageType.MethodReturn)
                    {
                        mm.HasSubscribed = true;
                    }
                    CompleteCallValueTaskSource(exception, message, mm.AddMatchTcs!);
                };

                _pendingCalls.Add(addMatchMessage.Serial, new(fn, matchMaker));
            }
        }

        if (subscribe)
        {
            if (addMatchMessage is not null)
            {
                if (!await _messageStream!.TrySendMessageAsync(addMatchMessage).ConfigureAwait(false))
                {
                    addMatchMessage.ReturnToPool();
                }
            }

            try
            {
                await matchMaker.AddMatchTask!.ConfigureAwait(false);
            }
            catch
            {
                observer.Dispose(exception: null);

                throw;
            }
        }

        return observer;

        MessageBuffer CreateAddMatchMessage(string ruleString)
        {
            using var writer = GetMessageWriter();

            writer.WriteMethodCallHeader(
                destination: "org.freedesktop.DBus",
                path: "/org/freedesktop/DBus",
                @interface: "org.freedesktop.DBus",
                member: "AddMatch",
                signature: "s");

            writer.WriteString(ruleString);

            return writer.CreateMessage();
        }
    }

    internal static readonly ObjectDisposedException ObserverDisposedException = new ObjectDisposedException(typeof(Observer).FullName);

    sealed class Observer : IDisposable
    {
        private readonly object _gate = new object();
        private readonly SynchronizationContext? _synchronizationContext;
        private readonly MatchMaker _matchMaker;
        private readonly MessageHandler4 _messageHandler;
        private readonly ObserverFlags _flags;
        private bool _disposed;

        public bool Subscribes => (_flags & ObserverFlags.NoSubscribe) == 0;
        public bool EmitOnConnectionDispose => (_flags & ObserverFlags.EmitOnConnectionDispose) != 0;
        public bool EmitOnObserverDispose => (_flags & ObserverFlags.EmitOnObserverDispose) != 0;

        public Observer(SynchronizationContext? synchronizationContext, MatchMaker matchMaker, in MessageHandler4 messageHandler, ObserverFlags flags)
        {
            _synchronizationContext = synchronizationContext;
            _matchMaker = matchMaker;
            _messageHandler = messageHandler;
            _flags = flags;
        }

        public void Dispose() =>
            Dispose(EmitOnObserverDispose ? ObserverDisposedException : null);

        public void Dispose(Exception? exception, bool removeObserver = true)
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }

            if (exception is not null)
            {
                Emit(exception);
            }

            if (removeObserver)
            {
                _matchMaker.Connection.RemoveObserver(_matchMaker, this);
            }
        }

        public void Emit(Message message)
        {
            if (_synchronizationContext is null)
            {
                InvokeHandler(message);
            }
            else
            {
                _matchMaker.Connection.EmitOnSynchronizationContextHelper(this, _synchronizationContext, message);
            }
        }

        private void Emit(Exception exception)
        {
            if (_synchronizationContext is null ||
                SynchronizationContext.Current == _synchronizationContext)
            {
                _messageHandler.Invoke(exception, null!);
            }
            else
            {
                _synchronizationContext.Send(
                    delegate
                    {
                        SynchronizationContext? previousContext = SynchronizationContext.Current;
                        try
                        {
                            SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
                            _messageHandler.Invoke(exception, null!);
                        }
                        finally
                        {
                            SynchronizationContext.SetSynchronizationContext(previousContext);
                        }
                    }, null);
            }
        }

        internal void InvokeHandler(Message message)
        {
            if (Subscribes && !_matchMaker.HasSubscribed)
            {
                return;
            }

            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _messageHandler.Invoke(null, message);
            }
        }
    }

    private async void RemoveObserver(MatchMaker matchMaker, Observer observer)
    {
        string ruleString = matchMaker.RuleString;
        bool sendMessage = false;

        lock (_gate)
        {
            if (_state == ConnectionState.Disconnected)
            {
                return;
            }

            if (_matchMakers.TryGetValue(ruleString, out _))
            {
                matchMaker.Observers.Remove(observer);
                sendMessage = matchMaker.AddMatchTcs is not null && matchMaker.HasSubscribers;
                if (sendMessage)
                {
                    _matchMakers.Remove(ruleString);
                }
            }
        }

        if (sendMessage)
        {
            var message = CreateRemoveMatchMessage();
            if (!await _messageStream!.TrySendMessageAsync(message).ConfigureAwait(false))
            {
                message.ReturnToPool();
            }
        }

        MessageBuffer CreateRemoveMatchMessage()
        {
            using var writer = GetMessageWriter();

            writer.WriteMethodCallHeader(
                destination: "org.freedesktop.DBus",
                path: "/org/freedesktop/DBus",
                @interface: "org.freedesktop.DBus",
                member: "RemoveMatch",
                signature: "s",
                flags: MessageFlags.NoReplyExpected);

            writer.WriteString(ruleString);

            return writer.CreateMessage();
        }
    }

    sealed class MatchMaker
    {
        private readonly MessageType? _type;
        private readonly byte[]? _sender;
        private readonly byte[]? _interface;
        private readonly byte[]? _member;
        private readonly byte[]? _path;
        private readonly byte[]? _pathNamespace;
        private readonly byte[]? _destination;
        private readonly byte[]? _arg0;
        private readonly byte[]? _arg0Path;
        private readonly byte[]? _arg0Namespace;
        private readonly string _rule;

        private MyValueTaskSource<object?>? _vts;

        public List<Observer> Observers { get; } = new();

        public MyValueTaskSource<object?>? AddMatchTcs
        {
            get => _vts;
            set
            {
                _vts = value;
                if (value != null)
                {
                    AddMatchTask = new ValueTask<object?>(value, token: 0).AsTask();
                }
            }
        }

        public Task<object?>? AddMatchTask { get; private set; }

        public bool HasSubscribed { get; set; }

        public DBusConnection Connection { get; }

        public MatchMaker(DBusConnection connection, string rule, in MatchRuleData data)
        {
            Connection = connection;
            _rule = rule;

            _type = data.MessageType;

            if (data.Sender is not null && data.Sender.StartsWith(":"))
            {
                _sender = Encoding.UTF8.GetBytes(data.Sender);
            }
            if (data.Interface is not null)
            {
                _interface = Encoding.UTF8.GetBytes(data.Interface);
            }
            if (data.Member is not null)
            {
                _member = Encoding.UTF8.GetBytes(data.Member);
            }
            if (data.Path is not null)
            {
                _path = Encoding.UTF8.GetBytes(data.Path);
            }
            if (data.PathNamespace is not null)
            {
                _pathNamespace = Encoding.UTF8.GetBytes(data.PathNamespace);
            }
            if (data.Destination is not null)
            {
                _destination = Encoding.UTF8.GetBytes(data.Destination);
            }
            if (data.Arg0 is not null)
            {
                _arg0 = Encoding.UTF8.GetBytes(data.Arg0);
            }
            if (data.Arg0Path is not null)
            {
                _arg0Path = Encoding.UTF8.GetBytes(data.Arg0Path);
            }
            if (data.Arg0Namespace is not null)
            {
                _arg0Namespace = Encoding.UTF8.GetBytes(data.Arg0Namespace);
            }
        }

        public string RuleString => _rule;

        public bool HasSubscribers
        {
            get
            {
                if (Observers.Count == 0)
                {
                    return false;
                }
                foreach (var observer in Observers)
                {
                    if (observer.Subscribes)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override string ToString() => _rule;

        internal bool Matches(Message message)
        {
            if (_type.HasValue && _type != message.MessageType)
            {
                return false;
            }

            if (_sender is not null && !IsEqual(_sender, message.Sender))
            {
                return false;
            }

            if (_interface is not null && !IsEqual(_interface, message.Interface))
            {
                return false;
            }

            if (_member is not null && !IsEqual(_member, message.Member))
            {
                return false;
            }

            if (_path is not null && !IsEqual(_path, message.Path))
            {
                return false;
            }

            if (_destination is not null && !IsEqual(_destination, message.Destination))
            {
                return false;
            }

            if (_pathNamespace is not null && (!message.PathIsSet || !IsEqualOrChildOfPath(message.Path, _pathNamespace)))
            {
                return false;
            }

            if (_arg0Namespace is not null ||
                _arg0 is not null ||
                _arg0Path is not null)
            {
                if (message.Signature.Length == 0)
                {
                    return false;
                }

                DBusType arg0Type = (DBusType)message.Signature![0];

                if (arg0Type != DBusType.String &&
                    arg0Type != DBusType.ObjectPath)
                {
                    return false;
                }

                ReadOnlySpan<byte> arg0 = message.GetBodyReader().ReadStringAsSpan();

                if (_arg0Path is not null && !IsEqualParentOrChildOfPath(arg0, _arg0Path))
                {
                    return false;
                }

                if (arg0Type != DBusType.String)
                {
                    return false;
                }

                if (_arg0 is not null && !IsEqual(_arg0, arg0))
                {
                    return false;
                }

                if (_arg0Namespace is not null && !IsEqualOrChildOfName(arg0, _arg0Namespace))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsEqualOrChildOfName(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
        {
            return lhs.StartsWith(rhs) && (lhs.Length == rhs.Length || lhs[rhs.Length] == '.');
        }

        private static bool IsEqualOrChildOfPath(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
        {
            return lhs.StartsWith(rhs) && (lhs.Length == rhs.Length || lhs[rhs.Length] == '/');
        }

        private static bool IsEqualParentOrChildOfPath(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
        {
            if (rhs.Length < lhs.Length)
            {
                return rhs[rhs.Length - 1] == '/' && lhs.StartsWith(rhs);
            }
            else if (lhs.Length < rhs.Length)
            {
                return lhs[lhs.Length - 1] == '/' && rhs.StartsWith(lhs);
            }
            else
            {
                return IsEqual(lhs, rhs);
            }
        }

        private static bool IsEqual(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
        {
            return lhs.SequenceEqual(rhs);
        }
    }

    public MessageWriter GetMessageWriter() => _parentConnection.GetMessageWriter();

    public async void SendMessage(MessageBuffer message)
    {
        bool messageSent = await _messageStream!.TrySendMessageAsync(message).ConfigureAwait(false);
        if (!messageSent)
        {
            message.ReturnToPool();
        }
    }

    public Task<Exception?> DisconnectedAsync()
    {
        lock (_gate)
        {
            if (_disconnectedTcs is null)
            {
                if (_state == ConnectionState.Disconnected)
                {
                    return Task.FromResult(GetWaitForDisconnectException());
                }
                else
                {
                    _disconnectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }
            return _disconnectedTcs.Task;
        }
    }

    private Exception? GetWaitForDisconnectException()
        => _disconnectReason is ObjectDisposedException ? null : _disconnectReason;

    private void SendErrorReplyMessage(Message methodCall, string errorName, string errorMsg)
    {
        SendMessage(CreateErrorMessage(methodCall, errorName, errorMsg));

        MessageBuffer CreateErrorMessage(Message methodCall, string errorName, string errorMsg)
        {
            using var writer = GetMessageWriter();

            writer.WriteError(
                replySerial: methodCall.Serial,
                destination: methodCall.Sender,
                errorName: errorName,
                errorMsg: errorMsg);

            return writer.CreateMessage();
        }
    }
}
