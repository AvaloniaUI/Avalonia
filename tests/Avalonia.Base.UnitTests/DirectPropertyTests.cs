using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class DirectPropertyTests
    {
        [Fact]
        public void IsDirect_Property_Returns_True()
        {
            var target = new DirectProperty<Class1, string>(
                "test", 
                o => null, 
                null,
                new DirectPropertyMetadata<string>());

            Assert.True(target.IsDirect);
        }

        [Fact]
        public void AddOwnered_Property_Should_Equal_Original()
        {
            var p1 = Class1.FooProperty;
            var p2 = p1.AddOwner<Class2>(o => null, (o, v) => { });

            Assert.NotSame(p1, p2);
            Assert.True(p1.Equals(p2));
            Assert.Equal(p1.GetHashCode(), p2.GetHashCode());
            Assert.True(p1 == p2);
        }

        [Fact]
        public void AddOwnered_Property_Should_Have_OwnerType_Set()
        {
            var p1 = Class1.FooProperty;
            var p2 = p1.AddOwner<Class2>(o => null, (o, v) => { });

            Assert.Equal(typeof(Class2), p2.OwnerType);
        }

        [Fact]
        public void AddOwnered_Properties_Should_Share_Observables()
        {
            var p1 = Class1.FooProperty;
            var p2 = p1.AddOwner<Class2>(o => null, (o, v) => { });

            Assert.Same(p1.Changed, p2.Changed);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly DirectProperty<Class1, string> FooProperty =
                AvaloniaProperty.RegisterDirect<Class1, string>(nameof(Foo), o => o.Foo, (o, v) => o.Foo = v);

            private string _foo = "foo";

            public string Foo
            {
                get { return _foo; }
                set { SetAndRaise(FooProperty, ref _foo, value); }
            }
        }

        private class Class2 : AvaloniaObject
        {
        }
    }
}
