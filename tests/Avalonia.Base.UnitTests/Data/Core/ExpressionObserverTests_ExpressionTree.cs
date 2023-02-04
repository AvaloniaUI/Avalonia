using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data.Core;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class ExpressionObserverTests_ExpressionTree
    {
        [Fact]
        public async Task IdentityExpression_Creates_IdentityObserver()
        {
            var target = new object();

            var observer = ExpressionObserver.Create(target, o => o);

            Assert.Equal(target, await observer.Take(1));
            GC.KeepAlive(target);
        }

        [Fact]
        public async Task Property_Access_Expression_Observes_Property()
        {
            var target = new Class1();

            var observer = ExpressionObserver.Create(target, o => o.Foo);

            Assert.Null(await observer.Take(1));

            using (observer.Subscribe(_ => {}))
            {
                target.Foo = "Test"; 
            }

            Assert.Equal("Test", await observer.Take(1));

            GC.KeepAlive(target);
        }

        [Fact]
        public void Property_Access_Expression_Can_Set_Property()
        {
            var data = new Class1();
            var target = ExpressionObserver.Create(data, o => o.Foo);

            using (target.Subscribe(_ => { }))
            {
                Assert.True(target.SetValue("baz"));
            }

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Indexer_Accessor_Can_Read_Value()
        {
            var data = new[] { 1, 2, 3, 4 };

            var target = ExpressionObserver.Create(data, o => o[0]);

            Assert.Equal(data[0], await target.Take(1));
            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Indexer_List_Accessor_Can_Read_Value()
        {
            var data = new List<int> { 1, 2, 3, 4 };

            var target = ExpressionObserver.Create(data, o => o[0]);

            Assert.Equal(data[0], await target.Take(1));
            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Indexer_Accessor_Can_Read_Complex_Index()
        {
            var data = new Dictionary<object, object>();

            var key = new object();

            data.Add(key, new object());

            var target = ExpressionObserver.Create(data, o => o[key]);

            Assert.Equal(data[key], await target.Take(1));

            GC.KeepAlive(data);
        }

        [Fact]
        public void Indexer_Can_Set_Value()
        {
            var data = new[] { 1, 2, 3, 4 };

            var target = ExpressionObserver.Create(data, o => o[0]);

            using (target.Subscribe(_ => { }))
            {
                Assert.True(target.SetValue(2));
            }

            GC.KeepAlive(data);
        }

        [Fact]
        public async Task Inheritance_Casts_Should_Be_Ignored()
        {
            NotifyingBase test = new Class1 { Foo = "Test" };

            var target = ExpressionObserver.Create(test, o => ((Class1)o).Foo);

            Assert.Equal("Test", await target.Take(1));

            GC.KeepAlive(test);
        }

        [Fact]
        public void Convert_Casts_Should_Error()
        {
            var test = 1;

            Assert.Throws<ExpressionParseException>(() => ExpressionObserver.Create(test, o => (double)o));
        }

        [Fact]
        public async Task As_Operator_Should_Be_Ignored()
        {
            NotifyingBase test = new Class1 { Foo = "Test" };

            var target = ExpressionObserver.Create(test, o => (o as Class1).Foo);

            Assert.Equal("Test", await target.Take(1));

            GC.KeepAlive(test);
        }

        [Fact]
        public async Task Avalonia_Property_Indexer_Reads_Avalonia_Property_Value()
        {
            var test = new Class2();

            var target = ExpressionObserver.Create(test, o => o[Class2.FooProperty]);

            Assert.Equal("foo", await target.Take(1));

            GC.KeepAlive(test);
        }

        [Fact]
        public async Task Complex_Expression_Correctly_Parsed()
        {
            var test = new Class1 { Foo = "Test" };

            var target = ExpressionObserver.Create(test, o => o.Foo.Length);

            Assert.Equal(test.Foo.Length, await target.Take(1));

            GC.KeepAlive(test);
        }

        [Fact]
        public void Should_Get_Completed_Task_Value()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var data = new { Foo = Task.FromResult("foo") };
                var target = ExpressionObserver.Create(data, o => o.Foo.StreamBinding());
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));

                Assert.Equal(new[] { "foo" }, result);

                GC.KeepAlive(data);
            }
        }

        [Fact]
        public async Task Should_Create_Method_Binding()
        {
            var data = new Class3();
            var target = ExpressionObserver.Create(data, o => (Action)o.Method);
            var value = await target.Take(1);

            Assert.IsAssignableFrom<Delegate>(value);
            GC.KeepAlive(data);
        }

        private class Class1 : NotifyingBase
        {
            private string _foo;

            public string Foo
            {
                get { return _foo; }
                set
                {
                    _foo = value;
                    RaisePropertyChanged(nameof(Foo));
                }
            }
        }


        private class Class2 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class2, string>("Foo", defaultValue: "foo");

            public string ClrProperty { get; } = "clr-property";
        }

        private class Class3
        {
            public void Method() { }
        }
    }
}
