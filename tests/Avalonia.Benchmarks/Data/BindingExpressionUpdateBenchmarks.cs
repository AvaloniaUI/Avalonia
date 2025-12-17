using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Data
{
    [MemoryDiagnoser]
    public class BindingExpressionUpdateBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;
        private TextBlock? _target;
        private ViewModel? _viewModel;

        [Params(1, 3, 5)]
        public int PathDepth { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };

            _viewModel = CreateViewModel(PathDepth);
            _target = new TextBlock();
            _root.Child = _target;
            _root.DataContext = _viewModel;

            // Bind based on path depth
            var path = GetBindingPath(PathDepth);
            _target.Bind(TextBlock.TextProperty, new Binding(path));

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        private static ViewModel CreateViewModel(int depth)
        {
            var vm = new ViewModel { Value = "initial" };
            var current = vm;
            for (int i = 1; i < depth; i++)
            {
                current.Child = new ViewModel { Value = $"level{i}" };
                current = current.Child;
            }
            return vm;
        }

        private static string GetBindingPath(int depth)
        {
            if (depth == 1) return "Value";
            var path = "Child";
            for (int i = 2; i < depth; i++)
            {
                path += ".Child";
            }
            return path + ".Value";
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark updating the source value (triggers binding update)
        /// </summary>
        [Benchmark(Baseline = true)]
        public void UpdateSourceValue()
        {
            var leaf = GetLeafViewModel(_viewModel!, PathDepth);
            leaf.Value = "updated1";
            leaf.Value = "updated2";
        }

        /// <summary>
        /// Benchmark changing an intermediate node in the path
        /// </summary>
        [Benchmark]
        public void ChangeIntermediateNode()
        {
            if (PathDepth > 1)
            {
                var original = _viewModel!.Child;
                _viewModel.Child = new ViewModel { Value = "new" };
                _viewModel.Child = original;
            }
        }

        /// <summary>
        /// Benchmark changing DataContext (rebinds entire path)
        /// </summary>
        [Benchmark]
        public void ChangeDataContext()
        {
            var newVm = CreateViewModel(PathDepth);
            _root!.DataContext = newVm;
            _root.DataContext = _viewModel;
        }

        /// <summary>
        /// Benchmark setting value to null then back
        /// </summary>
        [Benchmark]
        public void SetValueToNull()
        {
            var leaf = GetLeafViewModel(_viewModel!, PathDepth);
            var original = leaf.Value;
            leaf.Value = null;
            leaf.Value = original;
        }

        private static ViewModel GetLeafViewModel(ViewModel root, int depth)
        {
            var current = root;
            for (int i = 1; i < depth; i++)
            {
                current = current.Child!;
            }
            return current;
        }

        private class ViewModel : NotifyingBase
        {
            private string? _value;
            private ViewModel? _child;

            public string? Value
            {
                get => _value;
                set
                {
                    _value = value;
                    RaisePropertyChanged();
                }
            }

            public ViewModel? Child
            {
                get => _child;
                set
                {
                    _child = value;
                    RaisePropertyChanged();
                }
            }
        }

        private class NotifyingBase : System.ComponentModel.INotifyPropertyChanged
        {
            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

            protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
