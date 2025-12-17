using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Data
{
    [MemoryDiagnoser]
    public class ValueConverterBenchmarks
    {
        private IMultiValueConverter _andConverter = null!;
        private IMultiValueConverter _orConverter = null!;
        private IMultiValueConverter _customSumConverter = null!;
        private IList<object?> _boolValues = null!;
        private IList<object?> _intValues = null!;
        private IList<object?> _mixedValues = null!;

        [GlobalSetup]
        public void Setup()
        {
            _andConverter = BoolConverters.And;
            _orConverter = BoolConverters.Or;
            _customSumConverter = new FuncMultiValueConverter<int, int>(values => 
            {
                int sum = 0;
                foreach (int? v in values)
                {
                    if (v.HasValue)
                        sum += v.Value;
                }
                return sum;
            });

            _boolValues = new List<object?> { true, true, true, false };
            _intValues = new List<object?> { 1, 2, 3, 4, 5 };
            _mixedValues = new List<object?> { true, false, "invalid", true };
        }

        /// <summary>
        /// Benchmark BoolConverters.And (uses LINQ .All)
        /// </summary>
        [Benchmark(Baseline = true)]
        public object? BoolConverters_And()
        {
            return _andConverter.Convert(_boolValues, typeof(bool), null, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Benchmark BoolConverters.Or (uses LINQ .Any)
        /// </summary>
        [Benchmark]
        public object? BoolConverters_Or()
        {
            return _orConverter.Convert(_boolValues, typeof(bool), null, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Benchmark FuncMultiValueConverter with int values
        /// </summary>
        [Benchmark]
        public object? FuncConverter_IntSum()
        {
            return _customSumConverter.Convert(_intValues, typeof(int), null, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Benchmark with mixed/invalid values (causes type filtering)
        /// </summary>
        [Benchmark]
        public object? BoolConverters_And_MixedValues()
        {
            return _andConverter.Convert(_mixedValues, typeof(bool), null, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Benchmark small input (2 values) - common case
        /// </summary>
        [Benchmark]
        public object? BoolConverters_And_SmallInput()
        {
            var values = new List<object?> { true, true };
            return _andConverter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Benchmark larger input (10 values)
        /// </summary>
        [Benchmark]
        public object? BoolConverters_And_LargeInput()
        {
            var values = new List<object?> { true, true, true, true, true, true, true, true, true, true };
            return _andConverter.Convert(values, typeof(bool), null, CultureInfo.InvariantCulture);
        }
    }
}
