using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Wayland.Screens;
using Avalonia.Wayland.Server.Interop;
using Avalonia.Wayland.Server.Persistent;
using Avalonia.Wayland.Server.Transient;
using NWayland;
using NWayland.Interop;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland.Server;

partial class WaylandWorker
{
    private Queue<Action> _queue = new();
    private readonly WakeupFd _wakeupFd = new();
    private volatile bool _hasPendingServerJobs;
    private WaylandConnection? _connection;
    private WaylandGlobals? _globals;
    private WaylandOutputsSinkProxy? _outputsSink;
    public WaylandGlobals? Globals => _globals;
    public WaylandPlatformGraphics PlatformGraphics { get; } = new();
    private Thread? _thread;
    public IRawEventGrouperDispatchQueue InputDispatchQueue { get; }


    public Compositor Compositor { get; }
    public WaylandWorkerClient Client { get; }
    private HashSet<IPersistentWaylandObject> _persistentObjects = new();
    private HashSet<IPersistentWaylandObject> _connectedObjects = new();


    public WaylandWorker(IRawEventGrouperDispatchQueue inputDispatchQueue)
    {
        InputDispatchQueue = inputDispatchQueue;
        Compositor = new Compositor(_renderLoop, PlatformGraphics);
        Client = new WaylandWorkerClient(this);
        InitRenderTimer();
    }

    /// <summary>
    /// Posts a callback to the wayland thread alongside with the current compositor batch.
    /// The order of execution of such callbacks and callbacks submitted via different means is UNDEFINED.
    /// </summary>
    public void PostWithCommit(Action cb)
    {
        _hasPendingServerJobs = true;
        Compositor.PostServerJob(cb);
    }
    
    /// <summary>
    /// Posts a callback to the wayland thread queue directly, bypassing the compositor batch.
    /// Use for rare out-of-band messages that need immediate delivery.
    /// </summary>
    public void PostOob(Action cb)
    {
        lock (_queue)
            _queue.Enqueue(cb);
        _wakeupFd.Set();
    }


