using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class ExpressionObserverTests_SetValue
    {
        [Fact]
        public void Should_Set_Simple_Property_Value()
        {
            var data = new { Foo = "foo" };
            var target = ExpressionObserver.Create(data, o => o.Foo);

            using (target.Subscribe(_ => { }))
            {
                target.SetValue("bar");
            }

            Assert.Equal("foo", data.Foo);
        }

        [Fact]
        public void Should_Set_Value_On_Simple_Property_Chain()
        {
            var data = new Class1 { Foo = new Class2 { Bar = "bar" } };
            var target = ExpressionObserver.Create(data, o => o.Foo.Bar);


            using (target.Subscribe(_ => { }))
            {
                target.SetValue("foo");
            }

            Assert.Equal("foo", data.Foo.Bar);
        }

        [Fact]
        public void Should_Not_Try_To_Set_Value_On_Broken_Chain()
        {
            var data = new Class1 { Foo = new Class2 { Bar = "bar" } };
            var target = ExpressionObserver.Create(data, o => o.Foo.Bar);

            // Ensure the ExpressionObserver's subscriptions are kept active.
            using (target.OfType<string>().Subscribe(x => { }))
            {
                data.Foo = null;
                Assert.False(target.SetValue("foo"));
            }

        }

        /// <summary>
        /// Test for #831 - Bound properties are incorrectly updated when changing tab items.
        /// </summary>
        /// <remarks>
        /// There was a bug whereby pushing a null as the ExpressionObserver root didn't update
        /// the leaf node, cauing a subsequent SetValue to update an object that should have become
        /// unbound.
        /// </remarks>
        [Fact]
        public void Pushing_Null_To_RootObservable_Updates_Leaf_Node()
        {
            var data = new Class1 { Foo = new Class2 { Bar = "bar" } };
            var rootObservable = new BehaviorSubject<Class1>(data);
            var target = ExpressionObserver.Create(rootObservable, o => o.Foo.Bar);

            using (target.Subscribe(_ => { }))
            {
                rootObservable.OnNext(null);
                target.SetValue("baz");
                Assert.Equal("bar", data.Foo.Bar);
            }

        }

        private class Class1 : NotifyingBase
        {
            private Class2 _foo;

            public Class2 Foo
            {
                get { return _foo; }
                set
                {
                    _foo = value;
                    RaisePropertyChanged(nameof(Foo));
                }
            }
        }

        private class Class2 : NotifyingBase
        {
            private string _bar;

            public string Bar
            {
                get { return _bar; }
                set
                {
                    _bar = value;
                    RaisePropertyChanged(nameof(Bar));
                }
            }
        }
    }
}
