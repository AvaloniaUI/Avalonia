using System;
using System.Reactive.Subjects;
using BenchmarkDotNet.Attributes;
using AvaloniaObservable = Avalonia.Reactive.Observable;

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class ReactiveBenchmarks
    {
        private BehaviorSubject<int>? _subject1;
        private BehaviorSubject<int>? _subject2;
        private BehaviorSubject<int>? _subject3;
        private int _result;

        [GlobalSetup]
        public void Setup()
        {
            _subject1 = new BehaviorSubject<int>(0);
            _subject2 = new BehaviorSubject<int>(0);
            _subject3 = new BehaviorSubject<int>(0);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _subject1?.Dispose();
            _subject2?.Dispose();
            _subject3?.Dispose();
        }

        /// <summary>
        /// Benchmark CombineLatest with 2 sources - measures LINQ optimization (AllOthersDone, AllTrue)
        /// </summary>
        [Benchmark(Baseline = true)]
        public int CombineLatest_TwoSources()
        {
            _result = 0;
            using var subscription = AvaloniaObservable.CombineLatest(
                _subject1!,
                _subject2!,
                (a, b) => a + b)
                .Subscribe(x => _result = x);

            // Trigger updates
            _subject1.OnNext(1);
            _subject2!.OnNext(2);
            _subject1.OnNext(3);
            _subject2.OnNext(4);

            return _result;
        }

        /// <summary>
        /// Benchmark CombineLatest completion path - exercises the optimized AllTrue path
        /// </summary>
        [Benchmark]
        public int CombineLatest_Completion()
        {
            _result = 0;
            var s1 = new BehaviorSubject<int>(1);
            var s2 = new BehaviorSubject<int>(2);

            using var subscription = AvaloniaObservable.CombineLatest(
                s1,
                s2,
                (a, b) => a + b)
                .Subscribe(x => _result = x);

            s1.OnCompleted();
            s2.OnCompleted();

            s1.Dispose();
            s2.Dispose();

            return _result;
        }

        /// <summary>
        /// Benchmark rapid value updates through CombineLatest
        /// </summary>
        [Benchmark]
        public int CombineLatest_RapidUpdates()
        {
            _result = 0;
            using var subscription = AvaloniaObservable.CombineLatest(
                _subject1!,
                _subject2!,
                (a, b) => a + b)
                .Subscribe(x => _result = x);

            // Rapid updates to stress the AllTrue/AllOthersDone paths
            for (int i = 0; i < 100; i++)
            {
                _subject1.OnNext(i);
                _subject2!.OnNext(i);
            }

            return _result;
        }
    }
}
