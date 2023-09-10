using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class BindingExpressionTests_SetValue
    {
        [Fact]
        public void Should_Set_Simple_Property_Value()
        {
            var data = new Person { Name = "Frank" };
            var target = BindingExpression.Create(data, o => o.Name);

            using (target.Subscribe(_ => { }))
            {
                target.SetValue("Kups");
            }

            Assert.Equal("Kups", data.Name);
        }

        [Fact]
        public void Should_Set_Attached_Property_Value()
        {
            var data = new AvaloniaObject();
            var target = BindingExpression.Create(data, o => o[DockPanel.DockProperty]);

            using (target.Subscribe(_ => { }))
            {
                target.SetValue(Dock.Right);
            }

            Assert.Equal(Dock.Right, data[DockPanel.DockProperty]);
        }

        [Fact]
        public void Should_Set_Indexed_Value()
        {
            var data = new { Foo = new[] { "foo" } };
            var target = BindingExpression.Create(data, o => o.Foo[0]);

            using (target.Subscribe(_ => { }))
            {
                target.SetValue("bar");
            }

            Assert.Equal("bar", data.Foo[0]);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Set_Value_On_Simple_Property_Chain()
        {
            var data = new Person { Pet = new Dog { Name = "Fido" } };
            var target = BindingExpression.Create(data, o => o.Pet!.Name);

            using (target.Subscribe(_ => { }))
            {
                target.SetValue("Rover");
            }

            Assert.Equal("Rover", data.Pet.Name);
        }

        [Fact]
        public void Should_Not_Try_To_Set_Value_On_Broken_Chain()
        {
            var data = new Person { Pet = new Dog { Name = "Fido" } };
            var target = BindingExpression.Create(data, o => o.Pet!.Name);

            // Ensure the UntypedBindingExpression's subscriptions are kept active.
            using (target!.OfType<string?>().Subscribe(x => { }))
            {
                data.Pet = null;
                Assert.False(target.SetValue("Rover"));
            }
        }

        [Fact]
        public void SetValue_Should_Return_False_For_Missing_Property()
        {
            var data = new Person { Pet = new Cat() };
            var target = BindingExpression.Create(data, o => (o.Pet as Dog)!.IsBarky);

            using (target.Subscribe(_ => { }))
            {
                Assert.False(target.SetValue("baz"));
            }

            GC.KeepAlive(data);
        }

        [Fact]
        public void SetValue_Should_Notify_New_Value_With_Inpc()
        {
            var data = new Person();
            var target = BindingExpression.Create(data, o => o.Name);
            var result = new List<object?>();

            target.Subscribe(result.Add);
            target.SetValue("Frank");

            Assert.Equal(new[] { null, "Frank" }, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void SetValue_Should_Notify_New_Value_Without_Inpc()
        {
            var data = new Snail();
            var target = BindingExpression.Create(data, o => o.Name);
            var result = new List<object?>();

            target.Subscribe(result.Add);
            target.SetValue("Frank");

            Assert.Equal(new[] { null, "Frank" }, result);

            GC.KeepAlive(data);
        }

        [Fact]
        public void SetValue_Should_Return_False_For_Missing_Object()
        {
            var data = new Person();
            var target = BindingExpression.Create(data, o => (o.Pet as Dog)!.Name);

            using (target.Subscribe(_ => { }))
            {
                Assert.False(target.SetValue("Fido"));
            }

            GC.KeepAlive(data);
        }

        [Fact]
        public void SetValue_Should_Use_Converter()
        {
            var data = new Person { Name = "Frank" };
            var target = BindingExpression.Create(
                data, 
                o => o.Name,
                converter: new CaseConverter());

            using (target.Subscribe(_ => { }))
            {
                Assert.True(target.SetValue("Kups"));
            }

            Assert.Equal("kups", data.Name);

            GC.KeepAlive(data);
        }

        private interface IAnimal
        {
            string? Name { get; }
        }

        private class Person : NotifyingBase
        {
            private string? _name;
            private IAnimal? _pet;

            public string? Name
            {
                get => _name;
                set
                {
                    _name = value;
                    RaisePropertyChanged(nameof(Name));
                }
            }

            public IAnimal? Pet
            {
                get => _pet;
                set
                {
                    _pet = value;
                    RaisePropertyChanged(nameof(Pet));
                }
            }
        }

        private class Animal : NotifyingBase, IAnimal
        {
            private string? _name;

            public string? Name
            {
                get => _name;
                set
                {
                    _name = value;
                    RaisePropertyChanged(nameof(Name));
                }
            }
        }

        private class Dog : Animal
        {
            public bool IsBarky { get; set; }
        }

        private class Cat : Animal
        {
        }

        private class Snail : IAnimal
        {
            public string? Name { get; set; }
        }

        private class CaseConverter : IValueConverter
        {
            public static readonly CaseConverter Instance = new();

            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return value?.ToString()?.ToUpper();
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return value?.ToString()?.ToLower();
            }
        }
    }
}
