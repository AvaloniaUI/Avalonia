// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Data;
using Xunit;

namespace Perspex.Base.UnitTests
{
    public class PerspexPropertyTests
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
            var notify = (Action<IPerspexObject, bool>)((a, b) => { });
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
                Assert.Equal(PerspexProperty.UnsetValue, x.OldValue);
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

        ////[Fact]
        ////public void IsDirect_Property_Set_On_Direct_PerspexProperty()
        ////{
        ////    PerspexProperty<string> target = new PerspexProperty<string>(
        ////        "test",
        ////        typeof(Class1),
        ////        o => null,
        ////        (o, v) => { });

        ////    Assert.True(target.IsDirect);
        ////}

        [Fact]
        public void Property_Equals_Should_Handle_Null()
        {
            var p1 = new TestProperty<string>("p1", typeof(Class1));

            Assert.NotEqual(p1, null);
            Assert.NotEqual(null, p1);
            Assert.False(p1 == null);
            Assert.False(null == p1);
            Assert.False(p1.Equals(null));
            Assert.True((PerspexProperty)null == (PerspexProperty)null);
        }

        ////[Fact]
        ////public void AddOwnered_Property_Should_Equal_Original()
        ////{
        ////    var p1 = new PerspexProperty<string>("p1", typeof(Class1));
        ////    var p2 = p1.AddOwner<Class3>();

        ////    Assert.Equal(p1, p2);
        ////    Assert.Equal(p1.GetHashCode(), p2.GetHashCode());
        ////    Assert.True(p1 == p2);
        ////}

        ////[Fact]
        ////public void AddOwnered_Property_Should_Have_OwnerType_Set()
        ////{
        ////    var p1 = new PerspexProperty<string>("p1", typeof(Class1));
        ////    var p2 = p1.AddOwner<Class3>();

        ////    Assert.Equal(typeof(Class3), p2.OwnerType);
        ////}

        ////[Fact]
        ////public void AddOwnered_Properties_Should_Share_Observables()
        ////{
        ////    var p1 = new PerspexProperty<string>("p1", typeof(Class1));
        ////    var p2 = p1.AddOwner<Class3>();

        ////    Assert.Same(p1.Changed, p2.Changed);
        ////    Assert.Same(p1.Initialized, p2.Initialized);
        ////}

        ////[Fact]
        ////public void AddOwnered_Direct_Property_Should_Equal_Original()
        ////{
        ////    var p1 = new PerspexProperty<string>("d1", typeof(Class1), o => null, (o,v) => { });
        ////    var p2 = p1.AddOwner<Class3>(o => null, (o, v) => { });

        ////    Assert.Equal(p1, p2);
        ////    Assert.Equal(p1.GetHashCode(), p2.GetHashCode());
        ////    Assert.True(p1 == p2);
        ////}

        ////[Fact]
        ////public void AddOwnered_Direct_Property_Should_Have_OwnerType_Set()
        ////{
        ////    var p1 = new PerspexProperty<string>("d1", typeof(Class1), o => null, (o, v) => { });
        ////    var p2 = p1.AddOwner<Class3>(o => null, (o, v) => { });

        ////    Assert.Equal(typeof(Class3), p2.OwnerType);
        ////}

        ////[Fact]
        ////public void AddOwnered_Direct_Properties_Should_Share_Observables()
        ////{
        ////    var p1 = new PerspexProperty<string>("d1", typeof(Class1), o => null, (o, v) => { });
        ////    var p2 = p1.AddOwner<Class3>(o => null, (o, v) => { });

        ////    Assert.Same(p1.Changed, p2.Changed);
        ////    Assert.Same(p1.Initialized, p2.Initialized);
        ////}

        ////[Fact]
        ////public void AddOwner_With_Getter_And_Setter_On_Standard_Property_Should_Throw()
        ////{
        ////    var p1 = new PerspexProperty<string>("p1", typeof(Class1));

        ////    Assert.Throws<InvalidOperationException>(() => p1.AddOwner<Class3>(o => null, (o, v) => { }));
        ////}

        ////[Fact]
        ////public void AddOwner_On_Direct_Property_Without_Getter_Or_Setter_Should_Throw()
        ////{
        ////    var p1 = new PerspexProperty<string>("e1", typeof(Class1), o => null, (o, v) => { });

        ////    Assert.Throws<InvalidOperationException>(() => p1.AddOwner<Class3>());
        ////}

        private class TestProperty<TValue> : PerspexProperty<TValue>
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

        private class Class1 : PerspexObject
        {
            public static readonly StyledProperty<string> FooProperty =
                PerspexProperty.Register<Class1, string>("Foo", "default");
        }

        private class Class2 : Class1
        {
        }

        private class Class3 : PerspexObject
        {
        }
    }
}
