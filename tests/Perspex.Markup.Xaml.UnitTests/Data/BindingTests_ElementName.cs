// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Markup.Xaml.Data;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Data
{
    public class BindingTests_ElementName
    {
        [Fact]
        public void Should_Bind_To_Element()
        {
            TextBlock target;
            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children = new Controls.Controls
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

            var binding = new Binding
            {
                ElementName = "source",
                SourcePropertyPath = "Text",
            };

            binding.Bind(target, TextBlock.TextProperty);

            Assert.Equal("foo", target.Text);
        }

        [Fact]
        public void Should_Bind_To_Later_Added_Element()
        {
            TextBlock target;
            StackPanel stackPanel;

            var root = new TestRoot
            {
                Child = stackPanel = new StackPanel
                {
                    Children = new Controls.Controls
                    {
                        (target = new TextBlock
                        {
                            Name = "target",
                        }),
                    }
                }
            };

            var binding = new Binding
            {
                ElementName = "source",
                SourcePropertyPath = "Text",
            };

            binding.Bind(target, TextBlock.TextProperty);

            stackPanel.Children.Add(new TextBlock
            {
                Name = "source",
                Text = "foo",
            });

            Assert.Equal("foo", target.Text);
        }
    }
}
