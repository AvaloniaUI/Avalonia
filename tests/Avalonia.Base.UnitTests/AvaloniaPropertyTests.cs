// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Data;
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
            var metadata = new PropertyMetadata();
            var target = new TestProperty<string>("test", typeof(Class1), metadata);

            Assert.Same(metadata, target.GetMetadata<Class1>());
        }

        [Fact]
        public void GetMetadata_Returns_Supplied_Value_For_Derived_Class()
        {
            var metadata = new PropertyMetadata();
            var target = new TestProperty<string>("test", typeof(Class1), metadata);

            Assert.Same(metadata, target.GetMetadata<Class2>());
        }

        [Fact]
        public void GetMetadata_Returns_Supplied_Value_For_Unrelated_Class()
        {
            var metadata = new PropertyMetadata();
            var target = new TestProperty<string>("test", typeof(Class3), metadata);

            Assert.Same(metadata, target.GetMetadata<Class2>());
        }

        [Fact]
        public void GetMetadata_Returns_Overridden_Value()
        {
            var metadata = new PropertyMetadata();
            var overridden = new PropertyMetadata();
            var target = new TestProperty<string>("test", typeof(Class1), metadata);

            target.OverrideMetadata<Class2>(overridden);

            Assert.Same(overridden, target.GetMetadata<Class2>());
        }

        [Fact]
        public void OverrideMetadata_Should_Merge_Values()
        {
            var metadata = new PropertyMetadata(BindingMode.TwoWay);
            var notify = (Action<IAvaloniaObject, bool>)((a, b) => { });
            var overridden = new PropertyMetadata();
            var target = new TestProperty<string>("test", typeof(Class1), metadata);

            target.OverrideMetadata<Class2>(overridden);

            var result = target.GetMetadata<Class2>();
            Assert.Equal(BindingMode.TwoWay, result.DefaultBindingMode);
        }

        [Fact]
        public void Initialized_Observable_Fired()
        {
            bool invoked = false;

            Class1.FooProperty.Initialized.Subscribe(x =>
            {
                Assert.Equal(AvaloniaProperty.UnsetValue, x.OldValue);
                Assert.Equal("default", x.NewValue);
                Assert.Equal(BindingPriority.Unset, x.Priority);
                invoked = true;
            });

            var target = new Class1();

            Assert.True(invoked);
        }

        [Fact]
        public void Changed_Observable_Fired()
        {
            var target = new Class1();
            string value = null;

            Class1.FooProperty.Changed.Subscribe(x => value = (string)x.NewValue);
            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", value);
        }

        [Fact]
        public void Property_Equals_Should_Handle_Null()
        {
            var p1 = new TestProperty<string>("p1", typeof(Class1));

            Assert.NotEqual(p1, null);
            Assert.NotEqual(null, p1);
            Assert.False(p1 == null);
            Assert.False(null == p1);
            Assert.False(p1.Equals(null));
            Assert.True((AvaloniaProperty)null == (AvaloniaProperty)null);
        }

        [Fact]
        public void PropertyMetadata_BindingMode_Default_Returns_OneWay()
        {
            var data = new PropertyMetadata(defaultBindingMode: BindingMode.Default);

            Assert.Equal(BindingMode.OneWay, data.DefaultBindingMode);
        }

        private class TestProperty<TValue> : AvaloniaProperty<TValue>
        {
            public TestProperty(string name, Type ownerType, PropertyMetadata metadata = null)
                : base(name, ownerType, metadata ?? new PropertyMetadata())
            {
            }

            public void OverrideMetadata<T>(PropertyMetadata metadata)
            {
                OverrideMetadata(typeof(T), metadata);
            }
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", "default");
        }

        private class Class2 : Class1
        {
        }

        private class Class3 : AvaloniaObject
        {
        }
    }
}
