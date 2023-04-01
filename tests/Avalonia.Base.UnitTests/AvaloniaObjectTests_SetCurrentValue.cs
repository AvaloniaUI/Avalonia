using System;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;
using Observable = Avalonia.Reactive.Observable;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_SetCurrentValue
    {
        [Fact]
        public void SetCurrentValue_Sets_Unset_Value()
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.GetValue(Class1.FooProperty));
            Assert.True(target.IsSet(Class1.FooProperty));
            Assert.Equal(BindingPriority.Unset, GetPriority(target, Class1.FooProperty));
            Assert.True(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void SetCurrentValue_Sets_Unset_Value_Untyped()
        {
            var target = new Class1();

            target.SetCurrentValue((AvaloniaProperty)Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.GetValue(Class1.FooProperty));
            Assert.True(target.IsSet(Class1.FooProperty));
            Assert.Equal(BindingPriority.Unset, GetPriority(target, Class1.FooProperty));
            Assert.True(IsOverridden(target, Class1.FooProperty));
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        [InlineData(BindingPriority.Animation)]
        public void SetCurrentValue_Overrides_Existing_Value(BindingPriority priority)
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "oldvalue", priority);
            target.SetCurrentValue(Class1.FooProperty, "newvalue");

            Assert.Equal("newvalue", target.GetValue(Class1.FooProperty));
            Assert.True(target.IsSet(Class1.FooProperty));
            Assert.Equal(priority, GetPriority(target, Class1.FooProperty));
            Assert.True(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void SetCurrentValue_Overrides_Inherited_Value()
        {
            var parent = new Class1();
            var target = new Class1 { InheritanceParent = parent };

            parent.SetValue(Class1.InheritedProperty, "inheritedvalue");
            target.SetCurrentValue(Class1.InheritedProperty, "newvalue");

            Assert.Equal("newvalue", target.GetValue(Class1.InheritedProperty));
            Assert.True(target.IsSet(Class1.InheritedProperty));
            Assert.Equal(BindingPriority.Unset, GetPriority(target, Class1.InheritedProperty));
            Assert.True(IsOverridden(target, Class1.InheritedProperty));
        }

        [Fact]
        public void SetCurrentValue_Is_Inherited()
        {
            var parent = new Class1();
            var target = new Class1 { InheritanceParent = parent };

            parent.SetCurrentValue(Class1.InheritedProperty, "newvalue");

            Assert.Equal("newvalue", target.GetValue(Class1.InheritedProperty));
            Assert.False(target.IsSet(Class1.FooProperty));
            Assert.Equal(BindingPriority.Inherited, GetPriority(target, Class1.InheritedProperty));
            Assert.False(IsOverridden(target, Class1.InheritedProperty));
        }

        [Fact]
        public void ClearValue_Clears_CurrentValue_With_Unset_Priority()
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, "newvalue");
            target.ClearValue(Class1.FooProperty);

            Assert.Equal("foodefault", target.Foo);
            Assert.False(target.IsSet(Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void ClearValue_Clears_CurrentValue_With_Inherited_Priority()
        {
            var parent = new Class1();
            var target = new Class1 { InheritanceParent = parent };

            parent.SetValue(Class1.InheritedProperty, "inheritedvalue");
            target.SetCurrentValue(Class1.InheritedProperty, "newvalue");
            target.ClearValue(Class1.InheritedProperty);

            Assert.Equal("inheritedvalue", target.Inherited);
            Assert.False(target.IsSet(Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void ClearValue_Clears_CurrentValue_With_LocalValue_Priority()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "localvalue");
            target.SetCurrentValue(Class1.FooProperty, "newvalue");
            target.ClearValue(Class1.FooProperty);

            Assert.Equal("foodefault", target.Foo);
            Assert.False(target.IsSet(Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void ClearValue_Clears_CurrentValue_With_Style_Priority()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "stylevalue", BindingPriority.Style);
            target.SetCurrentValue(Class1.FooProperty, "newvalue");
            target.ClearValue(Class1.FooProperty);

            Assert.Equal("stylevalue", target.Foo);
            Assert.True(target.IsSet(Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void SetCurrentValue_Can_Be_Coerced()
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.CoercedProperty, 60);
            Assert.Equal(60, target.GetValue(Class1.CoercedProperty));

            target.CoerceMax = 50;
            target.CoerceValue(Class1.CoercedProperty);
            Assert.Equal(50, target.GetValue(Class1.CoercedProperty));

            target.CoerceMax = 100;
            target.CoerceValue(Class1.CoercedProperty);
            Assert.Equal(60, target.GetValue(Class1.CoercedProperty));
        }

        [Fact]
        public void SetCurrentValue_Unset_Clears_CurrentValue()
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, "newvalue");
            target.SetCurrentValue(Class1.FooProperty, AvaloniaProperty.UnsetValue);

            Assert.Equal("foodefault", target.Foo);
            Assert.False(target.IsSet(Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        [InlineData(BindingPriority.Animation)]
        public void SetValue_Overrides_CurrentValue_With_Unset_Priority(BindingPriority priority)
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, "current");
            target.SetValue(Class1.FooProperty, "setvalue", priority);

            Assert.Equal("setvalue", target.Foo);
            Assert.True(target.IsSet(Class1.FooProperty));
            Assert.Equal(priority, GetPriority(target, Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void Animation_Value_Overrides_CurrentValue_With_LocalValue_Priority()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "localvalue");
            target.SetCurrentValue(Class1.FooProperty, "current");
            target.SetValue(Class1.FooProperty, "setvalue", BindingPriority.Animation);

            Assert.Equal("setvalue", target.Foo);
            Assert.True(target.IsSet(Class1.FooProperty));
            Assert.Equal(BindingPriority.Animation, GetPriority(target, Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Fact]
        public void StyleTrigger_Value_Overrides_CurrentValue_With_Style_Priority()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "style", BindingPriority.Style);
            target.SetCurrentValue(Class1.FooProperty, "current");
            target.SetValue(Class1.FooProperty, "setvalue", BindingPriority.StyleTrigger);

            Assert.Equal("setvalue", target.Foo);
            Assert.True(target.IsSet(Class1.FooProperty));
            Assert.Equal(BindingPriority.StyleTrigger, GetPriority(target, Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        [InlineData(BindingPriority.Animation)]
        public void Binding_Overrides_CurrentValue_With_Unset_Priority(BindingPriority priority)
        {
            var target = new Class1();

            target.SetCurrentValue(Class1.FooProperty, "current");
            
            var s = target.Bind(Class1.FooProperty, Observable.SingleValue("binding"), priority);

            Assert.Equal("binding", target.Foo);
            Assert.True(target.IsSet(Class1.FooProperty));
            Assert.Equal(priority, GetPriority(target, Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));

            s.Dispose();

            Assert.Equal("foodefault", target.Foo);
        }

        [Fact]
        public void Animation_Binding_Overrides_CurrentValue_With_LocalValue_Priority()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "localvalue");
            target.SetCurrentValue(Class1.FooProperty, "current");

            var s = target.Bind(Class1.FooProperty, Observable.SingleValue("binding"), BindingPriority.Animation);

            Assert.Equal("binding", target.Foo);
            Assert.True(target.IsSet(Class1.FooProperty));
            Assert.Equal(BindingPriority.Animation, GetPriority(target, Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));

            s.Dispose();

            Assert.Equal("current", target.Foo);
        }

        [Fact]
        public void StyleTrigger_Binding_Overrides_CurrentValue_With_Style_Priority()
        {
            var target = new Class1();

            target.SetValue(Class1.FooProperty, "style", BindingPriority.Style);
            target.SetCurrentValue(Class1.FooProperty, "current");
            
            var s = target.Bind(Class1.FooProperty, Observable.SingleValue("binding"), BindingPriority.StyleTrigger);

            Assert.Equal("binding", target.Foo);
            Assert.True(target.IsSet(Class1.FooProperty));
            Assert.Equal(BindingPriority.StyleTrigger, GetPriority(target, Class1.FooProperty));
            Assert.False(IsOverridden(target, Class1.FooProperty));

            s.Dispose();

            Assert.Equal("style", target.Foo);
        }

        [Fact]
        public void SetCurrent_Value_Persists_When_Toggling_Style_1()
        {
            var target = new Class1();
            var root = new TestRoot(target)
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>().Class("foo"))
                    {
                        Setters = { new Setter(Class1.BarProperty, "bar") },
                    }
                }
            };

            root.LayoutManager.ExecuteInitialLayoutPass();

            target.SetCurrentValue(Class1.FooProperty, "current");

            Assert.Equal("current", target.Foo);
            Assert.Equal("bardefault", target.Bar);

            target.Classes.Add("foo");

            Assert.Equal("current", target.Foo);
            Assert.Equal("bar", target.Bar);

            target.Classes.Remove("foo");

            Assert.Equal("current", target.Foo);
            Assert.Equal("bardefault", target.Bar);
        }

        [Fact]
        public void SetCurrent_Value_Persists_When_Toggling_Style_2()
        {
            var target = new Class1();
            var root = new TestRoot(target)
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>().Class("foo"))
                    {
                        Setters = 
                        { 
                            new Setter(Class1.BarProperty, "bar"),
                            new Setter(Class1.InheritedProperty, "inherited"),
                        },
                    }
                }
            };

            root.LayoutManager.ExecuteInitialLayoutPass();

            target.SetCurrentValue(Class1.FooProperty, "current");

            Assert.Equal("current", target.Foo);
            Assert.Equal("bardefault", target.Bar);
            Assert.Equal("inheriteddefault", target.Inherited);

            target.Classes.Add("foo");

            Assert.Equal("current", target.Foo);
            Assert.Equal("bar", target.Bar);
            Assert.Equal("inherited", target.Inherited);

            target.Classes.Remove("foo");

            Assert.Equal("current", target.Foo);
            Assert.Equal("bardefault", target.Bar);
            Assert.Equal("inheriteddefault", target.Inherited);
        }

        [Fact]
        public void SetCurrent_Value_Persists_When_Toggling_Style_3()
        {
            var target = new Class1();
            var root = new TestRoot(target)
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>().Class("foo"))
                    {
                        Setters =
                        {
                            new Setter(Class1.BarProperty, "bar"),
                            new Setter(Class1.InheritedProperty, "inherited"),
                        },
                    }
                }
            };

            root.LayoutManager.ExecuteInitialLayoutPass();

            target.SetValue(Class1.FooProperty, "not current", BindingPriority.Template);
            target.SetCurrentValue(Class1.FooProperty, "current");

            Assert.Equal("current", target.Foo);
            Assert.Equal("bardefault", target.Bar);
            Assert.Equal("inheriteddefault", target.Inherited);

            target.Classes.Add("foo");

            Assert.Equal("current", target.Foo);
            Assert.Equal("bar", target.Bar);
            Assert.Equal("inherited", target.Inherited);

            target.Classes.Remove("foo");

            Assert.Equal("current", target.Foo);
            Assert.Equal("bardefault", target.Bar);
            Assert.Equal("inheriteddefault", target.Inherited);
        }

        [Theory]
        [InlineData(BindingPriority.LocalValue)]
        [InlineData(BindingPriority.Style)]
        [InlineData(BindingPriority.Animation)]
        public void CurrentValue_Is_Replaced_By_Binding_Value(BindingPriority priority)
        {
            var target = new Class1();
            var source = new BehaviorSubject<string>("initial");

            target.Bind(Class1.FooProperty, source, priority);
            target.SetCurrentValue(Class1.FooProperty, "current");
            source.OnNext("new");
            
            Assert.Equal("new", target.Foo);
        }

        [Fact]
        public void CurrentValue_Is_Replaced_By_New_Style_Activation_1()
        {
            var target = new Class1();
            var root = new TestRoot(target)
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>().Class("foo"))
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "initial"),
                            new Setter(Class1.BarProperty, "bar"),
                        },
                    },
                    new Style(x => x.OfType<Class1>().Class("bar"))
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "new"),
                            new Setter(Class1.BarProperty, "baz"),
                        },
                    },                }
            };

            root.LayoutManager.ExecuteInitialLayoutPass();

            target.Classes.Add("foo");
            Assert.Equal("initial", target.Foo);

            target.SetCurrentValue(Class1.FooProperty, "current");
            target.Classes.Add("bar");

            Assert.Equal("new", target.Foo);
        }

        [Fact]
        public void CurrentValue_Is_Replaced_By_New_Style_Activation_2()
        {
            var target = new Class1();
            var root = new TestRoot(target)
            {
                Styles =
                {
                    new Style(x => x.OfType<Class1>().Class("foo"))
                    {
                        Setters =
                        {
                            new Setter(Class1.FooProperty, "foo"),
                        },
                    },
                    new Style(x => x.OfType<Class1>().Class("foo"))
                    {
                        Setters =
                        {
                            new Setter(Class1.BarProperty, "bar"),
                        },
                    },
                }
            };

            root.LayoutManager.ExecuteInitialLayoutPass();

            target.SetValue(Class1.FooProperty, "template", BindingPriority.Template);
            target.SetCurrentValue(Class1.FooProperty, "current");

            target.Classes.Add("foo");
            Assert.Equal("foo", target.Foo);
        }

        private BindingPriority GetPriority(AvaloniaObject target, AvaloniaProperty property)
        {
            return target.GetDiagnostic(property).Priority;
        }

        private bool IsOverridden(AvaloniaObject target, AvaloniaProperty property)
        {
            return target.GetDiagnostic(property).IsOverriddenCurrentValue;
        }

        private class Class1 : Control
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>(nameof(Foo), "foodefault");
            public static readonly StyledProperty<string> BarProperty =
                AvaloniaProperty.Register<Class1, string>(nameof(Bar), "bardefault");
            public static readonly StyledProperty<string> InheritedProperty =
                AvaloniaProperty.Register<Class1, string>(nameof(Inherited), "inheriteddefault", inherits: true);
            public static readonly StyledProperty<double> CoercedProperty =
                AvaloniaProperty.Register<Class1, double>(nameof(Coerced), coerce: Coerce);

            public string Foo => GetValue(FooProperty);
            public string Bar => GetValue(BarProperty);
            public string Inherited => GetValue(InheritedProperty);
            public double Coerced => GetValue(CoercedProperty);
            public double CoerceMax { get; set; } = 100;

            private static double Coerce(AvaloniaObject sender, double value)
            {
                return Math.Min(value, ((Class1)sender).CoerceMax);
            }
        }

        private class ViewModel : NotifyingBase
        {
            private string _value;

            public string Value
            {
                get => _value;
                set
                {
                    if (_value != value)
                    {
                        _value = value;
                        RaisePropertyChanged();
                    }
                }
            }
        }
    }
}
