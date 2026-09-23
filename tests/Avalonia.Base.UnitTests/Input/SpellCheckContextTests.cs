using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Xunit;

namespace Avalonia.Base.UnitTests.Input;

public class SpellCheckContextTests
{
    [Fact]
    public void Disposing_Cancels_Pending_Operations_And_Rejects_New_Ones()
    {
        var context = new FakeContext();
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var check = context.CheckAsync("text".AsMemory(), cancellationToken).AsTask();
        var result = new TestResult(context);
        var suggestions = result.SuggestAsync(cancellationToken).AsTask();

        context.Dispose();

        Assert.True(context.CheckToken.IsCancellationRequested);
        Assert.True(context.SuggestionToken.IsCancellationRequested);

        context.CheckCompletion.SetResult(Array.Empty<ISpellCheckResult>());
        context.SuggestionCompletion.SetResult(Array.Empty<string>());

        Assert.True(check.IsCanceled);
        Assert.True(suggestions.IsCanceled);
        Assert.Throws<ObjectDisposedException>(() => { _ = context.CheckAsync("again".AsMemory(), cancellationToken); });
        Assert.Throws<ObjectDisposedException>(() => { _ = result.SuggestAsync(cancellationToken); });
    }

    [Fact]
    public void Checks_Do_Not_Cancel_Each_Other()
    {
        var context = new FakeContext();
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        var first = context.CheckAsync("first".AsMemory(), cancellationToken).AsTask();
        var firstToken = context.CheckToken;
        context.CheckCompletion.SetResult(Array.Empty<ISpellCheckResult>());

        Assert.False(firstToken.IsCancellationRequested);
        Assert.True(first.IsCompletedSuccessfully);
    }

    [Fact]
    public void Caller_Cancellation_Throws_Instead_Of_Returning_Partial_Results()
    {
        var context = new FakeContext();
        using var cancellation = new CancellationTokenSource();
        var check = context.CheckAsync("text".AsMemory(), cancellation.Token).AsTask();

        cancellation.Cancel();
        context.CheckCompletion.SetResult(new ISpellCheckResult[] { new TestResult(context) });

        Assert.True(check.IsCanceled);
    }

    [Fact]
    public void Context_Is_Affine_To_Its_Creating_Thread()
    {
        using var context = new FakeContext();
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        Exception? exception = null;
        var thread = new Thread(() =>
            exception = Record.Exception(() => { _ = context.CheckAsync("text".AsMemory(), cancellationToken); }));

        thread.Start();
        thread.Join();

        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Dispose_From_Another_Thread_Does_Not_Throw_And_Posts_Cleanup_To_The_Owner()
    {
        var owner = new RecordingSynchronizationContext();
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(owner);
        FakeContext context;

        try
        {
            context = new FakeContext();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Exception? exception = null;
        var thread = new Thread(() => exception = Record.Exception(context.Dispose));
        thread.Start();
        thread.Join();

        Assert.Null(exception);
        Assert.True(context.IsDisposed);
        Assert.Equal(0, context.DisposeCoreCount);

        owner.RunPosted();

        Assert.Equal(1, context.DisposeCoreCount);
    }

    [Fact]
    public void Dispose_Is_Idempotent_And_Swallows_Cleanup_Failures()
    {
        var context = new FakeContext { ThrowOnDispose = true };

        var exception = Record.Exception(() =>
        {
            context.Dispose();
            context.Dispose();
        });

        Assert.Null(exception);
        Assert.Equal(1, context.DisposeCoreCount);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 0)]
    public void Result_Rejects_Invalid_Ranges(int start, int length)
    {
        using var context = new FakeContext();

        Assert.Throws<ArgumentOutOfRangeException>(() => new TestResult(context, start, length));
    }

    private sealed class FakeContext : SpellCheckContextBase
    {
        public TaskCompletionSource<IReadOnlyList<ISpellCheckResult>> CheckCompletion { get; private set; } = new();

        public TaskCompletionSource<IReadOnlyList<string>> SuggestionCompletion { get; } = new();

        public CancellationToken CheckToken { get; private set; }

        public CancellationToken SuggestionToken { get; private set; }

        public int DisposeCoreCount { get; private set; }

        public bool ThrowOnDispose { get; init; }

        protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
            ReadOnlyMemory<char> text,
            CancellationToken cancellationToken)
        {
            CheckToken = cancellationToken;
            CheckCompletion = new TaskCompletionSource<IReadOnlyList<ISpellCheckResult>>();
            return new ValueTask<IReadOnlyList<ISpellCheckResult>>(CheckCompletion.Task);
        }

        public ValueTask<IReadOnlyList<string>> Suggest(CancellationToken cancellationToken)
        {
            SuggestionToken = cancellationToken;
            return new ValueTask<IReadOnlyList<string>>(SuggestionCompletion.Task);
        }

        protected override void DisposeCore()
        {
            DisposeCoreCount++;

            if (ThrowOnDispose)
            {
                throw new InvalidOperationException("Cleanup failed.");
            }
        }
    }

    private sealed class TestResult(FakeContext context, int start = 0, int length = 1)
        : SpellCheckResultBase(context, start, length)
    {
        protected override ValueTask<IReadOnlyList<string>> SuggestCoreAsync(CancellationToken cancellationToken) =>
            context.Suggest(cancellationToken);
    }

    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        private readonly List<(SendOrPostCallback Callback, object? State)> _posted = new();

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (_posted)
            {
                _posted.Add((d, state));
            }
        }

        public void RunPosted()
        {
            foreach (var (callback, state) in _posted)
            {
                callback(state);
            }

            _posted.Clear();
        }
    }
}
