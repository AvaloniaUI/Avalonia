using System;
using System.ComponentModel;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.LeakTests
{
    public class AvaloniaObjectTests : ScopedTestBase
    {
        [Fact]
        public void Binding_To_Direct_Property_Does_Not_Get_Collected()
        {
            var target = new Class1();

            Func<WeakReference> setupBinding = () =>
            {
                var source = new Subject<string>();
                var sub = target.Bind((AvaloniaProperty)Class1.FooProperty, source);
                source.OnNext("foo");
                return new WeakReference(source);
            };

            var weakSource = setupBinding();

            CollectGarbage();

            Assert.Equal("foo", target.Foo);
            Assert.True(weakSource.IsAlive);
        }

        [Fact]
        public void Binding_To_Direct_Property_Gets_Collected_When_Completed()
        {
            var target = new Class1();

            Func<WeakReference> setupBinding = () =>
            {
                var source = new Subject<string>();
                var sub = target.Bind((AvaloniaProperty)Class1.FooProperty, source);
                return new WeakReference(source);
            };

            var weakSource = setupBinding();

            Action completeSource = () =>
            {
                ((ISubject<string>)weakSource.Target!).OnCompleted();
            };

            completeSource();
            CollectGarbage();
            Assert.False(weakSource.IsAlive);
        }

        [Fact]
        public void CompiledBinding_To_InpcProperty_With_Alive_Source_Does_Not_Keep_Target_Alive()
        {
            var source = new Class2 { Foo = "foo" };

            WeakReference SetupBinding()
            {
                var path = new CompiledBindingPathBuilder()
                    .Property(
                        new ClrPropertyInfo(
                            nameof(Class2.Foo),
                            target => ((Class2)target).Foo,
                            (target, value) => ((Class2)target).Foo = (string?)value,
                            typeof(string)),
                        PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
                    .Build();

                var target = new TextBlock();

                target.Bind(TextBlock.TextProperty, new CompiledBindingExtension
                {
                    Source = source,
                    Path = path
                });

                return new WeakReference(target);
            }

            var weakTarget = SetupBinding();

            CollectGarbage();
            Assert.False(weakTarget.IsAlive);
        }

        [Fact]
        public void CompiledBinding_To_AvaloniaProperty_With_Alive_Source_Does_Not_Keep_Target_Alive()
        {
            var source = new StyledElement { Name = "foo" };

            WeakReference SetupBinding()
            {
                var path = new CompiledBindingPathBuilder()
                    .Property(StyledElement.NameProperty, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor)
                    .Build();

                var target = new TextBlock();

                target.Bind(TextBlock.TextProperty, new CompiledBindingExtension
                {
                    Source = source,
                    Path = path
                });

                return new WeakReference(target);
            }

            var weakTarget = SetupBinding();

            CollectGarbage();
            Assert.False(weakTarget.IsAlive);
        }

        [Fact]
        public void CompiledBinding_To_Method_With_Alive_Source_Does_Not_Keep_Target_Alive()
        {
            var source = new Class1();

            WeakReference SetupBinding()
            {
                var path = new CompiledBindingPathBuilder()
                    .Command(
                        nameof(Class1.DoSomething),
                        (o, _) => ((Class1) o).DoSomething(),
                        (_, _) => true,
                        [])
                    .Build();

                var target = new Button();

                target.Bind(Button.CommandProperty, new CompiledBindingExtension
                {
                    Source = source,
                    Path = path
                });

                return new WeakReference(target);
            }

            var weakTarget = SetupBinding();
            
            CollectGarbage();
            Assert.False(weakTarget.IsAlive);
        }

        [Fact]
        public void Binding_To_AttachedProperty_With_Alive_Source_Does_Not_Keep_Target_Alive()
        {
            var source = new StyledElement { Name = "foo" };

            WeakReference SetupBinding()
            {
                var target = new TextBlock();

                target.Bind(TextBlock.TextProperty, new Binding
                {
                    Source = source,
                    Path = "(Grid.Row)",
                    TypeResolver = (_, name) => name == "Grid" ? typeof(Grid) : throw new NotSupportedException()
                });

                return new WeakReference(target);
            }

            var weakTarget = SetupBinding();

            CollectGarbage();
            Assert.False(weakTarget.IsAlive);
        }

        private static void CollectGarbage()
        {
            GC.Collect();
            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs();
            GC.Collect();
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly DirectProperty<Class1, string> FooProperty =
                AvaloniaProperty.RegisterDirect<Class1, string>(
                    "Foo",
                    o => o.Foo,
                    (o, v) => o.Foo = v,
                    unsetValue: "unset");

            private string _foo = "initial2";

            static Class1()
            {
            }

            public string Foo
            {
                get { return _foo; }
                set { SetAndRaise(FooProperty, ref _foo, value); }
            }

            public void DoSomething()
            {
            }
        }

        private sealed class Class2 : INotifyPropertyChanged
        {
            private string? _foo;

            public string? Foo
            {
                get => _foo;
                set
                {
                    if (_foo != value)
                    {
                        _foo = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
