using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;
using Avalonia.Data;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Base
{
    [MemoryDiagnoser]
    public class StyledPropertyBenchmarks
    {
        [Benchmark]
        public void Set_Int_Property_LocalValue()
        {
            var obj = new StyledClass();

            for (var i = 0; i < 100; ++i)
            {
                obj.IntValue += 1;
            }
        }

        [Benchmark]
        public void Set_Int_Property_Multiple_Priorities()
        {
            var obj = new StyledClass();
            var value = 0;

            for (var i = 0; i < 100; ++i)
            {
                for (var p = BindingPriority.Animation; p <= BindingPriority.Style; ++p)
                {
                    obj.SetValue(StyledClass.IntValueProperty, value++, p);
                }
            }
        }

        [Benchmark]
        public void Set_Int_Property_TemplatedParent()
        {
            var obj = new StyledClass();

            for (var i = 0; i < 100; ++i)
            {
                obj.SetValue(StyledClass.IntValueProperty, obj.IntValue + 1, BindingPriority.Template);
            }
        }

        [Benchmark]
        public void Bind_Int_Property_LocalValue()
        {
            var obj = new StyledClass();
            var source = new Subject<BindingValue<int>>();

            obj.Bind(StyledClass.IntValueProperty, source);

            for (var i = 0; i < 100; ++i)
            {
                source.OnNext(i);
            }
        }

        [Benchmark]
        public void Bind_Int_Property_Multiple_Priorities()
        {
            var obj = new StyledClass();
            var sources = new List<Subject<BindingValue<int>>>();
            var value = 0;

            for (var p = BindingPriority.Animation; p <= BindingPriority.Style; ++p)
            {
                var source = new Subject<BindingValue<int>>();
                sources.Add(source);
                obj.Bind(StyledClass.IntValueProperty, source, p);
            }

            for (var i = 0; i < 100; ++i)
            {
                foreach (var source in sources)
                {
                    source.OnNext(value++);
                }
            }
        }

        [Benchmark]
        public void Set_Validated_Int_Property_LocalValue()
        {
            var obj = new StyledClass();

            for (var i = 0; i < 100; ++i)
            {
                obj.ValidatedIntValue += 1;
            }
        }

        [Benchmark]
        public void Set_Coerced_Int_Property_LocalValue()
        {
            var obj = new StyledClass();

            for (var i = 0; i < 100; ++i)
            {
                obj.CoercedIntValue += 1;
            }
        }

        class StyledClass : AvaloniaObject
        {
            public static readonly StyledProperty<int> IntValueProperty =
                AvaloniaProperty.Register<StyledClass, int>(nameof(IntValue));
            public static readonly StyledProperty<int> ValidatedIntValueProperty =
                AvaloniaProperty.Register<StyledClass, int>(nameof(ValidatedIntValue), validate: ValidateIntValue);
            public static readonly StyledProperty<int> CoercedIntValueProperty =
                AvaloniaProperty.Register<StyledClass, int>(nameof(CoercedIntValue), coerce: CoerceIntValue);

            public int IntValue
            {
                get => GetValue(IntValueProperty);
                set => SetValue(IntValueProperty, value);
            }

            public int ValidatedIntValue
            {
                get => GetValue(ValidatedIntValueProperty);
                set => SetValue(ValidatedIntValueProperty, value);
            }

            public int CoercedIntValue
            {
                get => GetValue(CoercedIntValueProperty);
                set => SetValue(CoercedIntValueProperty, value);
            }

            private static bool ValidateIntValue(int arg)
            {
                return arg < 1000;
            }

            private static int CoerceIntValue(AvaloniaObject arg1, int arg2)
            {
                return Math.Min(1000, arg2);
            }
        }
    }
}
