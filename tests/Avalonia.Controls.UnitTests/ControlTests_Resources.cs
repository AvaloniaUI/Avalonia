// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ControlTests_Resources
    {
        [Fact]
        public void FindResource_Should_Find_Control_Resource()
        {
            var target = new Control
            {
                Resources =
                {
                    { "foo", "foo-value" },
                }
            };

            Assert.Equal("foo-value", target.FindResource("foo"));
        }

        [Fact]
        public void FindResource_Should_Find_Control_Resource_In_Parent()
        {
            Control target;

            var root = new Decorator
            {
                Resources =
                {
                    { "foo", "foo-value" },
                },
                Child = target = new Control(),
            };

            Assert.Equal("foo-value", target.FindResource("foo"));
        }

        [Fact]
        public void FindResource_Should_Find_Application_Resource()
        {
            Control target;

            var app = new Application
            {
                Resources =
                {
                    { "foo", "foo-value" },
                },
            };

            var root = new TestRoot
            {
                Child = target = new Control(),
                StylingParent = app,
            };

            Assert.Equal("foo-value", target.FindResource("foo"));
        }

        [Fact]
        public void FindResource_Should_Find_Style_Resource()
        {
            var target = new Control
            {
                Styles =
                {
                    new Style
                    {
                        Resources =
                        {
                            { "foo", "foo-value" },
                        }
                    }
                },
                Resources =
                {
                    { "bar", "bar-value" },
                },
            };

            Assert.Equal("foo-value", target.FindResource("foo"));
        }

        [Fact]
        public void FindResource_Should_Find_Styles_Resource()
        {
            var target = new Control
            {
                Styles =
                {
                    new Styles
                    {
                        Resources =
                        {
                            { "foo", "foo-value" },
                        }
                    }
                },
                Resources =
                {
                    { "bar", "bar-value" },
                },
            };

            Assert.Equal("foo-value", target.FindResource("foo"));
        }

        [Fact]
        public void FindResource_Should_Find_Application_Style_Resource()
        {
            Control target;

            var app = new Application
            {
                Styles =
                {
                    new Style
                    {
                        Resources =
                        {
                            { "foo", "foo-value" },
                        },
                    }
                },
                Resources =
                {
                    { "bar", "bar-value" },
                },
            };

            var root = new TestRoot
            {
                Child = target = new Control(),
                StylingParent = app,
            };

            Assert.Equal("foo-value", target.FindResource("foo"));
        }
    }
}
