using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Avalonia.Data;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Coercion
    {
        [Fact]
        public void Coerces_Set_Value()
        {
            var target = new Class1();

            target.Foo = 150;

            Assert.Equal(100, target.Foo);
        }

        [Fact]
        public void Coerces_Set_Value_Attached()
        {
            var target = new Class1();

            target.SetValue(Class1.AttachedProperty, 150);

            Assert.Equal(100, target.GetValue(Class1.AttachedProperty));
        }

        [Fact]
        public void Coerces_Bound_Value()
        {
            var target = new Class1();
            var source = new Subject<BindingValue<int>>();

            target.Bind(Class1.FooProperty, source);
            source.OnNext(150);

            Assert.Equal(100, target.Foo);
        }

        [Fact]
        public void CoerceValue_Updates_Value()
        {
            var target = new Class1 { Foo = 99 };

            Assert.Equal(99, target.Foo);

            target.MaxFoo = 50;
            target.CoerceValue(Class1.FooProperty);

            Assert.Equal(50, target.Foo);
        }

        [Fact]
        public void CoerceValue_Updates_Base_Value()
        {
            var target = new Class1 { Foo = 99 };

            target.SetValue(Class1.FooProperty, 88, BindingPriority.Animation);

            Assert.Equal(88, target.Foo);
            Assert.Equal(99, target.GetBaseValue(Class1.FooProperty));

            target.MaxFoo = 50;
            target.CoerceValue(Class1.FooProperty);

            Assert.Equal(50, target.Foo);
            Assert.Equal(50, target.GetBaseValue(Class1.FooProperty));
        }

        [Fact]
        public void CoerceValue_Raises_PropertyChanged()
        {
            var target = new Class1 { Foo = 99 };
            var raised = 0;

            target.PropertyChanged += (s, e) =>
            {
                Assert.Equal(Class1.FooProperty, e.Property);
                Assert.Equal(99, e.OldValue);
                Assert.Equal(50, e.NewValue);
                Assert.Equal(BindingPriority.LocalValue, e.Priority);
                ++raised;
            };

            Assert.Equal(99, target.Foo);

            target.MaxFoo = 50;
            target.CoerceValue(Class1.FooProperty);

            Assert.Equal(50, target.Foo);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void CoerceValue_Raises_PropertyChangedCore_For_Base_Value()
        {
            var target = new Class1 { Foo = 99 };

            target.SetValue(Class1.FooProperty, 88, BindingPriority.Animation);

            Assert.Equal(88, target.Foo);
            Assert.Equal(99, target.GetBaseValue(Class1.FooProperty));

            target.MaxFoo = 50;
            target.CoreChanges.Clear();
            target.CoerceValue(Class1.FooProperty);

            Assert.Equal(2, target.CoreChanges.Count);
        }

        [Fact]
        public void Coerced_Value_Can_Be_Restored_If_Limit_Changed()
        {
            var target = new Class1();

            target.Foo = 150;
            Assert.Equal(100, target.Foo);

            target.MaxFoo = 200;
            target.CoerceValue(Class1.FooProperty);

            Assert.Equal(150, target.Foo);
        }

        [Fact]
        public void Coerced_Value_Can_Be_Restored_From_Previously_Active_Binding()
        {
            var target = new Class1();
            var source1 = new Subject<BindingValue<int>>();
            var source2 = new Subject<BindingValue<int>>();

            target.Bind(Class1.FooProperty, source1, BindingPriority.Style);
            source1.OnNext(150);

            target.Bind(Class1.FooProperty, source2);
            source2.OnNext(160);

            Assert.Equal(100, target.Foo);

            target.MaxFoo = 200;
            source2.OnCompleted();

            Assert.Equal(150, target.Foo);
        }

        [Fact]
        public void CoerceValue_Updates_Inherited_Value()
        {
            var parent = new Class1 { Inherited = 99 };
            var child = new AvaloniaObject { InheritanceParent = parent };
            var raised = 0;

            child.InheritanceParent = parent;
            child.PropertyChanged += (s, e) =>
            {
                Assert.Equal(Class1.InheritedProperty, e.Property);
                Assert.Equal(99, e.OldValue);
                Assert.Equal(50, e.NewValue);
                Assert.Equal(BindingPriority.Inherited, e.Priority);
                ++raised;
            };

            Assert.Equal(99, child.GetValue(Class1.InheritedProperty));

            parent.MaxFoo = 50;
            parent.CoerceValue(Class1.InheritedProperty);

            Assert.Equal(50, child.GetValue(Class1.InheritedProperty));
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Coercion_Can_Be_Overridden()
        {
            var target = new Class2();

            target.Foo = 150;

            Assert.Equal(-150, target.Foo);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<int> FooProperty =
                AvaloniaProperty.Register<Class1, int>(
                    "Qux",
                    defaultValue: 11,
                    coerce: CoerceFoo);

            public static readonly AttachedProperty<int> AttachedProperty =
                AvaloniaProperty.RegisterAttached<Class1, Class1, int>(
                    "Attached",
                    defaultValue: 11,
                    coerce: CoerceFoo);

            public static readonly StyledProperty<int> InheritedProperty =
                AvaloniaProperty.RegisterAttached<Class1, Class1, int>(
                    "Attached",
                    defaultValue: 11,
                    inherits: true,
                    coerce: CoerceFoo);

            public int Foo
            {
                get => GetValue(FooProperty);
                set => SetValue(FooProperty, value);
            }

            public int Inherited
            {
                get => GetValue(InheritedProperty);
                set => SetValue(InheritedProperty, value);
            }

            public int MaxFoo { get; set; } = 100;

            public List<AvaloniaPropertyChangedEventArgs> CoreChanges { get; } = new();

            public static int CoerceFoo(IAvaloniaObject instance, int value)
            {
                return Math.Min(((Class1)instance).MaxFoo, value);
            }

            protected override void OnPropertyChangedCore(AvaloniaPropertyChangedEventArgs change)
            {
                CoreChanges.Add(Clone(change));
                base.OnPropertyChangedCore(change);
            }

            private static AvaloniaPropertyChangedEventArgs Clone(AvaloniaPropertyChangedEventArgs change)
            {
                var e = (AvaloniaPropertyChangedEventArgs<int>)change;
                return new AvaloniaPropertyChangedEventArgs<int>(
                    change.Sender,
                    e.Property,
                    e.OldValue,
                    e.NewValue,
                    change.Priority,
                    change.IsEffectiveValueChange);
            }
        }

        private class Class2 : AvaloniaObject
        {
            public static readonly StyledProperty<int> FooProperty =
                Class1.FooProperty.AddOwner<Class2>();

            static Class2()
            {
                FooProperty.OverrideMetadata<Class2>(
                    new StyledPropertyMetadata<int>(
                        coerce: CoerceFoo));
            }

            public int Foo
            {
                get => GetValue(FooProperty);
                set => SetValue(FooProperty, value);
            }

            public static int CoerceFoo(IAvaloniaObject instance, int value)
            {
                return -value;
            }
        }
    }
}
