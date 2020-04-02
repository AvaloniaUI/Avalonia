using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
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

        [Fact]
        public void Should_Update_When_Detached_And_Attached_To_Visual_Tree_With_BindingPath()
        {
            TextBlock target;
            Decorator decorator1;
            Decorator decorator2;

            var viewModel = new { Value = "Foo" };

            var root1 = new TestRoot
            {
                Child = decorator1 = new Decorator
                {
                    Name = "decorator1",
                    Child = target = new TextBlock(),
                },
                DataContext = viewModel
            };

            var root2 = new TestRoot
            {
                Child = decorator2 = new Decorator
                {
                    Name = "decorator2",
                },
                DataContext = viewModel
            };

            var binding = new Binding
            {
                Path = "DataContext.Value",
                RelativeSource = new RelativeSource
                {
                    AncestorType = typeof(Decorator),
                }
            };

            target.Bind(TextBox.TextProperty, binding);
            Assert.Equal("Foo", target.Text);

            decorator1.Child = null;
            Assert.Null(target.Text);

            decorator2.Child = target;
            Assert.Equal("Foo", target.Text);
        }

        [Fact]
        public void Should_Update_When_Detached_And_Attached_To_Visual_Tree_With_ComplexBindingPath()
        {
            TextBlock target;
            Decorator decorator1;
            Decorator decorator2;

            var vm = new { Foo = new  { Value = "Foo" } };

            var root1 = new TestRoot
            {
                Child = decorator1 = new Decorator
                {
                    Name = "decorator1",
                    Child = target = new TextBlock(),
                },
                DataContext = vm
            };

            var root2 = new TestRoot
            {
                Child = decorator2 = new Decorator
                {
                    Name = "decorator2",
                },
                DataContext = vm
            };

            var binding = new Binding
            {
                Path = "DataContext.Foo.Value",
                RelativeSource = new RelativeSource
                {
                    AncestorType = typeof(Decorator),
                }
            };

            target.Bind(TextBox.TextProperty, binding);
            Assert.Equal("Foo", target.Text);

            decorator1.Child = null;
            Assert.Null(target.Text);

            decorator2.Child = target;
            Assert.Equal("Foo", target.Text);
        }
    }
}
