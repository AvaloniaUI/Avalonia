using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Logging;

namespace Avalonia.Input.TextInput;

// Lifetime and threading rules shared by the built in backends.
internal abstract class SpellCheckContextBase : ISpellCheckContext
{
    private readonly int _ownerThreadId = Environment.CurrentManagedThreadId;
    private readonly SynchronizationContext? _owner = SynchronizationContext.Current;
    private readonly CancellationTokenSource _lifetime = new();
    private int _disposed;

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    public ValueTask<IReadOnlyList<ISpellCheckResult>> CheckAsync(
        ReadOnlyMemory<char> text,
        CancellationToken cancellationToken = default)
    {
        VerifyAccess();
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        return RunAsync(token => CheckCoreAsync(text, token), cancellationToken);
    }

    protected abstract ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
        ReadOnlyMemory<char> text,
        CancellationToken cancellationToken);

    internal ValueTask<IReadOnlyList<string>> SuggestAsync(
        Func<CancellationToken, ValueTask<IReadOnlyList<string>>> suggest,
        CancellationToken cancellationToken)
    {
        VerifyAccess();
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        return RunAsync(suggest, cancellationToken);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (Environment.CurrentManagedThreadId == _ownerThreadId || _owner is null)
        {
            ReleaseResources();
        }
        else
        {
            // Native checkers are bound to their creating thread.
            _owner.Post(static state => ((SpellCheckContextBase)state!).ReleaseResources(), this);
        }
    }

    protected virtual void DisposeCore()
    {
    }

    protected void VerifyAccess()
    {
        if (Environment.CurrentManagedThreadId != _ownerThreadId)
        {
            throw new InvalidOperationException("A spell check context must be used from the thread that created it.");
        }
    }

    protected void ThrowIfDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    private async ValueTask<T> RunAsync<T>(
        Func<CancellationToken, ValueTask<T>> operation,
        CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        var result = await operation(linked.Token);
        linked.Token.ThrowIfCancellationRequested();
        return result;
    }

    private void ReleaseResources()
    {
        try
        {
            _lifetime.Cancel();
        }
        catch (Exception ex)
        {
            LogDisposeFailure(ex);
        }

        try
        {
            DisposeCore();
        }
        catch (Exception ex)
        {
            LogDisposeFailure(ex);
        }
    }

    private void LogDisposeFailure(Exception ex)
    {
        Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
            this, "Spell check context cleanup failed: {Error}", ex);
    }
}

// Suggestions of built in results stop working once their context is disposed.
internal abstract class SpellCheckResultBase : ISpellCheckResult
{
    private readonly SpellCheckContextBase _context;

    protected SpellCheckResultBase(SpellCheckContextBase context, int start, int length)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        _context = context;
        Start = start;
        Length = length;
    }

    public int Start { get; }

    public int Length { get; }

    public virtual string? Description => null;

    public ValueTask<IReadOnlyList<string>> SuggestAsync(CancellationToken cancellationToken = default) =>
        _context.SuggestAsync(SuggestCoreAsync, cancellationToken);

    protected abstract ValueTask<IReadOnlyList<string>> SuggestCoreAsync(CancellationToken cancellationToken);
}
