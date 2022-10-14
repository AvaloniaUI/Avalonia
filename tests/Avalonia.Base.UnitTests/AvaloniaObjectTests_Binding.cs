using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Microsoft.Reactive.Testing;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Binding
    {
        [Fact]
        public void Bind_Sets_Current_Value()
        {
            var target = new Class1();
            var source = new Class1();
            var property = Class1.FooProperty;

            source.SetValue(property, "initial");
            target.Bind(property, source.GetObservable(property));

            Assert.Equal("initial", target.GetValue(property));
        }

        [Fact]
        public void Bind_Raises_PropertyChanged()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<string>>();
            bool raised = false;

            target.PropertyChanged += (s, e) =>
                raised = e.Property == Class1.FooProperty &&
                         (string)e.OldValue == "foodefault" &&
                         (string)e.NewValue == "newvalue" &&
                         e.Priority == BindingPriority.LocalValue;

            target.Bind(Class1.FooProperty, source);
            source.OnNext("newvalue");

            Assert.True(raised);
        }

        [Fact]
        public void PropertyChanged_Not_Raised_When_Value_Unchanged()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<string>>();
            var raised = 0;

            target.PropertyChanged += (s, e) => ++raised;
            target.Bind(Class1.FooProperty, source);
            source.OnNext("newvalue");
            source.OnNext("newvalue");

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Setting_LocalValue_Overrides_Binding_Until_Binding_Produces_Next_Value()
        {
            var target = new Class1();
            var source = new Subject<string>();
            var property = Class1.FooProperty;

            target.Bind(property, source);
            source.OnNext("foo");
            Assert.Equal("foo", target.GetValue(property));

            target.SetValue(property, "bar");
            Assert.Equal("bar", target.GetValue(property));

            source.OnNext("baz"); 
            Assert.Equal("baz", target.GetValue(property));
        }

        [Fact]
        public void Completing_LocalValue_Binding_Reverts_To_Default_Value_Even_When_Local_Value_Set_Earlier()
        {
            var target = new Class1();
            var source = new Subject<string>();
            var property = Class1.FooProperty;

            target.Bind(property, source);
            source.OnNext("foo");
            target.SetValue(property, "bar");
            source.OnNext("baz");
            source.OnCompleted();

            Assert.Equal("foodefault", target.GetValue(property));
        }

        [Fact]
        public void Completing_LocalValue_Binding_Should_Not_Revert_To_Set_LocalValue()
        {
            var target = new Class1();
            var source = new BehaviorSubject<string>("bar");

            target.SetValue(Class1.FooProperty, "foo");
            var sub = target.Bind(Class1.FooProperty, source);

            Assert.Equal("bar", target.GetValue(Class1.FooProperty));

            sub.Dispose();

            Assert.Equal("foodefault", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Completing_Animation_Binding_Reverts_To_Set_LocalValue()
        {
            var target = new Class1();
            var source = new Subject<string>();
            var property = Class1.FooProperty;

            target.SetValue(property, "foo");
            target.Bind(property, source, BindingPriority.Animation);
            source.OnNext("bar");
            source.OnCompleted();

            Assert.Equal("foo", target.GetValue(property));
        }

        [Fact]
        public void Completing_LocalValue_Binding_Raises_PropertyChanged()
        {
            var target = new Class1();
            var source = new BehaviorSubject<BindingValue<string>>("foo");
            var property = Class1.FooProperty;
            var raised = 0;

            target.Bind(property, source);
            Assert.Equal("foo", target.GetValue(property));

            target.PropertyChanged += (s, e) =>
            {
                Assert.Equal(BindingPriority.Unset, e.Priority);
                Assert.Equal(property, e.Property);
                Assert.Equal("foo", e.OldValue as string);
                Assert.Equal("foodefault", e.NewValue as string);
                ++raised;
            };

            source.OnCompleted();

            Assert.Equal("foodefault", target.GetValue(property));
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Completing_Style_Binding_Raises_PropertyChanged()
        {
            var target = new Class1();
            var source = new BehaviorSubject<BindingValue<string>>("foo");
            var property = Class1.FooProperty;
            var raised = 0;

            target.Bind(property, source, BindingPriority.Style);
            Assert.Equal("foo", target.GetValue(property));

            target.PropertyChanged += (s, e) =>
            {
                Assert.Equal(BindingPriority.Unset, e.Priority);
                Assert.Equal(property, e.Property);
                Assert.Equal("foo", e.OldValue as string);
                Assert.Equal("foodefault", e.NewValue as string);
                ++raised;
            };

            source.OnCompleted();

            Assert.Equal("foodefault", target.GetValue(property));
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Completing_LocalValue_Binding_With_Style_Binding_Raises_PropertyChanged()
        {
            var target = new Class1();
            var source = new BehaviorSubject<BindingValue<string>>("foo");
            var property = Class1.FooProperty;
            var raised = 0;

            target.Bind(property, new BehaviorSubject<string>("bar"), BindingPriority.Style);
            target.Bind(property, source);
            Assert.Equal("foo", target.GetValue(property));

            target.PropertyChanged += (s, e) =>
            {
                Assert.Equal(BindingPriority.Style, e.Priority);
                Assert.Equal(property, e.Property);
                Assert.Equal("foo", e.OldValue as string);
                Assert.Equal("bar", e.NewValue as string);
                ++raised;
            };

            source.OnCompleted();

            Assert.Equal("bar", target.GetValue(property));
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Disposing_LocalValue_Binding_Raises_PropertyChanged()
        {
            var target = new Class1();
            var source = new BehaviorSubject<BindingValue<string>>("foo");
            var property = Class1.FooProperty;
            var raised = 0;

            var sub = target.Bind(property, source);
            Assert.Equal("foo", target.GetValue(property));

            target.PropertyChanged += (s, e) =>
            {
                Assert.Equal(BindingPriority.Unset, e.Priority);
                Assert.Equal(property, e.Property);
                Assert.Equal("foo", e.OldValue as string);
                Assert.Equal("foodefault", e.NewValue as string);
                ++raised;
            };

            sub.Dispose();

            Assert.Equal("foodefault", target.GetValue(property));
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Setting_Style_Value_Overrides_Binding_Permanently()
        {
            var target = new Class1();
            var source = new Subject<string>();

            target.Bind(Class1.FooProperty, source, BindingPriority.Style);
            source.OnNext("foo");
            Assert.Equal("foo", target.GetValue(Class1.FooProperty));

            target.SetValue(Class1.FooProperty, "bar", BindingPriority.Style);
            Assert.Equal("bar", target.GetValue(Class1.FooProperty));

            source.OnNext("baz");
            Assert.Equal("bar", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Second_LocalValue_Binding_Overrides_First()
        {
            var property = Class1.FooProperty;
            var target = new Class1();
            var source1 = new Subject<string>();
            var source2 = new Subject<string>();

            target.Bind(property, source1, BindingPriority.LocalValue);
            target.Bind(property, source2, BindingPriority.LocalValue);

            source1.OnNext("foo");
            Assert.Equal("foo", target.GetValue(property));

            source2.OnNext("bar");
            Assert.Equal("bar", target.GetValue(property));

            source1.OnNext("baz");
            Assert.Equal("bar", target.GetValue(property));
        }

        [Fact]
        public void Completing_Second_LocalValue_Binding_Reverts_To_First()
        {
            var property = Class1.FooProperty;
            var target = new Class1();
            var source1 = new Subject<string>();
            var source2 = new Subject<string>();

            target.Bind(property, source1, BindingPriority.LocalValue);
            target.Bind(property, source2, BindingPriority.LocalValue);

            source1.OnNext("foo");
            source2.OnNext("bar");
            source1.OnNext("baz");
            source2.OnCompleted();

            Assert.Equal("baz", target.GetValue(property));
        }

        [Fact]
        public void Completing_StyleTrigger_Binding_Reverts_To_StyleBinding()
        {
            var property = Class1.FooProperty;
            var target = new Class1();
            var source1 = new Subject<string>();
            var source2 = new Subject<string>();

            target.Bind(property, source1, BindingPriority.Style);
            target.Bind(property, source2, BindingPriority.StyleTrigger);

            source1.OnNext("foo");
            source2.OnNext("bar");
            source2.OnCompleted();
            source1.OnNext("baz");

            Assert.Equal("baz", target.GetValue(property));
        }

        [Fact]
        public void Bind_NonGeneric_Sets_Current_Value()
        {
            Class1 target = new Class1();
            Class1 source = new Class1();

            source.SetValue(Class1.FooProperty, "initial");
            target.Bind((AvaloniaProperty)Class1.FooProperty, source.GetObservable(Class1.FooProperty));

            Assert.Equal("initial", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Bind_To_ValueType_Accepts_UnsetValue()
        {
            var target = new Class1();
            var source = new Subject<object>();

            target.Bind(Class1.QuxProperty, source);
            source.OnNext(6.7);
            source.OnNext(AvaloniaProperty.UnsetValue);

            Assert.Equal(5.6, target.GetValue(Class1.QuxProperty));
            Assert.False(target.IsSet(Class1.QuxProperty));
        }

        [Fact]
        public void OneTime_Binding_Ignores_UnsetValue()
        {
            var target = new Class1();
            var source = new Subject<object>();

            target.Bind(Class1.QuxProperty, new TestOneTimeBinding(source));

            source.OnNext(AvaloniaProperty.UnsetValue);
            Assert.Equal(5.6, target.GetValue(Class1.QuxProperty));

            source.OnNext(6.7);
            Assert.Equal(6.7, target.GetValue(Class1.QuxProperty));
        }

        [Fact]
        public void OneTime_Binding_Ignores_Binding_Errors()
        {
            var target = new Class1();
            var source = new Subject<object>();

            target.Bind(Class1.QuxProperty, new TestOneTimeBinding(source));

            source.OnNext(new BindingNotification(new Exception(), BindingErrorType.Error));
            Assert.Equal(5.6, target.GetValue(Class1.QuxProperty));

            source.OnNext(6.7);
            Assert.Equal(6.7, target.GetValue(Class1.QuxProperty));
        }

        [Fact]
        public void Bind_Does_Not_Throw_Exception_For_Unregistered_Property()
        {
            Class1 target = new Class1();

            target.Bind(Class2.BarProperty, Observable.Never<string>().StartWith("foo"));

            Assert.Equal("foo", target.GetValue(Class2.BarProperty));
        }

        [Fact]
        public void Bind_Sets_Subsequent_Value()
        {
            Class1 target = new Class1();
            Class1 source = new Class1();

            source.SetValue(Class1.FooProperty, "initial");
            target.Bind(Class1.FooProperty, source.GetObservable(Class1.FooProperty));
            source.SetValue(Class1.FooProperty, "subsequent");

            Assert.Equal("subsequent", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Bind_Ignores_Invalid_Value_Type()
        {
            Class1 target = new Class1();
            target.Bind((AvaloniaProperty)Class1.FooProperty, Observable.Return((object)123));
            Assert.Equal("foodefault", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Observable_Is_Unsubscribed_When_Subscription_Disposed()
        {
            var scheduler = new TestScheduler();
            var source = scheduler.CreateColdObservable<string>();
            var target = new Class1();

            var subscription = target.Bind(Class1.FooProperty, source);
            Assert.Equal(1, source.Subscriptions.Count);
            Assert.Equal(Subscription.Infinite, source.Subscriptions[0].Unsubscribe);

            subscription.Dispose();
            Assert.Equal(1, source.Subscriptions.Count);
            Assert.Equal(0, source.Subscriptions[0].Unsubscribe);
        }

        [Fact]
        public void Two_Way_Separate_Binding_Works()
        {
            Class1 obj1 = new Class1();
            Class1 obj2 = new Class1();

            obj1.SetValue(Class1.FooProperty, "initial1");
            obj2.SetValue(Class1.FooProperty, "initial2");

            obj1.Bind(Class1.FooProperty, obj2.GetObservable(Class1.FooProperty));
            obj2.Bind(Class1.FooProperty, obj1.GetObservable(Class1.FooProperty));

            Assert.Equal("initial2", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("initial2", obj2.GetValue(Class1.FooProperty));

            obj1.SetValue(Class1.FooProperty, "first");

            Assert.Equal("first", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("first", obj2.GetValue(Class1.FooProperty));

            obj2.SetValue(Class1.FooProperty, "second");

            Assert.Equal("second", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("second", obj2.GetValue(Class1.FooProperty));

            obj1.SetValue(Class1.FooProperty, "third");

            Assert.Equal("third", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("third", obj2.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Two_Way_Binding_With_Priority_Works()
        {
            Class1 obj1 = new Class1();
            Class1 obj2 = new Class1();

            obj1.SetValue(Class1.FooProperty, "initial1", BindingPriority.Style);
            obj2.SetValue(Class1.FooProperty, "initial2", BindingPriority.Style);

            obj1.Bind(Class1.FooProperty, obj2.GetObservable(Class1.FooProperty), BindingPriority.Style);
            obj2.Bind(Class1.FooProperty, obj1.GetObservable(Class1.FooProperty), BindingPriority.Style);

            Assert.Equal("initial2", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("initial2", obj2.GetValue(Class1.FooProperty));

            obj1.SetValue(Class1.FooProperty, "first", BindingPriority.Style);

            Assert.Equal("first", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("first", obj2.GetValue(Class1.FooProperty));

            obj2.SetValue(Class1.FooProperty, "second", BindingPriority.Style);

            Assert.Equal("first", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("second", obj2.GetValue(Class1.FooProperty));

            obj1.SetValue(Class1.FooProperty, "third", BindingPriority.Style);

            Assert.Equal("third", obj1.GetValue(Class1.FooProperty));
            Assert.Equal("second", obj2.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Local_Binding_Overwrites_Local_Value()
        {
            var target = new Class1();
            var binding = new Subject<string>();

            target.Bind(Class1.FooProperty, binding);

            binding.OnNext("first");
            Assert.Equal("first", target.GetValue(Class1.FooProperty));

            target.SetValue(Class1.FooProperty, "second");
            Assert.Equal("second", target.GetValue(Class1.FooProperty));

            binding.OnNext("third");
            Assert.Equal("third", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void StyleBinding_Overrides_Default_Value()
        {
            Class1 target = new Class1();

            target.Bind(Class1.FooProperty, Single("stylevalue"), BindingPriority.Style);

            Assert.Equal("stylevalue", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void this_Operator_Returns_Value_Property()
        {
            Class1 target = new Class1();

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target[Class1.FooProperty]);
        }

        [Fact]
        public void this_Operator_Sets_Value_Property()
        {
            Class1 target = new Class1();

            target[Class1.FooProperty] = "newvalue";

            Assert.Equal("newvalue", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void this_Operator_Doesnt_Accept_Observable()
        {
            Class1 target = new Class1();

            Assert.Throws<ArgumentException>(() =>
            {
                target[Class1.FooProperty] = Observable.Return("newvalue");
            });
        }

        [Fact]
        public void this_Operator_Binds_One_Way()
        {
            Class1 target1 = new Class1();
            Class2 target2 = new Class2();
            IndexerDescriptor binding = Class2.BarProperty.Bind().WithMode(BindingMode.OneWay);

            target1.SetValue(Class1.FooProperty, "first");
            target2[binding] = target1[!Class1.FooProperty];
            target1.SetValue(Class1.FooProperty, "second");

            Assert.Equal("second", target2.GetValue(Class2.BarProperty));
        }

        [Fact]
        public void this_Operator_Binds_Two_Way()
        {
            Class1 target1 = new Class1();
            Class1 target2 = new Class1();

            target1.SetValue(Class1.FooProperty, "first");
            target2[!Class1.FooProperty] = target1[!!Class1.FooProperty];
            Assert.Equal("first", target2.GetValue(Class1.FooProperty));
            target1.SetValue(Class1.FooProperty, "second");
            Assert.Equal("second", target2.GetValue(Class1.FooProperty));
            target2.SetValue(Class1.FooProperty, "third");
            Assert.Equal("third", target1.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void this_Operator_Binds_One_Time()
        {
            Class1 target1 = new Class1();
            Class1 target2 = new Class1();

            target1.SetValue(Class1.FooProperty, "first");
            target2[!Class1.FooProperty] = target1[Class1.FooProperty.Bind().WithMode(BindingMode.OneTime)];
            target1.SetValue(Class1.FooProperty, "second");

            Assert.Equal("first", target2.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Binding_Error_Reverts_To_Default_Value()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<string>>();

            target.Bind(Class1.FooProperty, source);
            source.OnNext("initial");
            source.OnNext(BindingValue<string>.BindingError(new InvalidOperationException("Foo")));

            Assert.Equal("foodefault", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Binding_Error_With_FallbackValue_Causes_Target_Update()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<string>>();

            target.Bind(Class1.FooProperty, source);
            source.OnNext("initial");
            source.OnNext(BindingValue<string>.BindingError(new InvalidOperationException("Foo"), "bar"));

            Assert.Equal("bar", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void DataValidationError_Does_Not_Cause_Target_Update()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<string>>();

            target.Bind(Class1.FooProperty, source);
            source.OnNext("initial");
            source.OnNext(BindingValue<string>.DataValidationError(new InvalidOperationException("Foo")));

            Assert.Equal("initial", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void DataValidationError_With_FallbackValue_Causes_Target_Update()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<string>>();

            target.Bind(Class1.FooProperty, source);
            source.OnNext("initial");
            source.OnNext(BindingValue<string>.DataValidationError(new InvalidOperationException("Foo"), "bar"));

            Assert.Equal("bar", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Bind_Logs_Binding_Error()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<double>>();
            var called = false;
            var expectedMessageTemplate = "Error in binding to {Target}.{Property}: {Message}";

            LogCallback checkLogMessage = (level, area, src, mt, pv) =>
            {
                if (level == LogEventLevel.Warning &&
                    area == LogArea.Binding &&
                    mt == expectedMessageTemplate)
                {
                    called = true;
                }
            };

            using (TestLogSink.Start(checkLogMessage))
            {
                target.Bind(Class1.QuxProperty, source);
                source.OnNext(6.7);
                source.OnNext(BindingValue<double>.BindingError(new InvalidOperationException("Foo")));

                Assert.Equal(5.6, target.GetValue(Class1.QuxProperty));
                Assert.True(called);
            }
        }

        [Fact]
        public async Task Bind_With_Scheduler_Executes_On_Scheduler()
        {
            var target = new Class1();
            var source = new Subject<double>();
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            var threadingInterfaceMock = new Mock<IPlatformThreadingInterface>();
            threadingInterfaceMock.SetupGet(mock => mock.CurrentThreadIsLoopThread)
                .Returns(() => Thread.CurrentThread.ManagedThreadId == currentThreadId);

            var services = new TestServices(
                scheduler: AvaloniaScheduler.Instance,
                threadingInterface: threadingInterfaceMock.Object);

            using (UnitTestApplication.Start(services))
            {
                target.Bind(Class1.QuxProperty, source);

                await Task.Run(() => source.OnNext(6.7));
            }
        }

        [Fact]
        public void SetValue_Should_Not_Cause_StackOverflow_And_Have_Correct_Values()
        {
            var viewModel = new TestStackOverflowViewModel()
            {
                Value = 50
            };

            var target = new Class1();

            target.Bind(Class1.DoubleValueProperty,
                new Binding("Value") { Mode = BindingMode.TwoWay, Source = viewModel });

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

        [Fact]
        public void IsAnimating_On_Property_With_No_Value_Returns_False()
        {
            var target = new Class1();

            Assert.False(target.IsAnimating(Class1.FooProperty));
        }

        [Fact]
        public void IsAnimating_On_Property_With_Animation_Value_Returns_True()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "foo", BindingPriority.Animation);

            Assert.True(target.IsAnimating(Class1.FooProperty));
        }

        [Fact]
        public void IsAnimating_On_Property_With_Non_Animation_Binding_Returns_False()
        {
            var target = new Class1();
            var source = new Subject<string>();

            target.Bind(Class1.FooProperty, source, BindingPriority.LocalValue);

            Assert.False(target.IsAnimating(Class1.FooProperty));
        }

        [Fact]
        public void IsAnimating_On_Property_With_Animation_Binding_Returns_True()
        {
            var target = new Class1();
            var source = new BehaviorSubject<string>("foo");

            target.Bind(Class1.FooProperty, source, BindingPriority.Animation);

            Assert.True(target.IsAnimating(Class1.FooProperty));
        }

        [Fact]
        public void IsAnimating_On_Property_With_Local_Value_And_Animation_Binding_Returns_True()
        {
            var target = new Class1();
            var source = new BehaviorSubject<string>("foo");

            target.SetValue(Class1.FooProperty, "bar");
            target.Bind(Class1.FooProperty, source, BindingPriority.Animation);

            Assert.True(target.IsAnimating(Class1.FooProperty));
        }

        [Fact]
        public void IsAnimating_Returns_True_When_Animated_Value_Is_Same_As_Local_Value()
        {
            var target = new Class1();
            var source = new BehaviorSubject<string>("foo");

            target.SetValue(Class1.FooProperty, "foo");
            target.Bind(Class1.FooProperty, source, BindingPriority.Animation);

            Assert.True(target.IsAnimating(Class1.FooProperty));
        }

        [Fact]
        public void TwoWay_Binding_Should_Not_Call_Setter_On_Creation()
        {
            var target = new Class1();
            var source = new TestTwoWayBindingViewModel();

            target.Bind(Class1.DoubleValueProperty, new Binding(nameof(source.Value), BindingMode.TwoWay) { Source = source });

            Assert.False(source.ValueSetterCalled);
        }

        [Fact]
        public void TwoWay_Binding_Should_Not_Call_Setter_On_Creation_Indexer()
        {
            var target = new Class1();
            var source = new TestTwoWayBindingViewModel();

            target.Bind(Class1.DoubleValueProperty, new Binding("[0]", BindingMode.TwoWay) { Source = source });

            Assert.False(source.ValueSetterCalled);
        }

        [Fact]
        public void TwoWay_Binding_Should_Not_Fail_With_Null_DataContext()
        {
            var target = new TextBlock();
            target.DataContext = null;

            target.Bind(TextBlock.TextProperty, new Binding("Missing", BindingMode.TwoWay));
        }

        [Fact]
        public void TwoWay_Binding_Should_Not_Fail_With_Null_DataContext_Indexer()
        {
            var target = new TextBlock();
            target.DataContext = null;

            target.Bind(TextBlock.TextProperty, new Binding("[0]", BindingMode.TwoWay));
        }

        [Fact]
        public void Disposing_Completed_Binding_Does_Not_Throw()
        {
            var target = new Class1();
            var source = new Subject<string>();
            var subscription = target.Bind(Class1.FooProperty, source);

            source.OnCompleted();

            subscription.Dispose();
        }

        [Fact]
        public void TwoWay_Binding_Should_Not_Call_Setter_On_Creation_With_Value()
        {
            var target = new Class1();
            var source = new TestTwoWayBindingViewModel() { Value = 1 };
            source.ResetSetterCalled();

            target.Bind(Class1.DoubleValueProperty, new Binding(nameof(source.Value), BindingMode.TwoWay) { Source = source });

            Assert.False(source.ValueSetterCalled);
        }

        [Fact]
        public void TwoWay_Binding_Should_Not_Call_Setter_On_Creation_Indexer_With_Value()
        {
            var target = new Class1();
            var source = new TestTwoWayBindingViewModel() { [0] = 1 };
            source.ResetSetterCalled();

            target.Bind(Class1.DoubleValueProperty, new Binding("[0]", BindingMode.TwoWay) { Source = source });

            Assert.False(source.ValueSetterCalled);
        }


        [Fact]
        public void Disposing_a_TwoWay_Binding_Should_Set_Default_Value_On_Binding_Target_But_Not_On_Source()
        {
            var target = new Class3();

            // Create a source class which has a Value set to -1 and a Minimum set to -2
            var source = new TestTwoWayBindingViewModel() { Value = -1, Minimum = -2 };

            // Reset the setter counter
            source.ResetSetterCalled();

            // 1. bind the minimum
            var disposable_1 = target.Bind(Class3.MinimumProperty, new Binding("Minimum", BindingMode.TwoWay) { Source = source });
            // 2. Bind the value
            var disposable_2 = target.Bind(Class3.ValueProperty, new Binding("Value", BindingMode.TwoWay) { Source = source });

            // Dispose the minimum binding
            disposable_1.Dispose();
            // Dispose the value binding
            disposable_2.Dispose();


            // The value setter should be called here as we have disposed minimum fist and the default value of minimum is 0, so this should be changed.
            Assert.True(source.ValueSetterCalled);
            // The minimum value should not be changed in the source.
            Assert.False(source.MinimumSetterCalled);
        }

        /// <summary>
        /// Returns an observable that returns a single value but does not complete.
        /// </summary>
        /// <typeparam name="T">The type of the observable.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The observable.</returns>
        private static IObservable<T> Single<T>(T value)
        {
            return Observable.Never<T>().StartWith(value);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", "foodefault");

            public static readonly StyledProperty<double> QuxProperty =
                AvaloniaProperty.Register<Class1, double>("Qux", 5.6);

            public static readonly StyledProperty<double> DoubleValueProperty =
                        AvaloniaProperty.Register<Class1, double>(nameof(DoubleValue));

            public double DoubleValue
            {
                get { return GetValue(DoubleValueProperty); }
                set { SetValue(DoubleValueProperty, value); }
            }
        }

        private class Class2 : Class1
        {
            public static readonly StyledProperty<string> BarProperty =
                AvaloniaProperty.Register<Class2, string>("Bar", "bardefault");
        }

        private class Class3 : AvaloniaObject 
        {
            static Class3()
            {
                MinimumProperty.Changed.Subscribe(x => OnMinimumChanged(x));
            }

            private static void OnMinimumChanged(AvaloniaPropertyChangedEventArgs<double> e)
            {
                if (e.Sender is Class3 s)
                {
                    s.SetValue(ValueProperty, MathUtilities.Clamp(s.Value, e.NewValue.Value, double.PositiveInfinity));
                }
            }

            /// <summary>
            /// Defines the <see cref="Value"/> property.
            /// </summary>
            public static readonly StyledProperty<double> ValueProperty =
                AvaloniaProperty.Register<Class3, double>(nameof(Value), 0);

            /// <summary>
            /// Gets or sets the Value property
            /// </summary>
            public double Value
            {
                get { return GetValue(ValueProperty); }
                set { SetValue(ValueProperty, value); }
            }


            /// <summary>
            /// Defines the <see cref="Minimum"/> property.
            /// </summary>
            public static readonly StyledProperty<double> MinimumProperty =
                AvaloniaProperty.Register<Class3, double>(nameof(Minimum), 0);

            /// <summary>
            /// Gets or sets the minimum property
            /// </summary>
            public double Minimum
            {
                get { return GetValue(MinimumProperty); }
                set { SetValue(MinimumProperty, value); }
            }


        }


        private class TestOneTimeBinding : IBinding
        {
            private IObservable<object> _source;

            public TestOneTimeBinding(IObservable<object> source)
            {
                _source = source;
            }

            public InstancedBinding Initiate(
                IAvaloniaObject target,
                AvaloniaProperty targetProperty,
                object anchor = null,
                bool enableDataValidation = false)
            {
                return InstancedBinding.OneTime(_source);
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

        private class TestTwoWayBindingViewModel
        {
            private double _value;

            public double Value
            {
                get => _value;
                set
                {
                    _value = value;
                    ValueSetterCalled = true;
                }
            }

            private double _minimum;
            public double Minimum
            {
                get => _minimum;
                set
                {
                    _minimum = value;
                    MinimumSetterCalled = true;
                }
            }

            public double this[int index]
            {
                get => _value;
                set
                {
                    _value = value;
                    ValueSetterCalled = true;
                }
            }

            public bool ValueSetterCalled { get; private set; }
            public bool MinimumSetterCalled { get; private set; }

            public void ResetSetterCalled()
            {
                ValueSetterCalled = false;
                MinimumSetterCalled = false;
            }
        }
    }
}
