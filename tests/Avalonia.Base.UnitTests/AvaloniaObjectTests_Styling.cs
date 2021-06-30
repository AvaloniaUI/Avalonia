using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Styling
    {
        [Fact]
        public void Applies_Style_With_StyledProperty_Setter()
        {
            var target = new Class1();
            var style = new Style(x => x.OfType<Class1>())
            {
                Setters = { new Setter(Class1.FooProperty, "foo") }
            };

            ApplyStyles(target, style);

            Assert.Equal("foo", target.Foo);
        }

        [Fact]
        public void Applies_Style_With_DirectProperty_Setter()
        {
            var target = new Class1();
            var style = new Style(x => x.OfType<Class1>())
            {
                Setters = { new Setter(Class1.BarProperty, "foo") }
            };

            ApplyStyles(target, style);

            Assert.Equal("foo", target.Bar);
        }

        [Fact]
        public void Applies_Style_With_Binding()
        {
            var target = new Class1();
            var style = new Style(x => x.OfType<Class1>())
            {
                Setters = { new Setter(Class1.FooProperty, new TestBinding("foo")) }
            };

            ApplyStyles(target, style);

            Assert.Equal("foo", target.Foo);
        }

        [Fact]
        public void Later_Style_Overrides_Prior()
        {
            var target = new Class1();
            var styles = new[]
            {
                new Style(x => x.OfType<Class1>())
                {
                    Setters = { new Setter(Class1.FooProperty, "foo") }
                },
                new Style(x => x.OfType<Class1>())
                {
                    Setters = { new Setter(Class1.FooProperty, "bar") }
                },
            };

            ApplyStyles(target, styles);

            Assert.Equal("bar", target.Foo);
        }

        [Fact]
        public void Later_Style_Doesnt_Override_Prior_Style_Of_Higher_Priority()
        {
            var target = new Class1 { Classes = { "class" } };
            var styles = new[]
            {
                new Style(x => x.OfType<Class1>().Class("class"))
                {
                    Setters = { new Setter(Class1.FooProperty, "foo") }
                },
                new Style(x => x.OfType<Class1>())
                {
                    Setters = { new Setter(Class1.FooProperty, "bar") }
                },
            };

            ApplyStyles(target, styles);

            Assert.Equal("foo", target.Foo);
        }

        [Fact]
        public void Style_Doesnt_Override_LocalValue()
        {
            var target = new Class1();
            var style = new Style(x => x.OfType<Class1>())
            {
                Setters = { new Setter(Class1.FooProperty, "bar") }
            };

            target.SetValue(Class1.FooProperty, "foo");
            ApplyStyles(target, style);

            Assert.Equal("foo", target.Foo);
        }

        [Fact]
        public void Style_Doesnt_Override_LocalValue_2()
        {
            var target = new Class1();
            var style = new Style(x => x.OfType<Class1>())
            {
                Setters = { new Setter(Class1.FooProperty, "bar") }
            };

            ApplyStyles(target, style);
            target.SetValue(Class1.FooProperty, "foo");

            Assert.Equal("foo", target.Foo);
        }

        [Fact]
        public void Style_With_Class_Selector_Should_Update_And_Restore_Value()
        {
            var target = new Class1();
            var style = new Style(x => x.OfType<Class1>().Class("foo"))
            {
                Setters = { new Setter(Class1.FooProperty, "Foo") },
            };

            ApplyStyles(target, style);

            Assert.Equal("foodefault", target.Foo);
            target.Classes.Add("foo");
            Assert.Equal("Foo", target.Foo);
            target.Classes.Remove("foo");
            Assert.Equal("foodefault", target.Foo);
        }

        [Fact]
        public void Raises_PropertyChanged_On_Call_To_EndStyling()
        {
            var styles = new[]
            {
                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters = { new Setter(Class1.FooProperty, "Foo") },
                },

                new Style(x => x.OfType<Class1>().Class("bar"))
                {
                    Setters = { new Setter(Class1.FooProperty, "Bar") },
                }
            };

            var target = new Class1();
            var raised = 0;

            target.PropertyChanged += (s, e) =>
            {
                ++raised;
            };

            var stylable = (IStyleable)target;
            stylable.BeginStyling();
            
            foreach (var style in styles)
                stylable.ApplyStyle(style);

            Assert.Equal(0, raised);

            stylable.EndStyling();

            Assert.Equal(0, raised);
        }

        [Fact]
        public void Raises_PropertyChanged_For_Style_Activation_Changes()
        {
            var styles = new[]
            {
                new Style(x => x.OfType<Class1>().Class("foo"))
                {
                    Setters = { new Setter(Class1.FooProperty, "Foo") },
                },

                new Style(x => x.OfType<Class1>().Class("bar"))
                {
                    Setters = { new Setter(Class1.FooProperty, "Bar") },
                }
            };

            var target = new Class1();
            var values = new List<string?> { target.Foo };

            target.PropertyChanged += (s, e) =>
            {
                if (e.Property == Class1.FooProperty)
                {
                    values.Add((string?)e.NewValue);
                }
            };

            ApplyStyles(target, styles);
            target.Classes.Add("foo");
            target.Classes.Add("bar");
            target.Classes.Remove("foo");
            target.Classes.Remove("bar");

            Assert.Equal(new[] { "foodefault", "Foo", "Bar", "foodefault" }, values);
        }

        [Fact]
        public void Raises_PropertyChanged_For_Binding_Changes()
        {
            var target = new Class1();
            var binding = new TestBinding("foo");
            var style = new Style(x => x.OfType<Class1>())
            {
                Setters = { new Setter(Class1.FooProperty, binding) }
            };

            ApplyStyles(target, style);

            var values = new List<string?> { target.Foo };

            target.PropertyChanged += (s, e) =>
            {
                if (e.Property == Class1.FooProperty)
                {
                    values.Add((string?)e.NewValue);
                }
            };

            binding.OnNext("bar");
            binding.OnNext(AvaloniaProperty.UnsetValue);

            Assert.Equal(new[] { "foo", "bar", "foodefault" }, values);
        }

        [Fact]
        public void _Style_With_Activator_Is_Correctly_Applied_To_Multiple_Controls()
        {
            var target1 = new Class1();
            var target2 = new Class1();
            var style = new Style(x => x.OfType<Class1>().Class("foo"))
            {
                Setters = { new Setter(Class1.FooProperty, "foo") }
            };

            ApplyStyles(target1, style);
            ApplyStyles(target2, style);
            target1.Classes.Add("foo");

            Assert.Equal("foo", target1.Foo);
            Assert.Equal("foodefault", target2.Foo);
        }

        private static void ApplyStyles(IStyleable target, params Style[] styles)
        {
            target.BeginStyling();
            foreach (var style in styles)
                target.ApplyStyle(style);
            target.EndStyling();
        }

        private class Class1 : Control
        {
            public static readonly StyledProperty<string?> FooProperty = 
                AvaloniaProperty.Register<Class1, string?>(nameof(Foo), "foodefault");
            public static readonly DirectProperty<Class1, string?> BarProperty =
                AvaloniaProperty.RegisterDirect<Class1, string?>(nameof(Bar), o => o.Bar, (o, v) => o.Bar = v);

            private string? _bar;

            public string? Foo
            {
                get => GetValue(FooProperty);
                set => SetValue(FooProperty, value);
            }

            public string? Bar
            {
                get => _bar;
                set => SetAndRaise(BarProperty, ref _bar, value);
            }
        }

        internal class TestBinding : IBinding, IObservable<object?>
        {
            private List<IObserver<object?>> _observers = new();
            private object? _value;

            public TestBinding(object? initialValue) => _value = initialValue;

            public int InstanceCount { get; private set; }
            public int ObserverCount => _observers.Count;

            public InstancedBinding Initiate(
                IAvaloniaObject target,
                AvaloniaProperty targetProperty,
                object? anchor = null,
                bool enableDataValidation = false)
            {
                ++InstanceCount;
                return InstancedBinding.OneWay(this, BindingPriority.LocalValue);
            }

            public void OnNext(object? value)
            {
                _value = value;
                foreach (var observer in _observers)
                    observer.OnNext(value);
            }

            public IDisposable Subscribe(IObserver<object?> observer)
            {
                _observers.Add(observer);
                observer.OnNext(_value);
                return Disposable.Create(() => _observers.Remove(observer));
            }
        }
    }
}
