// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Markup.Xaml.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Data
{
    public class BindingTests_RelativeSource
    {
        [Fact]
        public void Should_Bind_To_First_Ancestor()
        {
            TextBlock target;
            var root = new TestRoot
            {
                Child = new Decorator
                {
                    Name = "decorator",
                    Child = target = new TextBlock(),
                },
            };

            var binding = new Binding
            {
                Path = "Name",
                RelativeSource = new RelativeSource
                {
                    AncestorType = typeof(Decorator),
                }
            };

            target.Bind(TextBox.TextProperty, binding);
            Assert.Equal("decorator", target.Text);
        }

        [Fact]
        public void Should_Bind_To_Second_Ancestor()
        {
            TextBlock target;
            var root = new TestRoot
            {
                Child = new Decorator
                {
                    Name = "decorator1",
                    Child = new Decorator
                    {
                        Name = "decorator2",
                        Child = target = new TextBlock(),
                    }
                },
            };

            var binding = new Binding
            {
                Path = "Name",
                RelativeSource = new RelativeSource
                {
                    AncestorType = typeof(Decorator),
                    AncestorLevel = 2,
                }
            };

            target.Bind(TextBox.TextProperty, binding);
            Assert.Equal("decorator1", target.Text);
        }

        [Fact]
        public void Should_Bind_To_Derived_Ancestor_Type()
        {
            TextBlock target;
            var root = new TestRoot
            {
                Child = new Border
                {
                    Name = "border",
                    Child = target = new TextBlock(),
                },
            };

            var binding = new Binding
            {
                Path = "Name",
                RelativeSource = new RelativeSource
                {
                    AncestorType = typeof(Decorator),
                }
            };

            target.Bind(TextBox.TextProperty, binding);
            Assert.Equal("border", target.Text);
        }

        [Fact]
        public void Should_Produce_Null_If_Ancestor_Not_Found()
        {
            TextBlock target;
            var root = new TestRoot
            {
                Child = new Decorator
                {
                    Name = "decorator",
                    Child = target = new TextBlock(),
                },
            };

            var binding = new Binding
            {
                Path = "Name",
                RelativeSource = new RelativeSource
                {
                    AncestorType = typeof(Decorator),
                    AncestorLevel = 2,
                }
            };

            target.Bind(TextBox.TextProperty, binding);
            Assert.Null(target.Text);
        }

        [Fact]
        public void Should_Update_When_Detached_And_Attached_To_Visual_Tree()
        {
            TextBlock target;
            Decorator decorator1;
            Decorator decorator2;
            var root1 = new TestRoot
            {
                Child = decorator1 = new Decorator
                {
                    Name = "decorator1",
                    Child = target = new TextBlock(),
                },
            };

            var root2 = new TestRoot
            {
                Child = decorator2 = new Decorator
                {
                    Name = "decorator2",
                },
            };

            var binding = new Binding
            {
                Path = "Name",
                RelativeSource = new RelativeSource
                {
                    AncestorType = typeof(Decorator),
                }
            };

            target.Bind(TextBox.TextProperty, binding);
            Assert.Equal("decorator1", target.Text);

            decorator1.Child = null;
            Assert.Null(target.Text);

            decorator2.Child = target;
            Assert.Equal("decorator2", target.Text);
        }
    }
}
