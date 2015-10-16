// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Perspex.LogicalTree;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class TreeViewTests
    {
        [Fact]
        public void Items_Should_Be_Created()
        {
            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = CreateTestTreeData(),
                DataTemplates = CreateNodeDataTemplate(),
            };

            target.ApplyTemplate();

            Assert.Equal(new[] { "Root" }, ExtractItemContent(target, 0));
            Assert.Equal(new[] { "Child1", "Child2" }, ExtractItemContent(target, 1));
            Assert.Equal(new[] { "Grandchild2a" }, ExtractItemContent(target, 2));
        }

        [Fact]
        public void LogicalChildren_Should_Be_Set()
        {
            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            target.ApplyTemplate();

            Assert.Equal(3, target.GetLogicalChildren().Count());

            foreach (var child in target.GetLogicalChildren())
            {
                Assert.IsType<TreeViewItem>(child);
            }
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            var items = new object[]
            {
                "Foo",
                new Node { Value = "Bar" },
                new TextBlock { Text = "Baz" },
                new TreeViewItem { Header = "Qux" },
            };

            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                DataContext = "Base",
                DataTemplates = new DataTemplates
                {
                    new FuncDataTemplate<Node>(x => new Button { Content = x })
                },
                Items = items,
            };

            target.ApplyTemplate();

            var dataContexts = target.Presenter.Panel.Children
                .Cast<Control>()
                .Select(x => x.DataContext)
                .ToList();

            Assert.Equal(
                new object[] { items[0], items[1], "Base", "Base" },
                dataContexts);
        }

        private IList<Node> CreateTestTreeData()
        {
            return new[]
            {
                new Node
                {
                    Value = "Root",
                    Children = new[]
                    {
                        new Node
                        {
                            Value = "Child1",
                        },
                        new Node
                        {
                            Value = "Child2",
                            Children = new[]
                            {
                                new Node
                                {
                                    Value = "Grandchild2a",
                                },
                            },
                        },
                    }
                }
            };
        }

        private DataTemplates CreateNodeDataTemplate()
        {
            return new DataTemplates
            {
                new FuncTreeDataTemplate<Node>(
                    x => new TextBlock { Text = x.Value },
                    x => x.Children),
            };
        }

        private ControlTemplate CreateTreeViewTemplate()
        {
            return new ControlTemplate<TreeView>(parent => new ItemsPresenter
            {
                Name = "itemsPresenter",
                [~ItemsPresenter.ItemsProperty] = parent[~ItemsControl.ItemsProperty],
            });
        }

        private ControlTemplate CreateTreeViewItemTemplate()
        {
            return new ControlTemplate<TreeViewItem>(parent => new ItemsPresenter
            {
                Name = "itemsPresenter",
                [~ItemsPresenter.ItemsProperty] = parent[~ItemsControl.ItemsProperty],
            });
        }

        private List<string> ExtractItemContent(TreeView tree, int level)
        {
            return ExtractItemContent(tree.Presenter.Panel, 0, level)
                .Select(x => x.Header)
                .OfType<TextBlock>()
                .Select(x => x.Text)
                .ToList();
        }

        private IEnumerable<TreeViewItem> ExtractItemContent(IPanel panel, int currentLevel, int level)
        {
            foreach (TreeViewItem container in panel.Children)
            {
                if (container.Template == null)
                {
                    container.Template = CreateTreeViewItemTemplate();
                    container.ApplyTemplate();
                }

                if (currentLevel == level)
                {
                    yield return container;
                }
                else
                {
                    foreach (var child in ExtractItemContent(container.Presenter.Panel, currentLevel + 1, level))
                    {
                        yield return child;
                    }
                }
            }
        }

        private class Node
        {
            public string Value { get; set; }
            public IList<Node> Children { get; set; }
        }
    }
}
