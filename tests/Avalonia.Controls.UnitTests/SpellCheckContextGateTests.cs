using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class SpellCheckContextGateTests : ScopedTestBase
    {
        [Fact]
        public void Calls_Run_One_At_A_Time_In_Order()
        {
            using (Start())
            {
                var context = new ManualContext();
                var gate = new SpellCheckContextGate(context);

                var first = gate.CheckAsync("first".AsMemory(), TestContext.Current.CancellationToken).AsTask();
                var second = gate.CheckAsync("second".AsMemory(), TestContext.Current.CancellationToken).AsTask();
                var third = gate.CheckAsync("third".AsMemory(), TestContext.Current.CancellationToken).AsTask();
                RunJobs();

                Assert.Equal(new[] { "first" }, context.Texts);

                context.CompleteNext();
                RunJobs();
                Assert.True(first.IsCompletedSuccessfully);
                Assert.Equal(new[] { "first", "second" }, context.Texts);

                context.CompleteNext();
                RunJobs();
                Assert.Equal(new[] { "first", "second", "third" }, context.Texts);

                context.CompleteNext();
                RunJobs();
                Assert.True(third.IsCompletedSuccessfully);
            }
        }

        [Fact]
        public void Cancelled_Caller_Leaves_The_Queue_While_A_Call_Is_Stuck()
        {
            using (Start())
            {
                var context = new ManualContext();
                var gate = new SpellCheckContextGate(context);
                using var cancellation = new CancellationTokenSource();

                var stuck = gate.CheckAsync("stuck".AsMemory(), TestContext.Current.CancellationToken).AsTask();
                var waiting = gate.CheckAsync("waiting".AsMemory(), cancellation.Token).AsTask();

                cancellation.Cancel();
                RunJobs();

                Assert.True(waiting.IsCanceled);
                Assert.Equal(new[] { "stuck" }, context.Texts);

                // The gate keeps working once the stuck call finishes.
                context.CompleteNext();
                RunJobs();
                var next = gate.CheckAsync("next".AsMemory(), TestContext.Current.CancellationToken).AsTask();
                RunJobs();

                Assert.True(stuck.IsCompletedSuccessfully);
                Assert.Equal(new[] { "stuck", "next" }, context.Texts);
            }
        }

        [Fact]
        public void Caller_Cancelled_After_Its_Turn_Was_Granted_Passes_The_Turn_On()
        {
            using (Start())
            {
                var context = new ManualContext();
                var gate = new SpellCheckContextGate(context);
                using var cancellation = new CancellationTokenSource();

                var first = gate.CheckAsync("first".AsMemory(), TestContext.Current.CancellationToken).AsTask();
                var second = gate.CheckAsync("second".AsMemory(), cancellation.Token).AsTask();
                var third = gate.CheckAsync("third".AsMemory(), TestContext.Current.CancellationToken).AsTask();

                // Cancel right after the first call hands its turn to the second caller, before that caller runs.
                _ = first.ContinueWith(_ => cancellation.Cancel(), TaskContinuationOptions.ExecuteSynchronously);
                context.CompleteNext();
                RunJobs();

                Assert.True(second.IsCanceled);
                Assert.Equal(new[] { "first", "third" }, context.Texts);
            }
        }

        private static IDisposable Start()
        {
            var app = UnitTestApplication.Start(TestServices.MockThreadingInterface);
            SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
            return app;
        }

        private static void RunJobs() => Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        private sealed class ManualContext : ISpellCheckContext
        {
            private readonly Queue<TaskCompletionSource<IReadOnlyList<ISpellCheckResult>>> _pending = new();

            public List<string> Texts { get; } = new();

            public ValueTask<IReadOnlyList<ISpellCheckResult>> CheckAsync(
                ReadOnlyMemory<char> text,
                CancellationToken cancellationToken = default)
            {
                Texts.Add(text.ToString());
                var completion = new TaskCompletionSource<IReadOnlyList<ISpellCheckResult>>();
                _pending.Enqueue(completion);
                return new ValueTask<IReadOnlyList<ISpellCheckResult>>(completion.Task);
            }

            public void CompleteNext() => _pending.Dequeue().SetResult(Array.Empty<ISpellCheckResult>());

            public void Dispose()
            {
            }
        }
    }
}
