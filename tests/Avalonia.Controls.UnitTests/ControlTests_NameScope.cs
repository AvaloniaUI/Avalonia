// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ControlTests_NameScope
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

            Assert.Null(NameScope.GetNameScope((Control)root.Presenter).Find("foo"));
        }

        [Fact]
        public void Control_That_Is_NameScope_Should_Register_With_Parent_NameScope()
        {
            UserControl userControl;
            var root = new TestTemplatedRoot
            {
                Content = userControl = new UserControl
                {
                    Name = "foo",
                }
            };

            root.ApplyTemplate();

            Assert.Same(userControl, root.FindControl<UserControl>("foo"));
            Assert.Same(userControl, userControl.FindControl<UserControl>("foo"));
        }
    }
}
