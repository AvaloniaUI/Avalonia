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
            var target = new Class1();

            target.SetValue(Class1.AliasProperty, "Test");

            Assert.Equal("Test", target.Foo);
        }

        [Fact]
        public void Setting_Property_Sets_Aliased_Property()
        {
            var target = new Class1
            {
                Foo = "Test"
            };

            Assert.Equal("Test", target.GetValue(Class1.AliasProperty));
        }

        [Fact]
        public void Binding_Aliased_Property_Binds_Property()
        {
            var subject = new Subject<string>();
            var target = new Class1();

            target.Bind(Class1.AliasProperty, subject);

            subject.OnNext("Test");

            Assert.Equal("Test", target.Foo);
        }

        [Fact]
        public void Checking_If_Property_Is_Set_Propagates_Through_Aliases()
        {
            var target = new Class1
            {
                Foo = "Test"
            };

            Assert.True(target.IsSet(Class1.AliasProperty));
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly DirectProperty<Class1, string> FooProperty =
                AvaloniaProperty.RegisterDirect<Class1, string>(nameof(Foo), o => o.Foo, (o, v) => o.Foo = v);

            public static readonly AliasedProperty<string> AliasProperty =
                AvaloniaProperty.RegisterAlias<Class1, string>(FooProperty, "Alias");

            private string _foo;
            public string Foo
            {
                get { return _foo; }
                set { SetAndRaise(FooProperty, ref _foo, value); }
            }
        }
    }
}
