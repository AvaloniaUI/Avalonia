using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class BindingTests_ElementName
    {
        [Fact]
        public void Should_Bind_To_Element_Path()
        {
            TextBlock target;
            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Name = "source",
                            Text = "foo",
                        },
                        (target = new TextBlock
                        {
                            Name = "target",
                        })
                    }
                }
            };

            root.RegisterChildrenNames();
            
            var binding = new Binding
            {
                ElementName = "source",
                Path = "Text",
                NameScope = new WeakReference<INameScope>(NameScope.GetNameScope(root))
            };

            target.Bind(TextBox.TextProperty, binding);

            Assert.Equal("foo", target.Text);
        }

        [Fact]
        public void Should_Bind_To_Element()
        {
            TextBlock source;
            ContentControl target;

            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        (source = new TextBlock
                        {
                            Name = "source",
                            Text = "foo",
                        }),
                        (target = new ContentControl
                        {
                            Name = "target",
                        })
                    }
                }
            };
            root.RegisterChildrenNames();

            var binding = new Binding
            {
                ElementName = "source",
                NameScope = new WeakReference<INameScope>(NameScope.GetNameScope(root))
            };

            target.Bind(ContentControl.ContentProperty, binding);

            Assert.Same(source, target.Content);
        }

        [Fact]
        public void Should_Bind_To_Later_Added_Element_Path()
        {
            TextBlock target;
            StackPanel stackPanel;

            var root = new TestRoot
            {
                Child = stackPanel = new StackPanel
                {
                    Children =
                    {
                        (target = new TextBlock
                        {
                            Name = "target",
                        }),
                    }
                }
            };
            root.RegisterChildrenNames();
            
            var binding = new Binding
            {
                ElementName = "source",
                Path = "Text",
                NameScope = new WeakReference<INameScope>(NameScope.GetNameScope(root))
            };

            target.Bind(TextBox.TextProperty, binding);

            stackPanel.Children.Add(new TextBlock
            {
                Name = "source",
                Text = "foo",
            });
            root.RegisterChildrenNames();
            Assert.Equal("foo", target.Text);
        }

        [Fact]
        public void Should_Bind_To_Later_Added_Element()
        {
            ContentControl target;
            StackPanel stackPanel;

            var root = new TestRoot
            {
                Child = stackPanel = new StackPanel
                {
                    Children =
                    {
                        (target = new ContentControl
                        {
                            Name = "target",
                        }),
                    }
                }
            };
            root.RegisterChildrenNames();

            var binding = new Binding
            {
                ElementName = "source",
                NameScope = new WeakReference<INameScope>(NameScope.GetNameScope(root))
            };

            target.Bind(ContentControl.ContentProperty, binding);

            var source = new TextBlock
            {
                Name = "source",
                Text = "foo",
            };

            stackPanel.Children.Add(source);
            root.RegisterChildrenNames();

            Assert.Same(source, target.Content);
        }
    }
}
