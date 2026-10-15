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
    public class CompositeSpellCheckContextTests : ScopedTestBase
    {
        [Fact]
        public void Reports_Only_Errors_Found_In_Every_Language()
        {
            var english = new FixedContext(new TestSpellCheckResult(5, 5, "frase"), new TestSpellCheckResult(15, 4, "eror"));
            var spanish = new FixedContext(new TestSpellCheckResult(0, 4, "This"), new TestSpellCheckResult(15, 4, "eror"));
            using var composite = new CompositeSpellCheckContext(new ISpellCheckContext[] { english, spanish });

            var results = Run(composite.CheckAsync("This frase has eror".AsMemory(), TestContext.Current.CancellationToken));

            var result = Assert.Single(results);
            Assert.Equal(15, result.Start);
            Assert.Equal(4, result.Length);
        }

        [Fact]
        public void Overlapping_Ranges_Count_As_The_Same_Word()
        {
            var first = new FixedContext(new TestSpellCheckResult(0, 6, "dont's"));
            var second = new FixedContext(new TestSpellCheckResult(0, 4, "dont"));
            using var composite = new CompositeSpellCheckContext(new ISpellCheckContext[] { first, second });

            var results = Run(composite.CheckAsync("dont's".AsMemory(), TestContext.Current.CancellationToken));

            Assert.Equal(6, Assert.Single(results).Length);
        }

        [Fact]
        public void Stops_Checking_When_A_Language_Accepts_Everything()
        {
            var first = new FixedContext();
            var second = new FixedContext(new TestSpellCheckResult(0, 4, "eror"));
            using var composite = new CompositeSpellCheckContext(new ISpellCheckContext[] { first, second });

            var results = Run(composite.CheckAsync("eror".AsMemory(), TestContext.Current.CancellationToken));

            Assert.Empty(results);
            Assert.Equal(0, second.Checks);
        }

        [Fact]
        public void Suggestions_Are_Interleaved_Without_Duplicates()
        {
            var english = new FixedContext(new TestSpellCheckResult(0, 4, "eror", new[] { "error", "eros" }));
            var spanish = new FixedContext(new TestSpellCheckResult(0, 4, "eror", new[] { "error", "erosión", "erró" }));
            using var composite = new CompositeSpellCheckContext(new ISpellCheckContext[] { english, spanish });

            var result = Assert.Single(Run(composite.CheckAsync("eror".AsMemory(), TestContext.Current.CancellationToken)));
            var suggestions = Run(result.SuggestAsync(TestContext.Current.CancellationToken));

            Assert.Equal(new[] { "error", "eros", "erosión", "erró" }, suggestions);
        }

        [Fact]
        public void Lets_The_Dispatcher_Run_Between_Languages()
        {
            var first = new FixedContext(new TestSpellCheckResult(0, 4, "eror"));
            var second = new FixedContext(new TestSpellCheckResult(0, 4, "eror"));
            using var composite = new CompositeSpellCheckContext(new ISpellCheckContext[] { first, second });

            var check = composite.CheckAsync("eror".AsMemory(), TestContext.Current.CancellationToken).AsTask();

            Assert.Equal(1, first.Checks);
            Assert.Equal(0, second.Checks);
            Assert.False(check.IsCompleted);

            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.Equal(1, second.Checks);
            Assert.True(check.IsCompletedSuccessfully);
        }

        [Fact]
        public void Disposing_While_Yielding_Stops_The_Check()
        {
            var first = new FixedContext(new TestSpellCheckResult(0, 4, "eror"));
            var second = new FixedContext(new TestSpellCheckResult(0, 4, "eror"));
            var composite = new CompositeSpellCheckContext(new ISpellCheckContext[] { first, second });

            var check = composite.CheckAsync("eror".AsMemory(), TestContext.Current.CancellationToken).AsTask();
            composite.Dispose();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.IsType<ObjectDisposedException>(check.Exception?.InnerException);
            Assert.Equal(0, second.Checks);
        }

        [Fact]
        public void Disposing_Disposes_Every_Language()
        {
            var first = new FixedContext();
            var second = new FixedContext();

            new CompositeSpellCheckContext(new ISpellCheckContext[] { first, second }).Dispose();

            Assert.True(first.IsDisposed);
            Assert.True(second.IsDisposed);
        }

        private static T Run<T>(ValueTask<T> operation)
        {
            var task = operation.AsTask();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
            Assert.True(task.IsCompleted);
            return task.Result;
        }

        private sealed class FixedContext(params ISpellCheckResult[] results) : ISpellCheckContext
        {
            public int Checks { get; private set; }

            public bool IsDisposed { get; private set; }

            public ValueTask<IReadOnlyList<ISpellCheckResult>> CheckAsync(
                ReadOnlyMemory<char> text,
                CancellationToken cancellationToken = default)
            {
                Checks++;
                return new ValueTask<IReadOnlyList<ISpellCheckResult>>(results);
            }

            public void Dispose() => IsDisposed = true;
        }
    }
}
