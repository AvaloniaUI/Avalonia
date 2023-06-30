using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.UnitTests;
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
        public void Coerces_Set_Value_Attached_On_Class_Not_Derived_From_Owner()
        {
            var target = new Class2();

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
        public void CoerceValue_Calls_Coerce_Callback_Only_Once()
        {
            var target = new Class1 { Foo = 99 };

            target.MaxFoo = 50;
            
            target.CoerceFooInvocations.Clear();
            target.CoerceValue(Class1.FooProperty);

            Assert.Equal(new[] { 99 }, target.CoerceFooInvocations);
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

        [Fact]
        public void Default_Value_Can_Be_Coerced()
        {
            var target = new Class1();
            var raised = 0;

            target.MinFoo = 20;

            target.PropertyChanged += (s, e) =>
            {
                Assert.Equal(Class1.FooProperty, e.Property);
                Assert.Equal(11, e.OldValue);
                Assert.Equal(20, e.NewValue);
                Assert.Equal(BindingPriority.Unset, e.Priority);
                ++raised;
            };

            target.CoerceValue(Class1.FooProperty);

            Assert.Equal(20, target.Foo);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Default_Value_Is_Coerced_Only_Once()
        {
            var target = new Class1();

            target.MinFoo = 20;
            target.CoerceFooInvocations.Clear();
            target.CoerceValue(Class1.FooProperty);

            Assert.Equal(new[] { 11 }, target.CoerceFooInvocations);
        }

        [Fact]
        public void Second_Coerce_Of_Default_Value_Is_Passed_Uncoerced_Value()
        {
            var target = new Class1();

            target.MinFoo = 20;
            target.CoerceFooInvocations.Clear();
            target.CoerceValue(Class1.FooProperty);
            target.CoerceValue(Class1.FooProperty);

            Assert.Equal(new[] { 11, 11 }, target.CoerceFooInvocations);
        }

        [Fact]
        public void ClearValue_Respects_Coerced_Default_Value()
        {
            var target = new Class1();
            var raised = 0;

            target.Foo = 30;
            target.MinFoo = 20;

            target.PropertyChanged += (s, e) =>
            {
                Assert.Equal(Class1.FooProperty, e.Property);
                Assert.Equal(30, e.OldValue);
                Assert.Equal(20, e.NewValue);
                Assert.Equal(BindingPriority.Unset, e.Priority);
                ++raised;
            };

            target.ClearValue(Class1.FooProperty);

            Assert.Equal(20, target.Foo);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void Deactivating_Style_Respects_Coerced_Default_Value()
        {
            var target = new Control1
            {
                MinFoo = 20,
            };

            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<Control1>().Class("foo"))
                    {
                        Setters =
                        {
                            new Setter(Control1.FooProperty, 50),
                        },
                    },
                },
                Child = target,
            };

            var raised = 0;

            target.Classes.Add("foo");
            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Equal(50, target.Foo);

            target.PropertyChanged += (s, e) =>
            {
                Assert.Equal(Control1.FooProperty, e.Property);
                Assert.Equal(50, e.OldValue);
                Assert.Equal(20, e.NewValue);
                Assert.Equal(BindingPriority.Unset, e.Priority);
                ++raised;
            };

            target.Classes.Remove("foo");

            Assert.Equal(20, target.Foo);
            Assert.Equal(1, raised);
        }

        [Fact]
        public void If_Initial_State_Has_Coerced_Default_Value_Then_CoerceValue_Must_Be_Called()
        {
            // This test is just explicitly describing an edge-case. If the initial state of the
            // object results in a coerced property value then CoerceValue must be called before
            // coercion takes effect. Confirmed as matching the behavior of WPF.
            var target = new Class3();

            Assert.Equal(11, target.Foo);

            target.CoerceValue(Class3.FooProperty);

            Assert.Equal(50, target.Foo);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<int> FooProperty =
                AvaloniaProperty.Register<Class1, int>(
                    "Foo",
                    defaultValue: 11,
                    coerce: CoerceFoo);

            public static readonly AttachedProperty<int> AttachedProperty =
                AvaloniaProperty.RegisterAttached<Class1, AvaloniaObject, int>(
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

            public int MinFoo { get; set; } = 0;
            public int MaxFoo { get; set; } = 100;

            public List<int> CoerceFooInvocations { get; } = new();
            public List<AvaloniaPropertyChangedEventArgs> CoreChanges { get; } = new();

            public static int CoerceFoo(AvaloniaObject instance, int value)
            {
                (instance as Class1)?.CoerceFooInvocations.Add(value);
                return instance is Class1 o ? 
                    Math.Clamp(value, o.MinFoo, o.MaxFoo) :
                    Math.Clamp(value, 0, 100);
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

            public static int CoerceFoo(AvaloniaObject instance, int value)
            {
                return -value;
            }
        }

        private class Class3: AvaloniaObject
        {
            public static readonly StyledProperty<int> FooProperty =
                AvaloniaProperty.Register<Class3, int>(
                    "Foo",
                    defaultValue: 11,
                    coerce: CoerceFoo);

            public int Foo
            {
                get => GetValue(FooProperty);
                set => SetValue(FooProperty, value);
            }


            public static int CoerceFoo(AvaloniaObject instance, int value)
            {
                var o = (Class3)instance;
                return Math.Clamp(value, 50, 100);
            }
        }

        private class Control1 : Control 
        {
            public static readonly StyledProperty<int> FooProperty =
                AvaloniaProperty.Register<Control1, int>(
                    "Foo",
                    defaultValue: 11,
                    coerce: CoerceFoo);

            public int Foo
            {
                get => GetValue(FooProperty);
                set => SetValue(FooProperty, value);
            }

            public int MinFoo { get; set; } = 0;
            public int MaxFoo { get; set; } = 100;

            public static int CoerceFoo(AvaloniaObject instance, int value)
            {
                var o = (Control1)instance;
                return Math.Clamp(value, o.MinFoo, o.MaxFoo);
            }
        }
    }
}
