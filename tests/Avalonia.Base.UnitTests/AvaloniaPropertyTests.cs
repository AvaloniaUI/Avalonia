using System;
using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.PropertyStore;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaPropertyTests
    {
        [Fact]
        public void Constructor_Sets_Properties()
        {
            var target = new TestProperty<string>("test", typeof(Class1));

            Assert.Equal("test", target.Name);
            Assert.Equal(typeof(string), target.PropertyType);
            Assert.Equal(typeof(Class1), target.OwnerType);
        }

        [Fact]
        public void Name_Cannot_Contain_Periods()
        {
            Assert.Throws<ArgumentException>(() => new TestProperty<string>("Foo.Bar", typeof(Class1)));
        }

        [Fact]
        public void GetMetadata_Returns_Supplied_Value()
        {
            var metadata = new TestMetadata();
            var target = new TestProperty<string>("test", typeof(Class1), metadata);

            Assert.Same(metadata, target.GetMetadata<Class1>());
        }

        [Fact]
        public void GetMetadata_Returns_Supplied_Value_For_Derived_Class()
        {
            var metadata = new TestMetadata();
            var target = new TestProperty<string>("test", typeof(Class1), metadata);

            Assert.Same(metadata, target.GetMetadata<Class2>());
        }

        [Fact]
        public void GetMetadata_Returns_TypeSafe_Metadata_For_Unrelated_Class()
        {
            var metadata = new TestMetadata(BindingMode.OneWayToSource, true, x => { _ = (StyledElement)x; });
            var target = new TestProperty<string>("test", typeof(Class3), metadata);

            var targetMetadata = (TestMetadata)target.GetMetadata<Class2>();

            Assert.Equal(metadata.DefaultBindingMode, targetMetadata.DefaultBindingMode);
            Assert.Equal(metadata.EnableDataValidation, targetMetadata.EnableDataValidation);
            Assert.Equal(null, targetMetadata.OwnerSpecificAction);
        }

        [Fact]
        public void GetMetadata_Returns_Overridden_Value()
        {
            var metadata = new TestMetadata();
            var overridden = new TestMetadata();
            var target = new TestProperty<string>("test", typeof(Class1), metadata);

            target.OverrideMetadata<Class2>(overridden);

            Assert.Same(overridden, target.GetMetadata<Class2>());
        }

        [Fact]
        public void OverrideMetadata_Should_Merge_Values()
        {
            var metadata = new TestMetadata(BindingMode.TwoWay);
            var notify = (Action<AvaloniaObject, bool>)((a, b) => { });
            var overridden = new TestMetadata();
            var target = new TestProperty<string>("test", typeof(Class1), metadata);

            target.OverrideMetadata<Class2>(overridden);

            var result = target.GetMetadata<Class2>();
            Assert.Equal(BindingMode.TwoWay, result.DefaultBindingMode);
        }

        [Fact]
        public void Changed_Observable_Fired()
        {
            var target = new Class1();
            string value = null;

            Class1.FooProperty.Changed.Subscribe(x => value = x.NewValue.GetValueOrDefault());
            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", value);
        }

        [Fact]
        public void Changed_Observable_Fired_Only_On_Effective_Value_Change()
        {
            var target = new Class1();
            var result = new List<string>();

            Class1.FooProperty.Changed.Subscribe(x => result.Add(x.NewValue.GetValueOrDefault()));
            target.SetValue(Class1.FooProperty, "animated", BindingPriority.Animation);
            target.SetValue(Class1.FooProperty, "local");

            Assert.Equal(new[] { "animated" }, result);
        }

        [Fact]
        public void Notify_Fired_Only_On_Effective_Value_Change()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "animated", BindingPriority.Animation);
            target.SetValue(Class1.FooProperty, "local");

            Assert.Equal(2, target.NotifyCount);
        }

        [Fact]
        public void Property_Equals_Should_Handle_Null()
        {
            var p1 = new TestProperty<string>("p1", typeof(Class1));

            Assert.NotNull(p1);
            Assert.NotNull(p1);
            Assert.False(p1 == null);
            Assert.False(null == p1);
            Assert.False(p1.Equals(null));
            Assert.True((AvaloniaProperty)null == (AvaloniaProperty)null);
        }

        [Fact]
        public void PropertyMetadata_BindingMode_Default_Returns_OneWay()
        {
            var data = new TestMetadata(defaultBindingMode: BindingMode.Default);

            Assert.Equal(BindingMode.OneWay, data.DefaultBindingMode);
        }

        private class TestMetadata : AvaloniaPropertyMetadata
        {
            public Action<AvaloniaObject> OwnerSpecificAction { get; }

            public TestMetadata(BindingMode defaultBindingMode = BindingMode.Default, 
                bool? enableDataValidation = null,
                Action<AvaloniaObject> ownerSpecificAction = null) 
                : base(defaultBindingMode, enableDataValidation)
            {
                OwnerSpecificAction = ownerSpecificAction;
            }

            public override AvaloniaPropertyMetadata GenerateTypeSafeMetadata() =>
                new TestMetadata(DefaultBindingMode, EnableDataValidation, null);
        }

        private class TestProperty<TValue> : AvaloniaProperty<TValue>
        {
            public TestProperty(string name, Type ownerType, TestMetadata metadata = null)
                : base(name, ownerType, ownerType, metadata ?? new TestMetadata())
            {
            }

            public void OverrideMetadata<T>(AvaloniaPropertyMetadata metadata)
            {
                OverrideMetadata(typeof(T), metadata);
            }

            internal override IDisposable RouteBind(
                AvaloniaObject o,
                IObservable<object> source,
                BindingPriority priority)
            {
                throw new NotImplementedException();
            }

            internal override void RouteClearValue(AvaloniaObject o)
            {
                throw new NotImplementedException();
            }

            internal override void RouteCoerceDefaultValue(AvaloniaObject o)
            {
                throw new NotImplementedException();
            }

            internal override object RouteGetValue(AvaloniaObject o)
            {
                throw new NotImplementedException();
            }

            internal override object RouteGetBaseValue(AvaloniaObject o)
            {
                throw new NotImplementedException();
            }

            internal override IDisposable RouteSetValue(
                AvaloniaObject o,
                object value,
                BindingPriority priority)
            {
                throw new NotImplementedException();
            }

            internal override void RouteSetCurrentValue(AvaloniaObject o, object value)
            {
                throw new NotImplementedException();
            }

            internal override EffectiveValue CreateEffectiveValue(AvaloniaObject o)
            {
                throw new NotImplementedException();
            }
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", "default",
                    inherits: true,
                    defaultBindingMode: BindingMode.OneWay,
                    validate: null,
                    coerce: null,
                    enableDataValidation: false,
                    notifying: FooNotifying);

            public int NotifyCount { get; private set; }

            private static void FooNotifying(AvaloniaObject o, bool n)
            {
                ++((Class1)o).NotifyCount;
            }
        }

        private class Class2 : Class1
        {
        }

        private class Class3 : AvaloniaObject
        {
        }
    }
}
