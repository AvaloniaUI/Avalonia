using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Aliases
    {
        [Fact]
        public void Setting_Aliased_Property_Sets_Property()
        {
            var target = new Direct();

            target.SetValue(Direct.AliasProperty, "Test");

            Assert.Equal("Test", target.Foo);
        }

        [Fact]
        public void Setting_Property_Sets_Aliased_Property()
        {
            var target = new Direct
            {
                Foo = "Test"
            };

            Assert.Equal("Test", target.GetValue(Direct.AliasProperty));
        }

        [Fact]
        public void Binding_Aliased_Property_Binds_Property()
        {
            var subject = new Subject<string>();
            var target = new Direct();

            target.Bind(Direct.AliasProperty, subject);

            subject.OnNext("Test");

            Assert.Equal("Test", target.Foo);
        }

        [Fact]
        public void Checking_If_Property_Is_Set_Propagates_Through_Aliases()
        {
            var target = new Styled
            {
                Foo = "Test"
            };

            Assert.True(target.IsSet(Direct.AliasProperty));
        }

        private class Direct : AvaloniaObject
        {
            public static readonly DirectProperty<Direct, string> FooProperty =
                AvaloniaProperty.RegisterDirect<Direct, string>(nameof(Foo), o => o.Foo, (o, v) => o.Foo = v);

            public static readonly AliasedProperty<string> AliasProperty =
                AvaloniaProperty.RegisterAlias<Direct, string>(FooProperty, "Alias");

            private string _foo;
            public string Foo
            {
                get { return _foo; }
                set { SetAndRaise(FooProperty, ref _foo, value); }
            }
        }

        private class Styled : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Direct, string>(nameof(Foo));

            public static readonly AliasedProperty<string> AliasProperty =
                AvaloniaProperty.RegisterAlias<Direct, string>(FooProperty, "Alias");

            public string Foo
            {
                get { return GetValue(FooProperty); }
                set { SetValue(FooProperty, value); }
            }
        }
    }
}
