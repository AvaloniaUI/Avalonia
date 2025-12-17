using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class PropertyChangeBenchmarks
    {
        private IDisposable? _app;
        private TestControl? _control;
        private PropertyChangedEventHandler? _inpcHandler;
        private EventHandler<AvaloniaPropertyChangedEventArgs>? _apHandler;

        [Params(0, 1, 5)]
        public int SubscriberCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _control = new TestControl();

            _inpcHandler = (s, e) => { };
            _apHandler = (s, e) => { };

            // Subscribe handlers based on SubscriberCount
            for (int i = 0; i < SubscriberCount; i++)
            {
                ((INotifyPropertyChanged)_control).PropertyChanged += _inpcHandler;
                _control.PropertyChanged += _apHandler;
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (_control != null)
            {
                for (int i = 0; i < SubscriberCount; i++)
                {
                    ((INotifyPropertyChanged)_control).PropertyChanged -= _inpcHandler;
                    _control.PropertyChanged -= _apHandler;
                }
            }
            _control = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark setting a styled property (triggers full property change notification chain)
        /// </summary>
        [Benchmark(Baseline = true)]
        public void SetStyledProperty()
        {
            _control!.Width = 100;
            _control.Width = 200;
        }

        /// <summary>
        /// Benchmark setting a direct property (simpler notification path)
        /// </summary>
        [Benchmark]
        public void SetDirectProperty()
        {
            _control!.Tag = "value1";
            _control.Tag = "value2";
        }

        /// <summary>
        /// Benchmark setting a property to the same value (should be no-op)
        /// </summary>
        [Benchmark]
        public void SetPropertySameValue()
        {
            _control!.Width = 100;
            _control.Width = 100;
        }

        /// <summary>
        /// Benchmark setting multiple properties in sequence
        /// </summary>
        [Benchmark]
        public void SetMultipleProperties()
        {
            _control!.Width = 100;
            _control.Height = 100;
            _control.Opacity = 0.5;
            _control.IsVisible = false;
            _control.IsVisible = true;
        }

        /// <summary>
        /// Benchmark inherited property change propagation
        /// </summary>
        [Benchmark]
        public void SetInheritedProperty()
        {
            _control!.DataContext = new object();
            _control.DataContext = null;
        }

        private class TestControl : Control
        {
            public static readonly StyledProperty<string?> TestStyledProperty =
                AvaloniaProperty.Register<TestControl, string?>(nameof(TestStyled));

            public string? TestStyled
            {
                get => GetValue(TestStyledProperty);
                set => SetValue(TestStyledProperty, value);
            }
        }
    }
}
