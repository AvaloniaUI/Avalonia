// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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

        [Fact]
        public void AddOwnered_Property_Retains_Validation()
        {
            var target = new Class2();

            Assert.Throws<IndexOutOfRangeException>(() => target.SetValue(Class2.FooProperty, "throw"));
        }

        [Fact]
        public void AvaloniaProperty_Initialized_Is_Called_For_Attached_Property()
        {
            bool raised = false;

            using (Class1.FooProperty.Initialized.Subscribe(x => raised = true))
            {
                new Class3();
            }

            Assert.True(raised);
        }

        private class Base : AvaloniaObject
        {
        }

        private class Class1 : Base
        {
            public static readonly AttachedProperty<string> FooProperty =
                AvaloniaProperty.RegisterAttached<Class1, Base, string>(
                    "Foo",
                    "foodefault",
                    validate: ValidateFoo);

            private static string ValidateFoo(AvaloniaObject arg1, string arg2)
            {
                if (arg2 == "throw")
                {
                    throw new IndexOutOfRangeException();
                }

                return arg2;
            }
        }

        private class Class2 : Base
        {
            public static readonly AttachedProperty<string> FooProperty =
                Class1.FooProperty.AddOwner<Class2>();
        }

        private class Class3 : Base
        {
        }
    }
}
