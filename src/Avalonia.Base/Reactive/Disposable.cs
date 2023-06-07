using System;
using System.Threading;

namespace Avalonia.Reactive;

/// <summary>
/// Provides a set of static methods for creating <see cref="IDisposable"/> objects.
/// </summary>
internal static class Disposable
{
    /// <summary>
    /// Represents a disposable that does nothing on disposal.
    /// </summary>
    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        private EmptyDisposable()
        {
        }

        public void Dispose()
        {
            // no op
        }
    }
    
    internal sealed class AnonymousDisposable : IDisposable
    {
        private volatile Action? _dispose;
        public AnonymousDisposable(Action dispose)
        {
            _dispose = dispose;
        }
        public bool IsDisposed => _dispose == null;
        public void Dispose()
        {
            Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }

    internal sealed class AnonymousDisposable<TState> : IDisposable
    {
        private TState _state;
        private volatile Action<TState>? _dispose;

        public AnonymousDisposable(TState state, Action<TState> dispose)
        {
            _state = state;
            _dispose = dispose;
        }

        public bool IsDisposed => _dispose == null;
        public void Dispose()
        {
            Interlocked.Exchange(ref _dispose, null)?.Invoke(_state);
            _state = default!;
        }
    }

    /// <summary>
    /// Gets the disposable that does nothing when disposed.
    /// </summary>
    public static IDisposable Empty => EmptyDisposable.Instance;

    /// <summary>
    /// Creates a disposable object that invokes the specified action when disposed.
    /// </summary>
    /// <param name="dispose">Action to run during the first call to <see cref="IDisposable.Dispose"/>. The action is guaranteed to be run at most once.</param>
    /// <returns>The disposable object that runs the given action upon disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispose"/> is <c>null</c>.</exception>
    public static IDisposable Create(Action dispose)
    {
        if (dispose == null)
        {
            throw new ArgumentNullException(nameof(dispose));
        }

        return new AnonymousDisposable(dispose);
    }

    /// <summary>
    /// Creates a disposable object that invokes the specified action when disposed.
    /// </summary>
    /// <param name="state">The state to be passed to the action.</param>
    /// <param name="dispose">Action to run during the first call to <see cref="IDisposable.Dispose"/>. The action is guaranteed to be run at most once.</param>
    /// <returns>The disposable object that runs the given action upon disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispose"/> is <c>null</c>.</exception>
    public static IDisposable Create<TState>(TState state, Action<TState> dispose)
    {
        if (dispose == null)
        {
            throw new ArgumentNullException(nameof(dispose));
        }

        return new AnonymousDisposable<TState>(state, dispose);
    }
}
