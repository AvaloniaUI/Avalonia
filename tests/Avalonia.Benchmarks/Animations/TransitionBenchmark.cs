using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Avalonia.Animation;
using Avalonia.Layout;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Animations
{
    [MemoryDiagnoser]
    public class TransitionBenchmark
    {
        private readonly DoubleTransition _transition;
        private readonly DoubleTransitionOld _oldTransition;
        private readonly int _frameCount;
        private readonly Subject<double> _timeProducer;
        private readonly List<double> _producedValues;

        public TransitionBenchmark()
        {
            _frameCount = 100;

            _oldTransition = new DoubleTransitionOld
            {
                Duration = TimeSpan.FromMilliseconds(_frameCount), Property = Layoutable.WidthProperty
            };

            _transition = new DoubleTransition
            {
                Duration = TimeSpan.FromMilliseconds(_frameCount), Property = Layoutable.WidthProperty
            };

            _timeProducer = new Subject<double>();
            _producedValues = new List<double>(_frameCount);
        }

        [Benchmark(Baseline = true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OldTransition()
        {
            TransitionCommon(_oldTransition);
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NewTransition()
        {
            TransitionCommon(_transition);
        }

        private void TransitionCommon(Transition<double> transition)
        {
            var transitionObs = transition.DoTransition(_timeProducer, 0, 1);

            _producedValues.Clear();

            using var transitionSub = transitionObs.Subscribe(new AddValueObserver(_producedValues));

            for (int i = 0; i < _frameCount; i++)
            {
                _timeProducer.OnNext(TimeSpan.FromMilliseconds(i).TotalSeconds);
            }

            Debug.Assert(_producedValues.Count == _frameCount);
        }

        private class AddValueObserver : IObserver<double>
        {
            private readonly List<double> _values;

            public AddValueObserver(List<double> values)
            {
                _values = values;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(double value)
            {
                _values.Add(value);
            }
        }
    }
}
