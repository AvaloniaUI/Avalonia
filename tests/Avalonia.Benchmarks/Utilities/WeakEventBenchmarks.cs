using System;
using System.ComponentModel;
using Avalonia.Utilities;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Utilities
{
    [MemoryDiagnoser]
    public class WeakEventBenchmarks
    {
        private TestNotifyPropertyChanged? _source;
        private TestSubscriber? _subscriber;

        [Params(1, 5, 10)]
        public int SubscriberCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _source = new TestNotifyPropertyChanged();
            _subscriber = new TestSubscriber();

            // Pre-subscribe some handlers
            for (int i = 0; i < SubscriberCount; i++)
            {
                WeakEvents.ThreadSafePropertyChanged.Subscribe(_source, _subscriber);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _source = null;
            _subscriber = null;
        }

        /// <summary>
        /// Benchmark subscribing to weak event
        /// </summary>
        [Benchmark(Baseline = true)]
        public void Subscribe()
        {
            var source = new TestNotifyPropertyChanged();
            var sub = new TestSubscriber();
            WeakEvents.ThreadSafePropertyChanged.Subscribe(source, sub);
        }

        /// <summary>
        /// Benchmark raising property changed with weak event subscribers
        /// </summary>
        [Benchmark]
        public void RaisePropertyChanged()
        {
            _source!.RaisePropertyChanged("TestProperty");
        }

        /// <summary>
        /// Benchmark unsubscribe from weak event
        /// </summary>
        [Benchmark]
        public void Unsubscribe()
        {
            var source = new TestNotifyPropertyChanged();
            var sub = new TestSubscriber();
            WeakEvents.ThreadSafePropertyChanged.Subscribe(source, sub);
            WeakEvents.ThreadSafePropertyChanged.Unsubscribe(source, sub);
        }

        /// <summary>
        /// Benchmark full subscribe/raise/unsubscribe cycle
        /// </summary>
        [Benchmark]
        public void FullCycle()
        {
            var source = new TestNotifyPropertyChanged();
            var sub = new TestSubscriber();
            WeakEvents.ThreadSafePropertyChanged.Subscribe(source, sub);
            source.RaisePropertyChanged("Test");
            WeakEvents.ThreadSafePropertyChanged.Unsubscribe(source, sub);
        }

        private class TestNotifyPropertyChanged : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            public void RaisePropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private class TestSubscriber : IWeakEventSubscriber<PropertyChangedEventArgs>
        {
            public int CallCount { get; private set; }

            public void OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs args)
            {
                CallCount++;
            }
        }
    }
}
