#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Platform.Interop;

namespace Avalonia.X11.Interop;

internal static unsafe class Glib
{
    private const string GlibName = "libglib-2.0.so.0";
    private const string GObjectName = "libgobject-2.0.so.0";

    [DllImport(GlibName)]
    public static extern void g_slist_free(GSList* data);

    [DllImport(GObjectName)]
    private static extern void g_object_ref(IntPtr instance);

    [DllImport(GObjectName)]
    private static extern ulong g_signal_connect_object(IntPtr instance, Utf8Buffer signal,
        IntPtr handler, IntPtr userData, int flags);

    [DllImport(GObjectName)]
    private static extern void g_object_unref(IntPtr instance);

    [DllImport(GObjectName)]
    private static extern ulong g_signal_handler_disconnect(IntPtr instance, ulong connectionId);

    public const int G_PRIORITY_HIGH = -100; 
    public const int G_PRIORITY_DEFAULT = 0; 
    public const int G_PRIORITY_HIGH_IDLE =  100; 
    public const int G_PRIORITY_DEFAULT_IDLE =  200; 
    
    [DllImport(GlibName)]
    public static extern IntPtr g_main_loop_new(IntPtr context, int is_running);
    
    [DllImport(GlibName)]
    public static extern void g_main_loop_quit(IntPtr loop);
    
    [DllImport(GlibName)]
    public static extern void g_main_loop_run(IntPtr loop);
    
    [DllImport(GlibName)]
    public static extern void g_main_loop_unref(IntPtr loop);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GSourceFunc(IntPtr userData);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void GDestroyNotify(IntPtr userData);

    [DllImport(GlibName)]
    private static extern int g_idle_add(GSourceFunc cb, IntPtr userData);

    private static readonly GSourceFunc s_onceSourceCb = (userData) =>
    {
        var h = GCHandle.FromIntPtr(userData);
        var cb = (Action)h.Target!;

        h.Free();
        cb();
        return 0;
    };

    public static void g_idle_add_once(Action cb) =>
        g_idle_add(s_onceSourceCb, GCHandle.ToIntPtr(GCHandle.Alloc(cb)));
    
    [DllImport(GlibName)]
    private static extern uint g_timeout_add(uint interval, GSourceFunc cb, IntPtr userData);
    
    public static uint g_timeout_add_once(uint interval, Action cb) =>
        g_timeout_add(interval, s_onceSourceCb, GCHandle.ToIntPtr(GCHandle.Alloc(cb)));

    private static readonly GDestroyNotify s_gcHandleDestroyNotify = handle => GCHandle.FromIntPtr(handle).Free();

    private static readonly GSourceFunc s_sourceFuncDispatchCallback =
        handle => ((Func<bool>)GCHandle.FromIntPtr(handle).Target!)() ? 1 : 0;
    
    [DllImport(GlibName)]
    private static extern uint g_idle_add_full (int priority, GSourceFunc function, IntPtr data, GDestroyNotify notify);

    public static uint g_idle_add_full(int priority, Func<bool> callback)
        => g_idle_add_full(priority, s_sourceFuncDispatchCallback, GCHandle.ToIntPtr(GCHandle.Alloc(callback)),
            s_gcHandleDestroyNotify);

    [DllImport(GlibName)]
    public static extern int g_source_get_can_recurse (IntPtr source);
    
    [DllImport(GlibName)]
    public static extern void g_source_set_can_recurse (IntPtr source, int can_recurse);
    
    [DllImport(GlibName)]
    public static extern IntPtr g_main_context_find_source_by_id (IntPtr context, uint source_id);
    
    [Flags]
    public enum GIOCondition
    {
        G_IO_IN = 1,
        G_IO_OUT = 4,
        G_IO_PRI = 2,
        G_IO_ERR = 8,
        G_IO_HUP = 16,
        G_IO_NVAL = 32
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GUnixFDSourceFunc(int fd, GIOCondition condition, IntPtr user_data);

    private static readonly GUnixFDSourceFunc s_unixFdSourceCallback = (fd, cond, handle) =>
        ((Func<int, GIOCondition, bool>)GCHandle.FromIntPtr(handle).Target!)(fd, cond) ? 1 : 0;
    
