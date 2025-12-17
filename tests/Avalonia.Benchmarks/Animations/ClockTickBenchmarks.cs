using System;
using Avalonia.Animation;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Animations
{
    [MemoryDiagnoser]
    public class ClockTickBenchmarks
    {
        private IDisposable? _app;
        private TestClock? _clock;
        private IDisposable?[] _subscriptions = Array.Empty<IDisposable>();
        private int _tickCount;

        [Params(1, 3, 5, 10)]
        public int SubscriberCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _clock = new TestClock();
            _tickCount = 0;

            _subscriptions = new IDisposable[SubscriberCount];
            for (int i = 0; i < SubscriberCount; i++)
            {
                _subscriptions[i] = _clock.Subscribe(new TickObserver(this));
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            foreach (var sub in _subscriptions)
            {
                sub?.Dispose();
            }
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark a single clock pulse with multiple subscribers
        /// </summary>
        [Benchmark(Baseline = true)]
        public int SinglePulse()
        {
            _tickCount = 0;
            _clock!.Pulse(TimeSpan.FromMilliseconds(16));
            return _tickCount;
        }

        /// <summary>
        /// Benchmark 60 pulses (1 second at 60fps)
        /// </summary>
        [Benchmark]
        public int SixtyPulses()
        {
            _tickCount = 0;
            for (int i = 0; i < 60; i++)
            {
                _clock!.Pulse(TimeSpan.FromMilliseconds(i * 16));
            }
            return _tickCount;
        }

        private class TickObserver : IObserver<TimeSpan>
        {
            private readonly ClockTickBenchmarks _parent;

            public TickObserver(ClockTickBenchmarks parent)
            {
                _parent = parent;
            }

            public void OnCompleted() { }
            public void OnError(Exception error) { }
            public void OnNext(TimeSpan value) => _parent._tickCount++;
        }

        private class TestClock : IClock
        {
            private readonly LightweightClockObservable _observable = new();
            public PlayState PlayState { get; set; } = PlayState.Run;

            public void Pulse(TimeSpan time)
            {
                _observable.Pulse(time);
            }

            public IDisposable Subscribe(IObserver<TimeSpan> observer)
            {
                return _observable.Subscribe(observer);
            }
        }

        private sealed class LightweightClockObservable : Avalonia.Reactive.LightweightObservableBase<TimeSpan>
        {
            public void Pulse(TimeSpan time) => PublishNext(time);
            protected override void Initialize() { }
            protected override void Deinitialize() { }
        }
    }
}