    /// <summary>
    /// Posts a callback to the wayland thread queue and returns a task that indicates its completion
    /// </summary>
    public Task<T> InvokeAsync<T>(Func<T> cb)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        PostOob(() =>
        {
            try
            {
                var result = cb();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    /// <summary>
    /// Posts a callback to the wayland thread queue and returns a task that completes when the callback finishes.
    /// Unlike <see cref="InvokeAsync{T}(Func{T})"/>, this does not return a value.
    /// </summary>
    public Task InvokeOobAsync(Action cb)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        PostOob(() =>
        {
            try
            {
                cb();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    void Run(WaylandPlatformOptions options, WaylandConnection connection)
    {
        var reconnectsDisabled = options.EnableReconnects == false || options.DisplayFd.HasValue;
        while (true)
        {
            RunConnection(connection, options);
            foreach (var obj in _persistentObjects.ToList())
                obj.OnDisconnected();
            Compositor.Server.ResetAllGpuResources();
            _globals?.Dispose();
            _globals = null;
            
            Logger.TryGet(LogEventLevel.Error, "Wayland")?.Log(null, "Wayland connection lost");
            if (reconnectsDisabled)
                throw new AvaloniaWaylandException("Connection lost");
            connection.Dispose();

            while (true)
            {
                var probe = Probe(options);
                if (probe == null)
                    Thread.Sleep(1000);
                else
                {
                    connection = probe;
                    break;
                }
            }

        }
    }
    
    void RunQueue()
    {
        while (true)
        {
            Action cb;
            lock (_queue)
            {
                if (!_queue.TryDequeue(out cb!))
                    return;
            }

            cb();
        }
    }
    
    void RunConnection(WaylandConnection connection, WaylandPlatformOptions options)
    {
        _thread = Thread.CurrentThread;
        _connection = connection;
        _globals = new WaylandGlobals(_connection, this, options, _outputsSink);

        foreach (var obj in _persistentObjects)
            ConnectPersistentObject(obj);
        
        WakeupRenderLoop();
        Compositor.Server.InvalidateAllCompositionTargets();
        
        while (true)
        {
            var res = connection.DispatchQueueOrWakeup(connection.Queue, _wakeupFd.PollFd);
            if (res == WaylandConnection.DispatchResult.ConnectionReset)
                return;
            if (res == WaylandConnection.DispatchResult.Wakeup) 
                _wakeupFd.Clear();
            RunQueue();
            TickRenderLoopIfNeeded();
        }
    }

    class Tracer : INWaylandTracer
    {
        string GetProxyString(WlProxy proxy)
        {
            return $"{proxy.GetType().Name}({proxy.Handle:x8}/{proxy.Id})";
        }
        
        string PrintArg(WlMessageDescription method, WlTracedArgument arg, int index)
        {
            var code = method.Arguments[index].Code;
            return code switch
            {
                WaylandArgumentCodes.Object when arg.Object is WlProxy proxy => GetProxyString(proxy),
                WaylandArgumentCodes.String => arg.Object?.ToString() ?? "null",
                WaylandArgumentCodes.Int32 or WaylandArgumentCodes.Fd => arg.Int32.ToString(),
                WaylandArgumentCodes.UInt32 or WaylandArgumentCodes.NewId => arg.UInt32.ToString(),
                WaylandArgumentCodes.Fixed => ((double)arg.Fixed).ToString(),
                _ => arg.Object?.ToString() ?? "null"
            };
        }

        public void Trace(WlProxy sender, bool isEvent, bool isDestructor, WlMessageDescription method, ReadOnlySpan<WlTracedArgument> args)
        {
            var argStrings = new string[args.Length];
            for (var i = 0; i < args.Length; i++)
                argStrings[i] = PrintArg(method, args[i], i);
            Console.Error.WriteLine(
                $"{GetProxyString(sender)}-{(isEvent ? '<' : '>')}{(isDestructor ? " (D)" : "")}-{method.Name}({string.Join(", ", argStrings)})");
        }

        public void TraceDestroy(WlProxy proxy, bool isFromFinalizer, bool nativeCallSkipped)
        {
            if (isFromFinalizer)
                Console.Error.WriteLine($"{GetProxyString(proxy)} IS FINALIZED!!!!!!");
        }
    }
    
    public static WaylandConnection? Probe(WaylandPlatformOptions options)
    {
        try
        {
            var conn = options.DisplayFd is { } fd
                ? new WaylandConnection(fd)
                : new WaylandConnection(options.WlDisplayName);
            if (options.EnableTracing)
                conn.Display.Tracer = new Tracer();
            return conn;
        }
        catch(Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, "Wayland")?.Log(null, "Failed to connect to Wayland display: " + e);
            return null;
        }
    }

    public void Start(WaylandPlatformOptions options, WaylandConnection probedConnection,
        WaylandOutputsSinkProxy? outputsSink = null)
    {
        _outputsSink = outputsSink;
        StartCore(options, probedConnection, null);
    }
    
    public void Start(WaylandPlatformOptions options, WlDisplay foreignDisplay,
        WaylandOutputsSinkProxy? outputsSink = null)
    {
        _outputsSink = outputsSink;
        StartCore(options, null, foreignDisplay);
    }
    
    void StartCore(WaylandPlatformOptions options, WaylandConnection? probedConnection, WlDisplay? foreignDisplay)
    {
        new Thread(() =>
        {
            if(probedConnection!=null)
                Run(options, probedConnection);
            else if (foreignDisplay != null)
            {
                RunConnection(new WaylandConnection(foreignDisplay), options);
                // This is fatal
                throw new AvaloniaWaylandException("Connection lost");
            }
            else
                throw new InvalidOperationException("Either probedConnection or foreignDisplay must be provided.");
        })
        {
            Name = "AvaloniaWayland",
            IsBackground = true
        }.Start();
    }

    void VerifyAccess()
    {
        if(_thread != Thread.CurrentThread)
            throw new AvaloniaWaylandException("Call from invalid thread");
        
    }

    void ConnectPersistentObject(IPersistentWaylandObject waylandObject)
    {
        if (_connection == null || _globals == null)
            throw new InvalidOperationException();
        waylandObject.OnConnected(_connection, _globals);
        _connectedObjects.Add(waylandObject);
    }
    
    public void RegisterPersistentObject(IPersistentWaylandObject waylandObject)
    {
        VerifyAccess();
        _persistentObjects.Add(waylandObject);
        if (_connection?.IsConnected == true)
            ConnectPersistentObject(waylandObject);

    }

    public void UnregisterPersistentObject(IPersistentWaylandObject waylandObject)
    {
        VerifyAccess();
        if (_connectedObjects.Remove(waylandObject))
            waylandObject.OnDisconnected();
        _persistentObjects.Remove(waylandObject);
    }
}