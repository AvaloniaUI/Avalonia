using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Threading
{
    [MemoryDiagnoser]
    public class DispatcherBenchmarks
    {
        private IDisposable? _app;
        private Dispatcher? _dispatcher;
        private int _counter;

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _dispatcher = Dispatcher.UIThread;
            _counter = 0;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark posting an action to dispatcher
        /// </summary>
        [Benchmark]
        public void Post_Action()
        {
            _dispatcher!.Post(() => _counter++);
            // Process the posted action
            _dispatcher.RunJobs();
        }

        /// <summary>
        /// Benchmark posting with different priorities
        /// </summary>
        [Benchmark]
        public void Post_WithPriority_Normal()
        {
            _dispatcher!.Post(() => _counter++, DispatcherPriority.Normal);
            _dispatcher.RunJobs();
        }

        [Benchmark]
        public void Post_WithPriority_Background()
        {
            _dispatcher!.Post(() => _counter++, DispatcherPriority.Background);
            _dispatcher.RunJobs();
        }

        [Benchmark]
        public void Post_WithPriority_Render()
        {
            _dispatcher!.Post(() => _counter++, DispatcherPriority.Render);
            _dispatcher.RunJobs();
        }

        /// <summary>
        /// Benchmark invoking on same thread (should be fast path)
        /// </summary>
        [Benchmark]
        public int Invoke_SameThread()
        {
            return _dispatcher!.Invoke(() => ++_counter);
        }

        /// <summary>
        /// Benchmark InvokeAsync
        /// </summary>
        [Benchmark]
        public async Task<int> InvokeAsync_Action()
        {
            var result = await _dispatcher!.InvokeAsync(() => ++_counter);
            return result;
        }

        /// <summary>
        /// Benchmark CheckAccess (very frequent operation)
        /// </summary>
        [Benchmark]
        public bool CheckAccess()
        {
            return _dispatcher!.CheckAccess();
        }

        /// <summary>
        /// Benchmark multiple posts followed by RunJobs
        /// </summary>
        [Benchmark]
        [Arguments(10)]
        [Arguments(50)]
        [Arguments(100)]
        public int BatchedPosts(int count)
        {
            _counter = 0;
            for (int i = 0; i < count; i++)
            {
                _dispatcher!.Post(() => _counter++);
            }
            _dispatcher!.RunJobs();
            return _counter;
        }

        /// <summary>
        /// Benchmark posting with cancellation token
        /// </summary>
        [Benchmark]
        public void Post_WithCancellationToken()
        {
            using var cts = new CancellationTokenSource();
            _dispatcher!.Post(() => _counter++, DispatcherPriority.Normal);
            _dispatcher.RunJobs();
        }

        /// <summary>
        /// Benchmark VerifyAccess (throws if wrong thread)
        /// </summary>
        [Benchmark]
        public void VerifyAccess()
        {
            _dispatcher!.VerifyAccess();
        }
    }
}
