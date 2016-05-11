// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class NameScopeTests
    {
        [Fact]
        public void Register_Registers_Element()
        {
            var target = new NameScope();
            var element = new object();

            target.Register("foo", element);

            Assert.Same(element, target.Find("foo"));
        }

        [Fact]
        public void Unregister_Unregisters_Element()
        {
            var target = new NameScope();
            var element = new object();

            target.Register("foo", element);
            target.Unregister("foo");

            Assert.Null(target.Find("foo"));
        }

        [Fact]
        public void Cannot_Register_New_Element_With_Existing_Name()
        {
            var target = new NameScope();

            target.Register("foo", new object());
            Assert.Throws<ArgumentException>(() => target.Register("foo", new object()));
        }

        [Fact]
        public void Can_Register_Same_Element_More_Than_Once()
        {
            var target = new NameScope();
            var element = new object();

            target.Register("foo", element);
            target.Register("foo", element);

            Assert.Same(element, target.Find("foo"));
        }
    }
}
