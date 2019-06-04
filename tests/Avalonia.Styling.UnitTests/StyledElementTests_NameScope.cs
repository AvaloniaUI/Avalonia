// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class StyledElementTests_NameScope
    {
        [Fact]
        public void Controls_Should_Register_With_NameScope()
        {
            var root = new TestRoot
            {
                Child = new Border
                {
                    Name = "foo",
                    Child = new Border
                    {
                        Name = "bar",
                    }
                }
            };

            root.ApplyTemplate();

            Assert.Same(root.FindControl<Border>("foo"), root.Child);
            Assert.Same(root.FindControl<Border>("bar"), ((Border)root.Child).Child);
        }

        [Fact]
        public void Control_Should_Unregister_With_NameScope()
        {
            var root = new TestRoot
            {
                Child = new Border
                {
                    Name = "foo",
                    Child = new Border
                    {
                        Name = "bar",
                    }
                }
            };

            root.Child = null;

            Assert.Null(root.FindControl<Border>("foo"));
            Assert.Null(root.FindControl<Border>("bar"));
        }

        [Fact]
        public void Control_Should_Not_Register_With_Template_NameScope()
        {
            var root = new TestTemplatedRoot
            {
                Content = new Border
                {
                    Name = "foo",
                }
            };

            root.ApplyTemplate();

            Assert.Null(NameScope.GetNameScope((StyledElement)root.Presenter).Find("foo"));
        }
    }
}
