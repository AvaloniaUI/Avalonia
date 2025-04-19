using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Controls.Platform;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Threading;

public partial class Dispatcher
{
    [ThreadStatic]
    private static DispatcherReferenceStorage? s_currentThreadDispatcher;
    private static readonly object s_globalLock = new();
    private static readonly ConditionalWeakTable<Thread, DispatcherReferenceStorage> s_dispatchers = new();
    
    #if !NET6_0_OR_GREATER
    // netstandard2.0 doesn't have ConditionalWeakTable enumeration support
    private static readonly WeakHashList<DispatcherReferenceStorage> s_resetForTestsList = new();
    #endif
    
    private static Dispatcher? s_uiThread;

    // This class is needed PURELY for ResetForUnitTests, so we can reset s_currentThreadDispatcher for all threads
    class DispatcherReferenceStorage
    {
        public WeakReference<Dispatcher> Reference = new(null!);
    }
    
    public static Dispatcher CurrentDispatcher
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (s_currentThreadDispatcher?.Reference.TryGetTarget(out var dispatcher) == true)
                return dispatcher;

            return new Dispatcher(null);
        }
    }

    public static Dispatcher? FromThread(Thread thread)
    {
        lock (s_globalLock)
        {
            if (s_dispatchers.TryGetValue(thread, out var reference) && reference.Reference.TryGetTarget(out var dispatcher) == true)
                return dispatcher;
            return null;
        }
    }
    
    public static Dispatcher UIThread
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            static Dispatcher GetUIThreadDispatcherSlow()
            {
                lock (s_globalLock)
                {
                    return s_uiThread ?? CurrentDispatcher;
                }
            }
            return s_uiThread ?? GetUIThreadDispatcherSlow();
        }
    }

    internal static Dispatcher? TryGetUIThread()
    {
        lock (s_globalLock)
            return s_uiThread;
    }
    
    [PrivateApi]
    public static void InitializeUIThreadDispatcher(IPlatformThreadingInterface impl) =>
        InitializeUIThreadDispatcher(new LegacyDispatcherImpl(impl));

    [PrivateApi]
    public static void InitializeUIThreadDispatcher(IDispatcherImpl impl)
    {
        UIThread.VerifyAccess();
        if (UIThread._initialized)
            throw new InvalidOperationException("UI thread dispatcher is already initialized");
        UIThread.ReplaceImplementation(impl);
    }

    internal void SetCurrentDispatcherForUnitTests(Dispatcher dispatcher)
    {
        if (FromThread(Thread.CurrentThread) != null || TryGetUIThread() != null)
            throw new InvalidOperationException();
    }

    private static void ResetGlobalState()
    {
        lock (s_globalLock)
        {
#if NET6_0_OR_GREATER
            foreach (var store in s_dispatchers)
                store.Value.Reference = new(null!);
            s_dispatchers.Clear();
#else
            var alive = s_resetForTestsList.GetAlive();
            if(alive != null)
                foreach (var store in alive)
                    store.Reference = new(null!);
#endif

            s_currentThreadDispatcher = null;
            s_uiThread = null;
        }
    }
}