// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Markup.Xaml.Data;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Direct
    {
        [Fact]
        public void GetValue_Gets_Value()
        {
            var target = new Class1();

            Assert.Equal("initial", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetValue_Gets_Value_NonGeneric()
        {
            var target = new Class1();

            Assert.Equal("initial", target.GetValue((AvaloniaProperty)Class1.FooProperty));
        }

        [Fact]
        public void GetValue_On_Unregistered_Property_Throws_Exception()
        {
            var target = new Class2();

            Assert.Throws<ArgumentException>(() => target.GetValue(Class1.BarProperty));
        }

        [Fact]
        public void SetValue_Sets_Value()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.Foo);
        }

        [Fact]
        public void SetValue_Sets_Value_NonGeneric()
        {
            var target = new Class1();

            target.SetValue((AvaloniaProperty)Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.Foo);
        }

        [Fact]
        public void SetValue_NonGeneric_Coerces_UnsetValue_To_Default_Value()
        {
            var target = new Class1();

            target.SetValue((AvaloniaProperty)Class1.BazProperty, AvaloniaProperty.UnsetValue);

            Assert.Equal(-1, target.Baz);
        }

        [Fact]
        public void SetValue_Raises_PropertyChanged()
        {
            var target = new Class1();
            bool raised = false;

            target.PropertyChanged += (s, e) =>
                raised = e.Property == Class1.FooProperty &&
                         (string)e.OldValue == "initial" &&
                         (string)e.NewValue == "newvalue" &&
                         e.Priority == BindingPriority.LocalValue;

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.True(raised);
        }

        [Fact]
        public void SetValue_Raises_Changed()
        {
            var target = new Class1();
            bool raised = false;

            Class1.FooProperty.Changed.Subscribe(e =>
                raised = e.Property == Class1.FooProperty &&
                         (string)e.OldValue == "initial" &&
                         (string)e.NewValue == "newvalue" &&
                         e.Priority == BindingPriority.LocalValue);

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.True(raised);
        }

        [Fact]
        public void SetValue_On_Unregistered_Property_Throws_Exception()
        {
            var target = new Class2();

            Assert.Throws<ArgumentException>(() => target.SetValue(Class1.BarProperty, "value"));
        }

        [Fact]
        public void GetObservable_Returns_Values()
        {
            var target = new Class1();
            List<string> values = new List<string>();

            target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
            target.Foo = "newvalue";

            Assert.Equal(new[] { "initial", "newvalue" }, values);
        }

        [Fact]
        public void Bind_Binds_Property_Value()
        {
            var target = new Class1();
            var source = new Subject<string>();

            var sub = target.Bind(Class1.FooProperty, source);

            Assert.Equal("initial", target.Foo);
            source.OnNext("first");
            Assert.Equal("first", target.Foo);
            source.OnNext("second");
            Assert.Equal("second", target.Foo);

            sub.Dispose();

            source.OnNext("third");
            Assert.Equal("second", target.Foo);
        }

        [Fact]
        public void Bind_Binds_Property_Value_NonGeneric()
        {
            var target = new Class1();
            var source = new Subject<string>();

            var sub = target.Bind((AvaloniaProperty)Class1.FooProperty, source);

            Assert.Equal("initial", target.Foo);
            source.OnNext("first");
            Assert.Equal("first", target.Foo);
            source.OnNext("second");
            Assert.Equal("second", target.Foo);

            sub.Dispose();

            source.OnNext("third");
            Assert.Equal("second", target.Foo);
        }

        [Fact]
        public void Bind_NonGeneric_Uses_UnsetValue()
        {
            var target = new Class1();
            var source = new Subject<object>();

            var sub = target.Bind((AvaloniaProperty)Class1.BazProperty, source);

            Assert.Equal(5, target.Baz);
            source.OnNext(6);
            Assert.Equal(6, target.Baz);
            source.OnNext(AvaloniaProperty.UnsetValue);
            Assert.Equal(-1, target.Baz);
        }

        [Fact]
        public void Bind_Handles_Wrong_Type()
        {
            var target = new Class1();
            var source = new Subject<object>();

            var sub = target.Bind(Class1.FooProperty, source);

            source.OnNext(45);

            Assert.Null(target.Foo);
        }

        [Fact]
        public void Bind_Handles_Wrong_Value_Type()
        {
            var target = new Class1();
            var source = new Subject<object>();

            var sub = target.Bind(Class1.BazProperty, source);

            source.OnNext("foo");

            Assert.Equal(0, target.Baz);
        }

        [Fact]
        public void ReadOnly_Property_Cannot_Be_Set()
        {
            var target = new Class1();

            Assert.Throws<ArgumentException>(() =>
                target.SetValue(Class1.BarProperty, "newvalue"));
        }

        [Fact]
        public void ReadOnly_Property_Cannot_Be_Set_NonGeneric()
        {
            var target = new Class1();

            Assert.Throws<ArgumentException>(() =>
                target.SetValue((AvaloniaProperty)Class1.BarProperty, "newvalue"));
        }

        [Fact]
        public void ReadOnly_Property_Cannot_Be_Bound()
        {
            var target = new Class1();
            var source = new Subject<string>();

            Assert.Throws<ArgumentException>(() =>
                target.Bind(Class1.BarProperty, source));
        }

        [Fact]
        public void ReadOnly_Property_Cannot_Be_Bound_NonGeneric()
        {
            var target = new Class1();
            var source = new Subject<string>();

            Assert.Throws<ArgumentException>(() =>
                target.Bind(Class1.BarProperty, source));
        }

        [Fact]
        public void GetValue_Gets_Value_On_AddOwnered_Property()
        {
            var target = new Class2();

            Assert.Equal("initial2", target.GetValue(Class2.FooProperty));
        }

        [Fact]
        public void GetValue_Gets_Value_On_AddOwnered_Property_Using_Original()
        {
            var target = new Class2();

            Assert.Equal("initial2", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void GetValue_Gets_Value_On_AddOwnered_Property_Using_Original_NonGeneric()
        {
            var target = new Class2();

            Assert.Equal("initial2", target.GetValue((AvaloniaProperty)Class1.FooProperty));
        }

        [Fact]
        public void SetValue_Sets_Value_On_AddOwnered_Property_Using_Original()
        {
            var target = new Class2();

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.Foo);
        }

        [Fact]
        public void SetValue_Sets_Value_On_AddOwnered_Property_Using_Original_NonGeneric()
        {
            var target = new Class2();

            target.SetValue((AvaloniaProperty)Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.Foo);
        }

        [Fact]
        public void UnsetValue_Is_Used_On_AddOwnered_Property()
        {
            var target = new Class2();

            target.SetValue((AvaloniaProperty)Class1.FooProperty, AvaloniaProperty.UnsetValue);

            Assert.Equal("unset", target.Foo);
        }

        [Fact]
        public void Bind_Binds_AddOwnered_Property_Value()
        {
            var target = new Class2();
            var source = new Subject<string>();

            var sub = target.Bind(Class1.FooProperty, source);

            Assert.Equal("initial2", target.Foo);
            source.OnNext("first");
            Assert.Equal("first", target.Foo);
            source.OnNext("second");
            Assert.Equal("second", target.Foo);

            sub.Dispose();

            source.OnNext("third");
            Assert.Equal("second", target.Foo);
        }

        [Fact]
        public void Bind_Binds_AddOwnered_Property_Value_NonGeneric()
        {
            var target = new Class2();
            var source = new Subject<string>();

            var sub = target.Bind((AvaloniaProperty)Class1.FooProperty, source);

            Assert.Equal("initial2", target.Foo);
            source.OnNext("first");
            Assert.Equal("first", target.Foo);
            source.OnNext("second");
            Assert.Equal("second", target.Foo);

            sub.Dispose();

            source.OnNext("third");
            Assert.Equal("second", target.Foo);
        }

        [Fact]
        public void Property_Notifies_Initialized()
        {
            bool raised = false;

            Class1.FooProperty.Initialized.Subscribe(e =>
                raised = e.Property == Class1.FooProperty &&
                         e.OldValue == AvaloniaProperty.UnsetValue &&
                         (string)e.NewValue == "initial" &&
                         e.Priority == BindingPriority.Unset);

            var target = new Class1();

            Assert.True(raised);
        }

        [Fact]
        public void DataValidationError_Does_Not_Cause_Target_Update()
        {
            var target = new Class1();
            var source = new Subject<object>();

            target.Bind(Class1.FooProperty, source);
            source.OnNext("initial");
            source.OnNext(new BindingNotification(new InvalidOperationException("Foo"), BindingErrorType.DataValidationError));

            Assert.Equal("initial", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void BindingError_With_FallbackValue_Causes_Target_Update()
        {
            var target = new Class1();
            var source = new Subject<object>();

            target.Bind(Class1.FooProperty, source);
            source.OnNext("initial");
            source.OnNext(new BindingNotification(
                new InvalidOperationException("Foo"),
                BindingErrorType.Error,
                "fallback"));

            Assert.Equal("fallback", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Binding_To_Direct_Property_Logs_BindingError()
        {
            var target = new Class1();
            var source = new Subject<object>();
            var called = false;

            LogCallback checkLogMessage = (level, area, src, mt, pv) =>
            {
                if (level == LogEventLevel.Warning &&
                    area == LogArea.Binding &&
                    mt == "Error in binding to {Target}.{Property}: {Message}" &&
                    pv.Length == 3 &&
                    pv[0] is Class1 &&
                    object.ReferenceEquals(pv[1], Class1.FooProperty) &&
                    (string)pv[2] == "Binding Error Message")
                {
                    called = true;
                }
            };

            using (TestLogSink.Start(checkLogMessage))
            {
                target.Bind(Class1.FooProperty, source);
                source.OnNext("baz");
                source.OnNext(new BindingNotification(new InvalidOperationException("Binding Error Message"), BindingErrorType.Error));
            }

            Assert.True(called);
        }

        [Fact]
        public async Task Bind_Executes_On_UIThread()
        {
            var target = new Class1();
            var source = new Subject<object>();
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            var threadingInterfaceMock = new Mock<IPlatformThreadingInterface>();
            threadingInterfaceMock.SetupGet(mock => mock.CurrentThreadIsLoopThread)
                .Returns(() => Thread.CurrentThread.ManagedThreadId == currentThreadId);

            var services = new TestServices(
                threadingInterface: threadingInterfaceMock.Object);

            using (UnitTestApplication.Start(services))
            {
                target.Bind(Class1.FooProperty, source);

                await Task.Run(() => source.OnNext("foobar"));
            }
        }

        [Fact]
        public void AddOwner_Should_Inherit_DefaultBindingMode()
        {
            var foo = new DirectProperty<Class1, string>(
                "foo",
                o => "foo",
                null,
                new DirectPropertyMetadata<string>(defaultBindingMode: BindingMode.TwoWay));
            var bar = foo.AddOwner<Class2>(o => "bar");

            Assert.Equal(BindingMode.TwoWay, bar.GetMetadata<Class1>().DefaultBindingMode);
            Assert.Equal(BindingMode.TwoWay, bar.GetMetadata<Class2>().DefaultBindingMode);
        }

        [Fact]
        public void AddOwner_Can_Override_DefaultBindingMode()
        {
            var foo = new DirectProperty<Class1, string>(
                "foo",
                o => "foo",
                null,
                new DirectPropertyMetadata<string>(defaultBindingMode: BindingMode.TwoWay));
            var bar = foo.AddOwner<Class2>(o => "bar", defaultBindingMode: BindingMode.OneWayToSource);

            Assert.Equal(BindingMode.TwoWay, bar.GetMetadata<Class1>().DefaultBindingMode);
            Assert.Equal(BindingMode.OneWayToSource, bar.GetMetadata<Class2>().DefaultBindingMode);
        }

        [Fact]
        public void SetValue_Should_Not_Cause_StackOverflow_And_Have_Correct_Values()
        {
            var viewModel = new TestStackOverflowViewModel()
            {
                Value = 50
            };

            var target = new Class1();

            target.Bind(Class1.DoubleValueProperty, new Binding("Value")
                                                    {
                                                        Mode = BindingMode.TwoWay,
                                                        Source = viewModel
                                                    });

            var child = new Class1();

            child[!!Class1.DoubleValueProperty] = target[!!Class1.DoubleValueProperty];

            Assert.Equal(1, viewModel.SetterInvokedCount);

            // Issues #855 and #824 were causing a StackOverflowException at this point.
            target.DoubleValue = 51.001;

            Assert.Equal(2, viewModel.SetterInvokedCount);

            double expected = 51;

            Assert.Equal(expected, viewModel.Value);
            Assert.Equal(expected, target.DoubleValue);
            Assert.Equal(expected, child.DoubleValue);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly DirectProperty<Class1, string> FooProperty =
                AvaloniaProperty.RegisterDirect<Class1, string>(
                    nameof(Foo),
                    o => o.Foo,
                    (o, v) => o.Foo = v,
                    unsetValue: "unset");

            public static readonly DirectProperty<Class1, string> BarProperty =
                AvaloniaProperty.RegisterDirect<Class1, string>(nameof(Bar), o => o.Bar);

            public static readonly DirectProperty<Class1, int> BazProperty =
                AvaloniaProperty.RegisterDirect<Class1, int>(
                    nameof(Baz),
                    o => o.Baz,
                    (o, v) => o.Baz = v,
                    unsetValue: -1);

            public static readonly DirectProperty<Class1, double> DoubleValueProperty =
                AvaloniaProperty.RegisterDirect<Class1, double>(
                    nameof(DoubleValue),
                    o => o.DoubleValue,
                    (o, v) => o.DoubleValue = v);

            private string _foo = "initial";
            private readonly string _bar = "bar";
            private int _baz = 5;
            private double _doubleValue;

            public string Foo
            {
                get { return _foo; }
                set { SetAndRaise(FooProperty, ref _foo, value); }
            }

            public string Bar
            {
                get { return _bar; }
            }

            public int Baz
            {
                get { return _baz; }
                set { SetAndRaise(BazProperty, ref _baz, value); }
            }

            public double DoubleValue
            {
                get { return _doubleValue; }
                set { SetAndRaise(DoubleValueProperty, ref _doubleValue, value); }
            }
        }

        private class Class2 : AvaloniaObject
        {
            public static readonly DirectProperty<Class2, string> FooProperty =
                Class1.FooProperty.AddOwner<Class2>(o => o.Foo, (o, v) => o.Foo = v);

            private string _foo = "initial2";

            static Class2()
            {
            }

            public string Foo
            {
                get { return _foo; }
                set { SetAndRaise(FooProperty, ref _foo, value); }
            }
        }

        private class TestStackOverflowViewModel : INotifyPropertyChanged
        {
            public int SetterInvokedCount { get; private set; }

            public const int MaxInvokedCount = 1000;

            private double _value;

            public event PropertyChangedEventHandler PropertyChanged;

            public double Value
            {
                get { return _value; }
                set
                {
                    if (_value != value)
                    {
                        SetterInvokedCount++;
                        if (SetterInvokedCount < MaxInvokedCount)
                        {
                            _value = (int)value;
                            if (_value > 75) _value = 75;
                            if (_value < 25) _value = 25;
                        }
                        else
                        {
                            _value = value;
                        }

                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                    }
                }
            }
        }
    }
}