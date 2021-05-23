using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.Layout;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Animations
{
    [MemoryDiagnoser]
    public class TransitionBenchmark
    {
        private readonly DoubleTransition _transition;
        private readonly DoubleTransitionOld _oldTransition;
        private readonly Subject<double> _timeProducer;
        private readonly List<double> _producedValues;
        private readonly AddValueObserver _observer;

        [Params(10, 100)]
        public int FrameCount { get; set; }

        public TransitionBenchmark()
        {
            _oldTransition = new DoubleTransitionOld
            {
                Duration = TimeSpan.FromMilliseconds(FrameCount), Property = Layoutable.WidthProperty
            };

            _transition = new DoubleTransition
            {
                Duration = TimeSpan.FromMilliseconds(FrameCount), Property = Layoutable.WidthProperty
            };

            _timeProducer = new Subject<double>();
            _producedValues = new List<double>(FrameCount);

            _observer = new AddValueObserver(_producedValues);
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

            using var transitionSub = transitionObs.Subscribe(_observer);

            for (int i = 0; i < FrameCount; i++)
            {
                _timeProducer.OnNext(i/1000d);
            }

            Debug.Assert(_producedValues.Count == FrameCount);
        }

        private class DoubleTransitionOld : Transition<double>
        {
            private static readonly DoubleAnimator s_animator = new DoubleAnimator();

            /// <inheritdocs/>
            public override IObservable<double> DoTransition(IObservable<double> progress, double oldValue, double newValue)
            {
                return progress
                    .Select(progress => s_animator.Interpolate(Easing.Ease(progress), oldValue, newValue));
            }
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
