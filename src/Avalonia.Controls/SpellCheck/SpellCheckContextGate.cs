using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Avalonia.Threading;

namespace Avalonia.Controls;

// Runs context calls one at a time so a suggestion query never cancels a background check.
// Queued callers resume through the dispatcher to stay on the UI thread.
internal sealed class SpellCheckContextGate : IDisposable
{
    private readonly Queue<TaskCompletionSource<bool>> _waiters = new();
    private bool _busy;

    public SpellCheckContextGate(ISpellCheckContext context)
    {
        Context = context;
    }

    public ISpellCheckContext Context { get; }

    public async ValueTask<IReadOnlyList<ISpellCheckResult>> CheckAsync(
        ReadOnlyMemory<char> text,
        CancellationToken cancellationToken)
    {
        await EnterAsync(cancellationToken);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Context.CheckAsync(text, cancellationToken);
        }
        finally
        {
            Exit();
        }
    }

    public async ValueTask<IReadOnlyList<string>> SuggestAsync(
        ISpellCheckResult result,
        CancellationToken cancellationToken)
    {
        await EnterAsync(cancellationToken);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await result.SuggestAsync(cancellationToken);
        }
        finally
        {
            Exit();
        }
    }

    public void Dispose() => Context.Dispose();

    // A cancelled caller leaves the queue, so a stuck provider call cannot pile up waiting work.
    private async ValueTask EnterAsync(CancellationToken cancellationToken)
    {
        if (!_busy)
        {
            _busy = true;
            return;
        }

        var waiter = new TaskCompletionSource<bool>();
        _waiters.Enqueue(waiter);

        using (cancellationToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetCanceled(), waiter))
        {
            await waiter.Task;
        }
    }

    private void Exit()
    {
        while (_waiters.Count > 0)
        {
            var next = _waiters.Dequeue();

            if (next.Task.IsCompleted)
            {
                continue;
            }

            // Hand the turn to the next caller, or pass it on if that caller was cancelled meanwhile.
            Dispatcher.UIThread.Post(() =>
            {
                if (!next.TrySetResult(true))
                {
                    Exit();
                }
            });
            return;
        }

        _busy = false;
    }
}