    [DllImport(GlibName)]
    public static extern uint g_unix_fd_add_full (int priority,
            int fd,
            GIOCondition condition,
            GUnixFDSourceFunc function,
            IntPtr user_data,
            GDestroyNotify notify);

    public static uint g_unix_fd_add_full(int priority, int fd, GIOCondition condition,
        Func<int, GIOCondition, bool> cb) =>
        g_unix_fd_add_full(priority, fd, condition, s_unixFdSourceCallback, GCHandle.ToIntPtr(GCHandle.Alloc(cb)),
            s_gcHandleDestroyNotify);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GSourcePrepareFunc(IntPtr source, out int timeout);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GSourceCheckFunc(IntPtr source);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GSourceDispatchFunc(IntPtr source, IntPtr callback, IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GSourceFinalizeFunc(IntPtr source);

    [StructLayout(LayoutKind.Sequential)]
    private struct GSourceFuncs
    {
        public IntPtr Prepare, Check, Dispatch, Finalize;
        // gmain.h includes two private slots after the public callbacks.
        public IntPtr ClosureCallback, ClosureMarshal;
    }

    private sealed class BufferedSourceState(int fd, Func<bool> isPending,
        Func<int, GIOCondition, bool> dispatch, Action<Exception> onError)
    {
        public int Fd { get; } = fd;
        public IntPtr UnixFdTag { get; set; }
        public Func<bool> IsPending { get; } = isPending;
        public Func<int, GIOCondition, bool> Dispatch { get; } = dispatch;
        public Action<Exception> OnError { get; } = onError;
        public bool Failed { get; set; }
    }

    private static readonly Dictionary<IntPtr, BufferedSourceState> s_bufferedSources = new();
    private static readonly GSourcePrepareFunc s_bufferedPrepare = BufferedSourcePrepare;
    private static readonly GSourceCheckFunc s_bufferedCheck = BufferedSourceCheck;
    private static readonly GSourceDispatchFunc s_bufferedDispatch = BufferedSourceDispatch;
    private static readonly GSourceFinalizeFunc s_bufferedFinalize = BufferedSourceFinalize;
    private static readonly IntPtr s_bufferedSourceFuncs = CreateBufferedSourceFuncs();

    [DllImport(GlibName)]
    private static extern IntPtr g_idle_source_new();

    [DllImport(GlibName)]
    private static extern void g_source_set_funcs(IntPtr source, IntPtr funcs);

    [DllImport(GlibName)]
    private static extern void g_source_set_priority(IntPtr source, int priority);

    [DllImport(GlibName)]
    private static extern IntPtr g_source_add_unix_fd(IntPtr source, int fd, GIOCondition condition);

    [DllImport(GlibName)]
    private static extern GIOCondition g_source_query_unix_fd(IntPtr source, IntPtr tag);

    [DllImport(GlibName)]
    private static extern uint g_source_attach(IntPtr source, IntPtr context);

    [DllImport(GlibName)]
    private static extern void g_source_unref(IntPtr source);

    public static uint AddBufferedUnixFdSource(int priority, int fd, Func<bool> isPending,
        Func<int, GIOCondition, bool> dispatch, Action<Exception> onError)
    {
        if (fd < 0) throw new ArgumentOutOfRangeException(nameof(fd));
        ArgumentNullException.ThrowIfNull(isPending);
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentNullException.ThrowIfNull(onError);
        // Let GLib allocate the opaque source. Replace its idle callbacks
        // before attaching it so the source uses fd and queue readiness.
        var source = g_idle_source_new();
        if (source == IntPtr.Zero) throw new OutOfMemoryException();
        try
        {
            g_source_set_funcs(source, s_bufferedSourceFuncs);
            g_source_set_priority(source, priority);
            g_source_set_can_recurse(source, 1);
            var state = new BufferedSourceState(fd, isPending, dispatch, onError)
            {
                UnixFdTag = g_source_add_unix_fd(source, fd, GIOCondition.G_IO_IN)
            };
            if (state.UnixFdTag == IntPtr.Zero)
                throw new InvalidOperationException("GLib refused the X11 descriptor source.");
            lock (s_bufferedSources) s_bufferedSources.Add(source, state);
            var id = g_source_attach(source, IntPtr.Zero);
            if (id == 0) throw new InvalidOperationException("GLib refused the X11 event source.");
            return id;
        }
        finally
        {
            // The context owns a reference after attach. On failure the final
            // unref calls finalize and releases the managed state.
            g_source_unref(source);
        }
    }

    private static IntPtr CreateBufferedSourceFuncs()
    {
        var funcs = new GSourceFuncs
        {
            Prepare = Marshal.GetFunctionPointerForDelegate(s_bufferedPrepare),
            Check = Marshal.GetFunctionPointerForDelegate(s_bufferedCheck),
            Dispatch = Marshal.GetFunctionPointerForDelegate(s_bufferedDispatch),
            Finalize = Marshal.GetFunctionPointerForDelegate(s_bufferedFinalize)
        };
        // GLib retains the table pointer. Keep the table and delegates
        // alive for the lifetime of the process.
        var address = Marshal.AllocHGlobal(Marshal.SizeOf<GSourceFuncs>());
        Marshal.StructureToPtr(funcs, address, false);
        return address;
    }

    private static BufferedSourceState? GetBufferedSource(IntPtr source)
    {
        lock (s_bufferedSources)
            return s_bufferedSources.TryGetValue(source, out var state) ? state : null;
    }

    private static int BufferedSourcePrepare(IntPtr source, out int timeout)
    {
        timeout = -1;
        var state = GetBufferedSource(source);
        if (state == null) return 0;
        try { return state.Failed || state.IsPending() ? 1 : 0; }
        catch (Exception error) { ReportBufferedSourceError(state, error); return 1; }
    }

    private static int BufferedSourceCheck(IntPtr source)
    {
        var state = GetBufferedSource(source);
        if (state == null) return 0;
        try
        {
            return state.Failed || state.IsPending() ||
                g_source_query_unix_fd(source, state.UnixFdTag) != 0 ? 1 : 0;
        }
        catch (Exception error) { ReportBufferedSourceError(state, error); return 1; }
    }

    private static int BufferedSourceDispatch(IntPtr source, IntPtr callback, IntPtr userData)
    {
        var state = GetBufferedSource(source);
        if (state == null || state.Failed) return 0;
        try
        {
            return state.Dispatch(state.Fd, g_source_query_unix_fd(source, state.UnixFdTag)) ? 1 : 0;
        }
        catch (Exception error) { ReportBufferedSourceError(state, error); return 0; }
    }

    private static void ReportBufferedSourceError(BufferedSourceState state, Exception error)
    {
        // Forward errors through the dispatcher so exceptions stay on the
        // managed side of the native callback.
        state.Failed = true;
        try { state.OnError(error); }
        catch { /* Keep reporting failures on the managed side as well. */ }
    }

    private static void BufferedSourceFinalize(IntPtr source)
    {
        lock (s_bufferedSources) s_bufferedSources.Remove(source);
    }
    
    [DllImport(GlibName)]
    public static extern int g_source_remove (uint tag);
    
    private class ConnectedSignal : IDisposable
    {
        private readonly IntPtr _instance;
        private GCHandle _handle;
        private readonly ulong _id;

        public ConnectedSignal(IntPtr instance, GCHandle handle, ulong id)
        {
            _instance = instance;
            g_object_ref(instance);
            _handle = handle;
            _id = id;
        }

        public void Dispose()
        {
            if (_handle.IsAllocated)
            {
                g_signal_handler_disconnect(_instance, _id);
                g_object_unref(_instance);
                _handle.Free();
            }
        }
    }

    public static IDisposable ConnectSignal<T>(IntPtr obj, string name, T handler)
        where T : notnull
    {
        var handle = GCHandle.Alloc(handler);
        var ptr = Marshal.GetFunctionPointerForDelegate(handler);
        using (var utf = new Utf8Buffer(name))
        {
            var id = g_signal_connect_object(obj, utf, ptr, IntPtr.Zero, 0);
            if (id == 0)
                throw new ArgumentException("Unable to connect to signal " + name);
            return new ConnectedSignal(obj, handle, id);
        }
    }

    public static Task<T> RunOnGlibThread<T>(Func<T> action)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        g_timeout_add_once(0, () =>
        {
            try
            {
                tcs.SetResult(action());
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        });
        return tcs.Task;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct GSList
{
    public readonly IntPtr Data;
    public readonly GSList* Next;
}
