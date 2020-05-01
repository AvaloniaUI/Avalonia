using System;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Attached
    {
        [Fact]
        public void AddOwnered_Property_Retains_Default_Value()
        {
            var target = new Class2();

            Assert.Equal("foodefault", target.GetValue(Class2.FooProperty));
        }

        private class Base : AvaloniaObject
        {
        }

        private class Class1 : Base
        {
            public static readonly AttachedProperty<string> FooProperty =
                AvaloniaProperty.RegisterAttached<Class1, Base, string>(
                    "Foo",
                    "foodefault");
        }

        private class Class2 : Base
        {
            public static readonly AttachedProperty<string> FooProperty =
                Class1.FooProperty.AddOwner<Class2>();
        }
    }
}
