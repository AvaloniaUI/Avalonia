using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Data
{
    [MemoryDiagnoser]
    public class DataValidationBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;
        private TextBox? _target;
        private ValidatingViewModel? _viewModel;

        [Params(0, 1, 3)]
        public int ErrorCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };

            _viewModel = new ValidatingViewModel();
            _target = new TextBox();
            _root.Child = _target;
            _root.DataContext = _viewModel;

            // Bind property - data validation is enabled by default for TextBox.Text
            _target[!TextBox.TextProperty] = new Binding("Value");

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark validation with no errors - exercises the optimized foreach path in IndeiValidationPlugin
        /// </summary>
        [Benchmark(Baseline = true)]
        public void ValidateWithNoErrors()
        {
            _viewModel!.SetErrorCount(0);
            _viewModel.Value = "test1";
            _viewModel.Value = "test2";
        }

        /// <summary>
        /// Benchmark validation with errors - exercises the optimized error collection path
        /// </summary>
        [Benchmark]
        public void ValidateWithErrors()
        {
            _viewModel!.SetErrorCount(ErrorCount);
            _viewModel.Value = "test1";
            _viewModel.Value = "test2";
        }

        /// <summary>
        /// Benchmark toggling between valid and invalid states
        /// </summary>
        [Benchmark]
        public void ToggleValidationState()
        {
            for (int i = 0; i < 10; i++)
            {
                _viewModel!.SetErrorCount(i % 2 == 0 ? 0 : ErrorCount);
                _viewModel.Value = $"test{i}";
            }
        }

        /// <summary>
        /// View model that implements INotifyDataErrorInfo for validation benchmarks
        /// </summary>
        private class ValidatingViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
        {
            private string? _value;
            private int _errorCount;
            private List<string>? _errors;

            public string? Value
            {
                get => _value;
                set
                {
                    if (_value != value)
                    {
                        _value = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Value)));
                    }
                }
            }

            public void SetErrorCount(int count)
            {
                _errorCount = count;
                if (count > 0)
                {
                    _errors = new List<string>();
                    for (int i = 0; i < count; i++)
                    {
                        _errors.Add($"Error {i + 1}");
                    }
                }
                else
                {
                    _errors = null;
                }
            }

            public bool HasErrors => _errorCount > 0;

            public event PropertyChangedEventHandler? PropertyChanged;
            public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

            public IEnumerable GetErrors(string? propertyName)
            {
                if (propertyName == nameof(Value) && _errors != null)
                {
                    return _errors;
                }
                return Array.Empty<string>();
            }
        }
    }
}
