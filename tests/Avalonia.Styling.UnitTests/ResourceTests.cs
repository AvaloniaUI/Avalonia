// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Controls;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class ResourceTests
    {
        [Fact]
        public void FindStyleResource_Should_Find_Correct_Resource()
        {
            Border target;

            var tree = new Decorator
            {
                Styles = new Styles
                {
                    new Style
                    {
                        Resources = new StyleResources
                        {
                            { "Foo", "foo resource" },
                            { "Bar", "overridden" },
                        }
                    }
                },
                Child = target = new Border
                {
                    Styles = new Styles
                    {
                        new Style
                        {
                            Resources = new StyleResources
                            {
                                { "Bar", "again overridden" },
                            }
                        },
                        new Style
                        {
                            Resources = new StyleResources
                            {
                                { "Bar", "bar resource" },
                            }
                        }
                    }
                }
            };

            Assert.Equal("foo resource", target.FindStyleResource("Foo"));
            Assert.Equal("bar resource", target.FindStyleResource("Bar"));
        }

        [Fact]
        public void FindStyleResource_Should_Return_UnsetValue_For_Not_Found()
        {
            Border target;

            var tree = target = new Border
            {
                Styles = new Styles
                    {
                        new Style
                        {
                            Resources = new StyleResources
                            {
                                { "Foo", "foo" },
                            }
                        },
                    }
            };

            Assert.Equal(AvaloniaProperty.UnsetValue, target.FindStyleResource("Baz"));
        }
    }
}
