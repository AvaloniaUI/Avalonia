using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Base.UnitTests.Styling;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Nito.AsyncEx;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Binding
    {
        [Fact]
        public void Bind_Sets_Current_Value()
        {
            var target = new Class1();
            var source = new BehaviorSubject<BindingValue<string>>("initial");
            var property = Class1.FooProperty;

            target.Bind(property, source);

            Assert.Equal("initial", target.GetValue(property));
        }

        [Fact]
        public void Bind_Raises_PropertyChanged()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<string>>();
            var raised = 0;

            target.PropertyChanged += (s, e) =>
            {
                Assert.Equal(Class1.FooProperty, e.Property);
                Assert.Equal("foodefault", (string?)e.OldValue);
                Assert.Equal("newvalue", (string?)e.NewValue);
                Assert.Equal(BindingPriority.LocalValue, e.Priority);
                ++raised;
            };

            target.Bind(Class1.FooProperty, source);
            source.OnNext("newvalue");

            Assert.Equal(1, raised);
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
            var source = new Subject<BindingValue<string>>();
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
            var source = new Subject<BindingValue<string>>();
            var property = Class1.FooProperty;

            target.Bind(property, source);
            source.OnNext("foo");
            target.SetValue(property, "bar");
            source.OnNext("baz");
            source.OnCompleted();

            Assert.Equal("foodefault", target.GetValue(property));
        }

        [Fact]
        public void Disposing_LocalValue_Binding_Should_Not_Revert_To_Set_LocalValue()
        {
            var target = new Class1();
            var source = new BehaviorSubject<BindingValue<string>>("bar");

            target.SetValue(Class1.FooProperty, "foo");
            var sub = target.Bind(Class1.FooProperty, source);

            Assert.Equal("bar", target.GetValue(Class1.FooProperty));

            sub.Dispose();

            Assert.Equal("foodefault", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void LocalValue_Binding_Should_Override_Style_Binding()
        {
            var target = new Class1();
            var source1 = new BehaviorSubject<BindingValue<string>>("foo");
            var source2 = new BehaviorSubject<BindingValue<string>>("bar");

            target.Bind(Class1.FooProperty, source1, BindingPriority.Style);

            Assert.Equal("foo", target.GetValue(Class1.FooProperty));

            target.Bind(Class1.FooProperty, source2, BindingPriority.LocalValue);

            Assert.Equal("bar", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Style_Binding_Should_NotOverride_LocalValue_Binding()
        {
            var target = new Class1();
            var source1 = new BehaviorSubject<BindingValue<string>>("foo");
            var source2 = new BehaviorSubject<BindingValue<string>>("bar");

            target.Bind(Class1.FooProperty, source1, BindingPriority.LocalValue);

            Assert.Equal("foo", target.GetValue(Class1.FooProperty));

            target.Bind(Class1.FooProperty, source2, BindingPriority.Style);

            Assert.Equal("foo", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Completing_Animation_Binding_Reverts_To_Set_LocalValue()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<string>>();
            var property = Class1.FooProperty;

            target.SetValue(property, "foo");
            target.Bind(property, source, BindingPriority.Animation);
            source.OnNext("bar");
            source.OnCompleted();

            Assert.Equal("foo", target.GetValue(property));
        }

        [Fact]
        public void Completing_Animation_Binding_Reverts_To_Set_LocalValue_With_Style_Value()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<string>>();
            var property = Class1.FooProperty;

            target.SetValue(property, "style", BindingPriority.Style);
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

            target.Bind(property, new BehaviorSubject<BindingValue<string>>("bar"), BindingPriority.Style);
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
        public void Second_LocalValue_Binding_Unsubscribes_First()
        {
            var property = Class1.FooProperty;
            var target = new Class1();
            var source1 = new Subject<BindingValue<string>>();
            var source2 = new Subject<BindingValue<string>>();

            target.Bind(property, source1, BindingPriority.LocalValue);
            target.Bind(property, source2, BindingPriority.LocalValue);

            source1.OnNext("foo");
            Assert.Equal("foodefault", target.GetValue(property));

            source2.OnNext("bar");
            Assert.Equal("bar", target.GetValue(property));

            source1.OnNext("baz");
            Assert.Equal("bar", target.GetValue(property));
        }

        [Fact]
        public void Completing_Second_LocalValue_Binding_Doesnt_Revert_To_First()
        {
            var property = Class1.FooProperty;
            var target = new Class1();
            var source1 = new Subject<BindingValue<string>>();
            var source2 = new Subject<BindingValue<string>>();

            target.Bind(property, source1, BindingPriority.LocalValue);
            target.Bind(property, source2, BindingPriority.LocalValue);

            source1.OnNext("foo");
            source2.OnNext("bar");
            source1.OnNext("baz");
            source2.OnCompleted();

            Assert.Equal("foodefault", target.GetValue(property));
        }

        [Fact]
        public void Completing_StyleTrigger_Binding_Reverts_To_StyleBinding()
        {
            var property = Class1.FooProperty;
            var target = new Class1();
            var source1 = new Subject<BindingValue<string>>();
            var source2 = new Subject<BindingValue<string>>();

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
        public void Bind_NonGeneric_Can_Set_Null_On_Reference_Type()
        {
            var target = new Class1();
            var source = new BehaviorSubject<object?>(null);
            var property = Class1.FooProperty;

            target.Bind(property, source);

            Assert.Null(target.GetValue(property));
        }

        [Fact]
        public void LocalValue_Bind_Generic_To_ValueType_Accepts_UnsetValue()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<double>>();

            target.Bind(Class1.QuxProperty, source);
            source.OnNext(6.7);
            source.OnNext(BindingValue<double>.Unset);

            Assert.Equal(5.6, target.GetValue(Class1.QuxProperty));
            Assert.True(target.IsSet(Class1.QuxProperty));
        }

        [Fact]
        public void LocalValue_Bind_NonGeneric_To_ValueType_Accepts_UnsetValue()
        {
            var target = new Class1();
            var source = new Subject<object>();

            target.Bind(Class1.QuxProperty, source);
            source.OnNext(6.7);
            source.OnNext(AvaloniaProperty.UnsetValue);

            Assert.Equal(5.6, target.GetValue(Class1.QuxProperty));
            Assert.True(target.IsSet(Class1.QuxProperty));
        }

        [Fact]
        public void Style_Bind_NonGeneric_To_ValueType_Accepts_UnsetValue()
        {
            var target = new Class1();
            var source = new Subject<object>();

            target.Bind(Class1.QuxProperty, source, BindingPriority.Style);
            source.OnNext(6.7);
            source.OnNext(AvaloniaProperty.UnsetValue);

            Assert.Equal(5.6, target.GetValue(Class1.QuxProperty));
            Assert.True(target.IsSet(Class1.QuxProperty));
        }

        [Fact]
        public void LocalValue_Bind_NonGeneric_To_ValueType_Accepts_DoNothing()
        {
            var target = new Class1();
            var source = new Subject<object>();

            target.Bind(Class1.QuxProperty, source);
            source.OnNext(6.7);
            source.OnNext(BindingOperations.DoNothing);

            Assert.Equal(6.7, target.GetValue(Class1.QuxProperty));
        }

        [Fact]
        public void Style_Bind_NonGeneric_To_ValueType_Accepts_DoNothing()
        {
            var target = new Class1();
            var source = new Subject<object>();

            target.Bind(Class1.QuxProperty, source, BindingPriority.Style);
            source.OnNext(6.7);
            source.OnNext(BindingOperations.DoNothing);

            Assert.Equal(6.7, target.GetValue(Class1.QuxProperty));
        }

        [Fact]
        public void OneTime_Binding_Ignores_UnsetValue()
        {
            var target = new Class1();
            var source = new Subject<object>();

            target.Bind(Class1.QuxProperty, source);

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

            target.Bind(Class1.QuxProperty, source);

            source.OnNext(new BindingNotification(new Exception(), BindingErrorType.Error));
            Assert.Equal(5.6, target.GetValue(Class1.QuxProperty));

            source.OnNext(6.7);
            Assert.Equal(6.7, target.GetValue(Class1.QuxProperty));
        }

        [Fact]
        public void Bind_Does_Not_Throw_Exception_For_Unregistered_Property()
        {
            Class1 target = new Class1();

            target.Bind(Class2.BarProperty, Observable.Never<BindingValue<string>>().StartWith("foo"));

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
            var source = new TestSubject<BindingValue<string>>("foo");
            var target = new Class1();

            var subscription = target.Bind(Class1.FooProperty, source);
            Assert.Equal(1, source.SubscriberCount);

            subscription.Dispose();
            Assert.Equal(0, source.SubscriberCount);
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        [InlineData(BindingPriority.Animation)]
        public void Observable_Is_Unsubscribed_When_New_Binding_Of_Same_Priority_Is_Added(BindingPriority priority)
        {
            var source1 = new TestSubject<BindingValue<string>>("foo");
            var source2 = new TestSubject<BindingValue<string>>("bar");
            var target = new Class1();

            target.Bind(Class1.FooProperty, source1, priority);
            Assert.Equal(1, source1.SubscriberCount);

            target.Bind(Class1.FooProperty, source2, priority);
            Assert.Equal(1, source2.SubscriberCount);
            Assert.Equal(0, source1.SubscriberCount);
        }

        [Theory]
        [InlineData(BindingPriority.Style)]
        public void Observable_Is_Unsubscribed_When_New_Binding_Of_Higher_Priority_Is_Added(BindingPriority priority)
        {
            var source1 = new TestSubject<BindingValue<string>>("foo");
            var source2 = new TestSubject<BindingValue<string>>("bar");
            var target = new Class1();

            target.Bind(Class1.FooProperty, source1, priority);
            Assert.Equal(1, source1.SubscriberCount);

            target.Bind(Class1.FooProperty, source2, priority - 1);
            Assert.Equal(1, source2.SubscriberCount);
            Assert.Equal(0, source1.SubscriberCount);
        }

        [Theory]
        [InlineData(BindingPriority.Style)]
        [InlineData(BindingPriority.Animation)]
        public void Observable_Is_Unsubscribed_When_New_Value_Of_Same_Priority_Is_Added(BindingPriority priority)
        {
            var source = new TestSubject<BindingValue<string>>("foo");
            var target = new Class1();

            target.Bind(Class1.FooProperty, source, priority);
            Assert.Equal(1, source.SubscriberCount);

            target.SetValue(Class1.FooProperty, "foo", priority);
            Assert.Equal(0, source.SubscriberCount);
        }

        [Theory]
        [InlineData(BindingPriority.Style)]
        public void Observable_Is_Unsubscribed_When_New_Value_Of_Higher_Priority_Is_Added(BindingPriority priority)
        {
            var source = new TestSubject<BindingValue<string>>("foo");
            var target = new Class1();

            target.Bind(Class1.FooProperty, source, priority);
            Assert.Equal(1, source.SubscriberCount);

            target.SetValue(Class1.FooProperty, "foo", priority - 1);
            Assert.Equal(0, source.SubscriberCount);
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        public void Observable_Is_Not_Unsubscribed_When_Animation_Value_Is_Set(BindingPriority priority)
        {
            var source = new TestSubject<BindingValue<string>>("foo");
            var target = new Class1();

            target.Bind(Class1.FooProperty, source, priority);
            Assert.Equal(1, source.SubscriberCount);

            target.SetValue(Class1.FooProperty, "bar", BindingPriority.Animation);
            Assert.Equal(1, source.SubscriberCount);
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        public void Observable_Is_Not_Unsubscribed_When_Animation_Binding_Is_Added(BindingPriority priority)
        {
            var source1 = new TestSubject<BindingValue<string>>("foo");
            var source2 = new TestSubject<BindingValue<string>>("bar");
            var target = new Class1();

            target.Bind(Class1.FooProperty, source1, priority);
            Assert.Equal(1, source1.SubscriberCount);

            target.Bind(Class1.FooProperty, source2, BindingPriority.Animation);
            Assert.Equal(1, source1.SubscriberCount);
            Assert.Equal(1, source2.SubscriberCount);
        }

        [Fact]
        public void LocalValue_Binding_Is_Not_Unsubscribed_When_LocalValue_Is_Set()
        {
            var source = new TestSubject<BindingValue<string>>("foo");
            var target = new Class1();

            target.Bind(Class1.FooProperty, source);
            Assert.Equal(1, source.SubscriberCount);

            target.SetValue(Class1.FooProperty, "foo");
            Assert.Equal(1, source.SubscriberCount);
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
            var binding = new Subject<BindingValue<string>>();

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

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        public void Typed_Bind_Executes_On_UIThread(BindingPriority priority)
        {
            AsyncContext.Run(async () =>
            {
                var target = new Class1();
                var source = new Subject<string>();
                var currentThreadId = Thread.CurrentThread.ManagedThreadId;
                var raised = 0;

                var dispatcherMock = new Mock<IDispatcherImpl>();
                dispatcherMock.SetupGet(mock => mock.CurrentThreadIsLoopThread)
                    .Returns(() => Thread.CurrentThread.ManagedThreadId == currentThreadId);

                var services = new TestServices(
                    dispatcherImpl: dispatcherMock.Object);

                target.PropertyChanged += (s, e) =>
                {
                    Assert.Equal(currentThreadId, Thread.CurrentThread.ManagedThreadId);
                    ++raised;
                };

                using (UnitTestApplication.Start(services))
                {
                    target.Bind(Class1.FooProperty, source, priority);

                    await Task.Run(() => source.OnNext("foobar"));
                    Dispatcher.UIThread.RunJobs();

                    Assert.Equal("foobar", target.GetValue(Class1.FooProperty));
                    Assert.Equal(1, raised);
                }
            });
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        public void Untyped_Bind_Executes_On_UIThread(BindingPriority priority)
        {
            AsyncContext.Run(async () =>
            {
                var target = new Class1();
                var source = new Subject<object>();
                var currentThreadId = Thread.CurrentThread.ManagedThreadId;
                var raised = 0;

                var dispatcherMock = new Mock<IDispatcherImpl>();
                dispatcherMock.SetupGet(mock => mock.CurrentThreadIsLoopThread)
                    .Returns(() => Thread.CurrentThread.ManagedThreadId == currentThreadId);

                var services = new TestServices(
                    dispatcherImpl: dispatcherMock.Object);

                target.PropertyChanged += (s, e) =>
                {
                    Assert.Equal(currentThreadId, Thread.CurrentThread.ManagedThreadId);
                    ++raised;
                };

                using (UnitTestApplication.Start(services))
                {
                    target.Bind(Class1.FooProperty, source, priority);

                    await Task.Run(() => source.OnNext("foobar"));
                    Dispatcher.UIThread.RunJobs();

                    Assert.Equal("foobar", target.GetValue(Class1.FooProperty));
                    Assert.Equal(1, raised);
                }
            });
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        public void BindingValue_Bind_Executes_On_UIThread(BindingPriority priority)
        {
            AsyncContext.Run(async () =>
            {
                var target = new Class1();
                var source = new Subject<BindingValue<string>>();
                var currentThreadId = Thread.CurrentThread.ManagedThreadId;
                var raised = 0;

                var threadingInterfaceMock = new Mock<IDispatcherImpl>();
                threadingInterfaceMock.SetupGet(mock => mock.CurrentThreadIsLoopThread)
                    .Returns(() => Thread.CurrentThread.ManagedThreadId == currentThreadId);

                var services = new TestServices(
                    dispatcherImpl: threadingInterfaceMock.Object);

                target.PropertyChanged += (s, e) =>
                {
                    Assert.Equal(currentThreadId, Thread.CurrentThread.ManagedThreadId);
                    ++raised;
                };

                using (UnitTestApplication.Start(services))
                {
                    target.Bind(Class1.FooProperty, source, priority);

                    await Task.Run(() => source.OnNext("foobar"));
                    Dispatcher.UIThread.RunJobs();

                    Assert.Equal("foobar", target.GetValue(Class1.FooProperty));
                    Assert.Equal(1, raised);
                }
            });
        }

        [Fact]
        public async Task Bind_With_Scheduler_Executes_On_UI_Thread()
        {
            var target = new Class1();
            var source = new Subject<double>();
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            var threadingInterfaceMock = new Mock<IDispatcherImpl>();
            threadingInterfaceMock.SetupGet(mock => mock.CurrentThreadIsLoopThread)
                .Returns(() => Thread.CurrentThread.ManagedThreadId == currentThreadId);

            var services = new TestServices(
                dispatcherImpl: threadingInterfaceMock.Object);

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
            var source = new BehaviorSubject<BindingValue<string>>("foo");

            target.Bind(Class1.FooProperty, source, BindingPriority.Animation);

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
        public void TwoWay_Binding_Should_Update_Source()
        {
            var target = new Class1();
            var source = new TestTwoWayBindingViewModel();

            target.Bind(Class1.DoubleValueProperty, new Binding(nameof(source.Value), BindingMode.TwoWay) { Source = source });

            target.DoubleValue = 123.4;

            Assert.True(source.SetterCalled);
            Assert.Equal(source.Value, 123.4);
        }

        [Fact]
        public void TwoWay_Binding_Should_Not_Call_Setter_On_Creation()
        {
            var target = new Class1();
            var source = new TestTwoWayBindingViewModel();

            target.Bind(Class1.DoubleValueProperty, new Binding(nameof(source.Value), BindingMode.TwoWay) { Source = source });

            Assert.False(source.SetterCalled);
        }

        [Fact]
        public void TwoWay_Binding_Should_Not_Call_Setter_On_Creation_Indexer()
        {
            var target = new Class1();
            var source = new TestTwoWayBindingViewModel();

            target.Bind(Class1.DoubleValueProperty, new Binding("[0]", BindingMode.TwoWay) { Source = source });

            Assert.False(source.SetterCalled);
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

        [Theory(Skip = "Will need changes to binding internals in order to pass")]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.StyleTrigger)]
        [InlineData(BindingPriority.Style)]
        public void TwoWay_Binding_Should_Not_Update_Source_When_Higher_Priority_Value_Set(BindingPriority priority)
        {
            var target = new Class1();
            var source = new TestTwoWayBindingViewModel();
            var binding = new Binding(nameof(source.Value), BindingMode.TwoWay) { Source = source };

            target.Bind(Class1.DoubleValueProperty, binding, priority);
            target.SetValue(Class1.DoubleValueProperty, 123.4, priority - 1);

            // Setter should not be called because the TwoWay binding with LocalValue priority
            // should be overridden by the animated value and the binding made inactive.
            Assert.False(source.SetterCalled);
        }

        [Theory(Skip = "Will need changes to binding internals in order to pass")]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.StyleTrigger)]
        [InlineData(BindingPriority.Style)]
        public void TwoWay_Binding_Should_Not_Update_Source_When_Higher_Priority_Binding_Added(BindingPriority priority)
        {
            var target = new Class1();
            var source = new TestTwoWayBindingViewModel();
            var binding1 = new Binding(nameof(source.Value), BindingMode.TwoWay) { Source = source };
            var binding2 = new BehaviorSubject<double>(123.4);
            
            target.Bind(Class1.DoubleValueProperty, binding1, priority);
            target.Bind(Class1.DoubleValueProperty, binding2, priority - 1);

            // Setter should not be called because the TwoWay binding with LocalValue priority
            // should be overridden by the animated binding and the binding made inactive.
            Assert.False(source.SetterCalled);
        }

        [Fact(Skip = "Will need changes to binding internals in order to pass")]
        public void TwoWay_Style_Binding_Should_Not_Update_Source_When_StyleTrigger_Value_Set()
        {
            var target = new Class1();
            var source = new TestTwoWayBindingViewModel();

            target.Bind(Class1.DoubleValueProperty, new Binding(nameof(source.Value), BindingMode.TwoWay) { Source = source });
            target.SetValue(Class1.DoubleValueProperty, 123.4, BindingPriority.Animation);

            // Setter should not be called because the TwoWay binding with Style priority
            // should be overridden by the animated value and the binding made inactive.
            Assert.False(source.SetterCalled);
        }

        [Fact(Skip = "Will need changes to binding internals in order to pass")]
        public void TwoWay_Style_Binding_Should_Not_Update_Source_When_Animated_Binding_Added()
        {
            var target = new Class1();
            var source1 = new TestTwoWayBindingViewModel();
            var source2 = new BehaviorSubject<double>(123.4);

            target.Bind(Class1.DoubleValueProperty, new Binding(nameof(source1.Value), BindingMode.TwoWay) { Source = source1 });
            target.Bind(Class1.DoubleValueProperty, source2, BindingPriority.Animation);

            // Setter should not be called because the TwoWay binding with Style priority
            // should be overridden by the animated binding and the binding made inactive.
            Assert.False(source1.SetterCalled);
        }

        [Fact]
        public void Disposing_Completed_Binding_Does_Not_Throw()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<string>>();
            var subscription = target.Bind(Class1.FooProperty, source);

            source.OnCompleted();

            subscription.Dispose();
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        public void Binding_Producing_UnsetValue_Does_Not_Cause_Unsubscribe(BindingPriority priority)
        {
            var target = new Class1();
            var source = new Subject<BindingValue<string>>();
            
            target.Bind(Class1.FooProperty, source, priority);

            source.OnNext("foo");
            Assert.Equal("foo", target.GetValue(Class1.FooProperty));
            source.OnNext(BindingValue<string>.Unset);
            Assert.Equal("foodefault", target.GetValue(Class1.FooProperty));
            source.OnNext("bar");
            Assert.Equal("bar", target.GetValue(Class1.FooProperty));
        }

        [Fact]
        public void Produces_Correct_Values_And_Base_Values_With_Multiple_Animation_Bindings()
        {
            var target = new Class1();
            var source1 = new BehaviorSubject<BindingValue<double>>(12.2);
            var source2 = new BehaviorSubject<BindingValue<double>>(13.3);

            target.SetValue(Class1.QuxProperty, 11.1);
            target.Bind(Class1.QuxProperty, source1, BindingPriority.Animation);

            Assert.Equal(12.2, target.GetValue(Class1.QuxProperty));
            Assert.Equal(11.1, target.GetBaseValue(Class1.QuxProperty));

            target.Bind(Class1.QuxProperty, source2, BindingPriority.Animation);

            Assert.Equal(13.3, target.GetValue(Class1.QuxProperty));
            Assert.Equal(11.1, target.GetBaseValue(Class1.QuxProperty));

            source2.OnCompleted();

            Assert.Equal(12.2, target.GetValue(Class1.QuxProperty));
            Assert.Equal(11.1, target.GetBaseValue(Class1.QuxProperty));

            source1.OnCompleted();

            Assert.Equal(11.1, target.GetValue(Class1.QuxProperty));
            Assert.Equal(11.1, target.GetBaseValue(Class1.QuxProperty));
        }

        /// <summary>
        /// Returns an observable that returns a single value but does not complete.
        /// </summary>
        /// <typeparam name="T">The type of the observable.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The observable.</returns>
        private IObservable<BindingValue<T>> Single<T>(T value)
        {
            return Observable.Never<BindingValue<T>>().StartWith(value);
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

        private class TestOneTimeBinding : IBinding
        {
            private IObservable<object> _source;

            public TestOneTimeBinding(IObservable<object> source)
            {
                _source = source;
            }

            public InstancedBinding Initiate(
                AvaloniaObject target,
                AvaloniaProperty? targetProperty,
                object? anchor = null,
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

            public event PropertyChangedEventHandler? PropertyChanged;

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
                            if (_value > 75)
                                _value = 75;
                            if (_value < 25)
                                _value = 25;
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
                    SetterCalled = true;
                }
            }

            public double this[int index]
            {
                get => _value;
                set
                {
                    _value = value;
                    SetterCalled = true;
                }
            }

            public bool SetterCalled { get; private set; }
        }
    }
}
