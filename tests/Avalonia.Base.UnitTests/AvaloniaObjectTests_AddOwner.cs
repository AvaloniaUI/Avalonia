// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_AddOwner
    {
        [Fact]
        public void AddOwnered_Property_Retains_Default_Value()
        {
            var target = new Class2();

            Assert.Equal("foodefault", target.GetValue(Class2.FooProperty));
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>(
                    "Foo",
                    "foodefault");
        }

        private class Class2 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                Class1.FooProperty.AddOwner<Class2>();
        }
    }
}
