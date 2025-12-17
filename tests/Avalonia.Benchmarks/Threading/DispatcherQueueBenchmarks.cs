using System;
using System.Threading;
using Avalonia.Threading;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Threading
{
    [MemoryDiagnoser]
    public class DispatcherQueueBenchmarks
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
        /// Benchmark enqueueing many operations at different priorities
        /// </summary>
        [Benchmark(Baseline = true)]
        public int EnqueueMixedPriorities()
        {
            _counter = 0;
            _dispatcher!.Post(() => _counter++, DispatcherPriority.Background);
            _dispatcher.Post(() => _counter++, DispatcherPriority.Normal);
            _dispatcher.Post(() => _counter++, DispatcherPriority.Render);
            _dispatcher.Post(() => _counter++, DispatcherPriority.Input);
            _dispatcher.Post(() => _counter++, DispatcherPriority.Send);
            _dispatcher.RunJobs();
            return _counter;
        }

        /// <summary>
        /// Benchmark rapid enqueue/dequeue cycles
        /// </summary>
        [Benchmark]
        [Arguments(10)]
        [Arguments(50)]
        [Arguments(100)]
        public int RapidEnqueueDequeue(int count)
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
        /// Benchmark priority interleaving (common in UI scenarios)
        /// </summary>
        [Benchmark]
        public int PriorityInterleaving()
        {
            _counter = 0;
            // Simulate typical UI scenario: render -> input -> normal operations
            for (int i = 0; i < 10; i++)
            {
                _dispatcher!.Post(() => _counter++, DispatcherPriority.Render);
                _dispatcher.Post(() => _counter++, DispatcherPriority.Input);
                _dispatcher.Post(() => _counter++, DispatcherPriority.Normal);
            }
            _dispatcher!.RunJobs();
            return _counter;
        }

        /// <summary>
        /// Benchmark InvokeAsync with immediate result
        /// </summary>
        [Benchmark]
        public int InvokeAsyncBatch()
        {
            _counter = 0;
            for (int i = 0; i < 10; i++)
            {
                _dispatcher!.InvokeAsync(() => _counter++);
            }
            _dispatcher!.RunJobs();
            return _counter;
        }

        /// <summary>
        /// Benchmark scheduling with Send priority (highest)
        /// </summary>
        [Benchmark]
        public int HighPriorityOperations()
        {
            _counter = 0;
            for (int i = 0; i < 10; i++)
            {
                _dispatcher!.Post(() => _counter++, DispatcherPriority.Send);
            }
            _dispatcher!.RunJobs();
            return _counter;
        }

        /// <summary>
        /// Benchmark scheduling with Background priority (lowest)
        /// </summary>
        [Benchmark]
        public int LowPriorityOperations()
        {
            _counter = 0;
            for (int i = 0; i < 10; i++)
            {
                _dispatcher!.Post(() => _counter++, DispatcherPriority.Background);
            }
            _dispatcher!.RunJobs();
            return _counter;
        }
    }
}
