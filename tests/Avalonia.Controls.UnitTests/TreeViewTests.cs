using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests
{
    public class TreeViewTests : ScopedTestBase
    {
        private readonly MouseTestHelper _mouse = new();

        [Fact]
        public void Items_Should_Be_Created()
        {
            using var app = Start();
            var target = CreateTarget();

            Assert.Equal(new[] { "Root" }, ExtractItemHeader(target, 0));
            Assert.Equal(new[] { "Child1", "Child2", "Child3" }, ExtractItemHeader(target, 1));
            Assert.Equal(new[] { "Grandchild2a" }, ExtractItemHeader(target, 2));
        }

        [Fact]
        public void Items_Should_Be_Created_Using_ItemTemplate_If_Present()
        {
            using var app = Start();
            var itemTemplate = new FuncTreeDataTemplate<Node>(
                (_, _) => new Canvas(),
                x => x.Children);
            var target = CreateTarget(itemTemplate: itemTemplate);

            var items = target.GetRealizedTreeContainers()
                .OfType<TreeViewItem>()
                .ToList();

            Assert.Equal(5, items.Count);
            Assert.All(items, x => Assert.IsType<Canvas>(x.HeaderPresenter?.Child));
        }

        [Fact]
        public void Items_Should_Be_Created_Using_ItemTemplate_If_Present_2()
        {
            using var app = Start();
            var itemTemplate = new TreeDataTemplate
            {
                Content = (IServiceProvider? _) => new TemplateResult<Control>(new Canvas(), new NameScope()),
                ItemsSource = new Binding("Children"),
            };
            var target = CreateTarget(itemTemplate: itemTemplate);

            var items = target.GetRealizedTreeContainers()
                .OfType<TreeViewItem>()
                .ToList();

            Assert.Equal(5, items.Count);
            Assert.All(items, x => Assert.IsType<Canvas>(x.HeaderPresenter?.Child));
        }

        [Fact]
        public void Items_Should_Be_Created_Using_ItemConatinerTheme_If_Present()
        {
            var theme = CreateTreeViewItemControlTheme();
            var itemTemplate = new FuncTreeDataTemplate<Node>(
                (_, _) => new Canvas(),
                x => x.Children);

            var target = CreateTarget(
                itemContainerTheme: theme,
                itemTemplate: itemTemplate);

            var items = target.GetRealizedTreeContainers()
                .OfType<TreeViewItem>()
                .ToList();

            Assert.Equal(5, items.Count);
            Assert.All(items, x => Assert.Same(theme, x.ItemContainerTheme));
        }

        [Fact]
        public void Finds_Correct_DataTemplate_When_Application_DataTemplate_Is_Present()
        {
            // #10398
            using var app = Start();

            Avalonia.Application.Current!.DataTemplates.Add(new FuncDataTemplate<object>((x, _) => new Canvas()));
            AvaloniaLocator.CurrentMutable.Bind<IGlobalDataTemplates>().ToConstant(Avalonia.Application.Current);

            var target = CreateTarget();

            Assert.Equal(new[] { "Root" }, ExtractItemHeader(target, 0));
            Assert.Equal(new[] { "Child1", "Child2", "Child3" }, ExtractItemHeader(target, 1));
            Assert.Equal(new[] { "Grandchild2a" }, ExtractItemHeader(target, 2));
        }

        [Fact]
        public void Root_ItemContainerGenerator_Containers_Should_Be_Root_Containers()
        {
            using var app = Start();
            var target = CreateTarget();

            var container = (TreeViewItem)target.GetRealizedContainers().Single();
            var header = Assert.IsType<TextBlock>(container.HeaderPresenter?.Child);
            Assert.Equal("Root", header?.Text);
        }

        [Fact]
        public void Root_TreeContainerFromItem_Should_Return_Descendant_Item()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            var container = target.TreeContainerFromItem(data[0].Children[1].Children[0]);

            Assert.NotNull(container);

            var header = ((TreeViewItem)container).HeaderPresenter!;
            var headerContent = ((TextBlock)header.Child!).Text;

            Assert.Equal("Grandchild2a", headerContent);
        }

        [Fact]
        public void Clicking_Item_Should_Select_It()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            var item = data[0].Children[1].Children[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

            _mouse.Click(container);

            Assert.Equal(item, target.SelectedItem);
            Assert.True(container.IsSelected);
        }

        [Fact]
        public void Clicking_WithControlModifier_Selected_Item_Should_Deselect_It()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            var item = data[0].Children[1].Children[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

            target.SelectedItem = item;

            Assert.True(container.IsSelected);

            _mouse.Click(container, modifiers: KeyModifiers.Control);

            Assert.Null(target.SelectedItem);
            Assert.False(container.IsSelected);
        }

        [Fact]
        public void Clicking_WithControlModifier_Not_Selected_Item_Should_Select_It()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            var item1 = data[0].Children[1].Children[0];
            var container1 = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item1));

            var item2 = data[0].Children[1];
            var container2 = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item2));

            target.SelectedItem = item1;

            Assert.True(container1.IsSelected);

            _mouse.Click(container2, modifiers: KeyModifiers.Control);

            Assert.Equal(item2, target.SelectedItem);
            Assert.False(container1.IsSelected);
            Assert.True(container2.IsSelected);
        }

        [Fact]
        public void Clicking_WithControlModifier_Selected_Item_Should_Deselect_And_Remove_From_SelectedItems()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);
            var rootNode = data[0];
            var item1 = rootNode.Children[0];
            var item2 = rootNode.Children.Last();
            var item1Container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item1));
            var item2Container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item2));

            ClickContainer(item1Container, KeyModifiers.Control);
            Assert.True(item1Container.IsSelected);

            ClickContainer(item2Container, KeyModifiers.Control);
            Assert.True(item2Container.IsSelected);

            Assert.Equal(new[] { item1, item2 }, target.SelectedItems.OfType<Node>());

            ClickContainer(item1Container, KeyModifiers.Control);
            Assert.False(item1Container.IsSelected);

            Assert.DoesNotContain(item1, target.SelectedItems.OfType<Node>());
        }

        [Fact]
        public void Clicking_WithShiftModifier_DownDirection_Should_Select_Range_Of_Items()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);
            var rootNode = data[0];
            var from = rootNode.Children[0];
            var to = rootNode.Children.Last();
            var fromContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
            var toContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

            ClickContainer(fromContainer, KeyModifiers.None);
            Assert.True(fromContainer.IsSelected);

            ClickContainer(toContainer, KeyModifiers.Shift);
            AssertAllChildContainersSelected(target, rootNode);
        }

        [Fact]
        public void Clicking_WithShiftModifier_UpDirection_Should_Select_Range_Of_Items()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);
            var rootNode = data[0];
            var from = rootNode.Children.Last();
            var to = rootNode.Children[0];
            var fromContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
            var toContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

            ClickContainer(fromContainer, KeyModifiers.None);
            Assert.True(fromContainer.IsSelected);

            ClickContainer(toContainer, KeyModifiers.Shift);
            AssertAllChildContainersSelected(target, rootNode);
        }

        [Fact]
        public void Clicking_First_Item_Of_SelectedItems_Should_Select_Only_It()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);
            var rootNode = data[0];
            var from = rootNode.Children.Last();
            var to = rootNode.Children[0];
            var fromContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
            var toContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

            ClickContainer(fromContainer, KeyModifiers.None);
            ClickContainer(toContainer, KeyModifiers.Shift);
            AssertAllChildContainersSelected(target, rootNode);

            ClickContainer(fromContainer, KeyModifiers.None);
            Assert.True(fromContainer.IsSelected);

            foreach (var child in rootNode.Children)
            {
                if (child == from)
                    continue;

                var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(child));
                Assert.False(container.IsSelected);
            }
        }

        [Fact]
        public void Double_Clicking_Item_Header_Should_Expand_It()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            CollapseAll(target);

            var item = data[0].Children[1];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
            var header = container.HeaderPresenter?.Child;

            Assert.False(container.IsExpanded);
            Assert.NotNull(header);

            _mouse.DoubleClick(header);

            Assert.True(container.IsExpanded);
        }

        [Fact]
        public void Double_Clicking_Item_Header_With_No_Children_Does_Not_Expand_It()
        {
            using var app = Start();
            {
                var data = CreateTestTreeData();
                var target = CreateTarget(data: data);

                CollapseAll(target);

                var item = data[0].Children[1].Children[0];
                var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
                var header = container.HeaderPresenter?.Child;

                Assert.False(container.IsExpanded);
                Assert.NotNull(header);

                _mouse.DoubleClick(header);

                Assert.False(container.IsExpanded);
            }
        }

        [Fact]
        public void Double_Clicking_Item_Header_Should_Collapse_It()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var item = data[0].Children[1];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
            var header = container.HeaderPresenter?.Child;

            Assert.True(container.IsExpanded);
            Assert.NotNull(header);

            _mouse.DoubleClick(header);

            Assert.False(container.IsExpanded);
        }

        [Fact]
        public void Enter_Key_Should_Collapse_TreeViewItem()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var item = data[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
            var header = container.HeaderPresenter?.Child;

            Assert.True(container.IsExpanded);
            Assert.NotNull(header);

            container.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Enter,
            });

            Assert.False(container.IsExpanded);
        }

        [Fact]
        public void Enter_Plus_Ctrl_Key_Should_Collapse_TreeViewItem_Recursively()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            var item = data[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
            var header = container.HeaderPresenter?.Child;

            Assert.True(container.IsExpanded);
            Assert.NotNull(header);

            container.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Enter,
                KeyModifiers = KeyModifiers.Control,
            });

            Assert.False(container.IsExpanded);

            AssertEachItemWithChildrenIsCollapsed(item);

            void AssertEachItemWithChildrenIsCollapsed(Node node)
            {
                var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(node));

                if (node.Children?.Count > 0)
                {
                    Assert.False(container.IsExpanded);
                    foreach (var c in node.Children)
                    {
                        AssertEachItemWithChildrenIsCollapsed(c);
                    }
                }
                else
                {
                    Assert.True(container.IsExpanded);
                }
            }
        }

        [Fact]
        public void Enter_Key_Should_Expand_TreeViewItem()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            CollapseAll(target);

            var item = data[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
            var header = container.HeaderPresenter?.Child;

            Assert.False(container.IsExpanded);
            Assert.NotNull(header);

            container.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Enter,
            });

            Assert.True(container.IsExpanded);
        }

        [Fact]
        public void Enter_Plus_Ctrl_Key_Should_Expand_TreeViewItem_Recursively()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            CollapseAll(target);

            var item = data[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
            var header = container.HeaderPresenter?.Child;

            Assert.False(container.IsExpanded);
            Assert.NotNull(header);

            container.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Enter,
                KeyModifiers = KeyModifiers.Control,
            });

            Assert.True(container.IsExpanded);

            AssertEachItemWithChildrenIsExpanded(item);

            void AssertEachItemWithChildrenIsExpanded(Node node)
            {
                var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(node));

                if (node.Children?.Count > 0)
                {
                    Assert.True(container.IsExpanded);
                    foreach (var c in node.Children)
                    {
                        AssertEachItemWithChildrenIsExpanded(c);
                    }
                }
                else
                {
                    Assert.False(container.IsExpanded);
                }
            }
        }

        [Fact]
        public void Space_Key_Should_Collapse_TreeViewItem()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var item = data[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
            var header = container.HeaderPresenter?.Child;

            Assert.True(container.IsExpanded);
            Assert.NotNull(header);

            container.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Enter,
            });

            Assert.False(container.IsExpanded);
        }

        [Fact]
        public void Space_Plus_Ctrl_Key_Should_Collapse_TreeViewItem_Recursively()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var item = data[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
            var header = container.HeaderPresenter?.Child;

            Assert.True(container.IsExpanded);
            Assert.NotNull(header);

            container.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Enter,
                KeyModifiers = KeyModifiers.Control,
            });

            Assert.False(container.IsExpanded);

            AssertEachItemWithChildrenIsCollapsed(item);

            void AssertEachItemWithChildrenIsCollapsed(Node node)
            {
                var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(node));
                Assert.NotNull(container);
                if (node.Children?.Count > 0)
                {
                    Assert.False(container.IsExpanded);
                    foreach (var c in node.Children)
                    {
                        AssertEachItemWithChildrenIsCollapsed(c);
                    }
                }
                else
                {
                    Assert.True(container.IsExpanded);
                }
            }
        }

        [Fact]
        public void Space_Key_Should_Expand_TreeViewItem()
        {
            using var app = Start();
            {
                var data = CreateTestTreeData();
                var target = CreateTarget(data: data);

                CollapseAll(target);

                var item = data[0];
                var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
                var header = container.HeaderPresenter?.Child;

                Assert.False(container.IsExpanded);
                Assert.NotNull(header);

                container.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Enter,
                });

                Assert.True(container.IsExpanded);
            }
        }

        [Fact]
        public void Space_plus_Ctrl_Key_Should_Expand_TreeViewItem_Recursively()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            CollapseAll(target);

            var item = data[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));
            var header = container.HeaderPresenter?.Child;

            Assert.False(container.IsExpanded);
            Assert.NotNull(header);

            container.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Enter,
                KeyModifiers = KeyModifiers.Control,
            });

            Assert.True(container.IsExpanded);

            AssertEachItemWithChildrenIsExpanded(item);

            void AssertEachItemWithChildrenIsExpanded(Node node)
            {
                var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(node));
                Assert.NotNull(container);
                if (node.Children?.Count > 0)
                {
                    Assert.True(container.IsExpanded);
                    foreach (var c in node.Children)
                    {
                        AssertEachItemWithChildrenIsExpanded(c);
                    }
                }
                else
                {
                    Assert.False(container.IsExpanded);
                }
            }
        }

        [Fact]
        public void Numpad_Star_Should_Expand_All_Children_Recursively()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            CollapseAll(target);

            var item = data[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

            Assert.NotNull(container);
            container.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Multiply,
            });

            AssertEachItemWithChildrenIsExpanded(item);

            void AssertEachItemWithChildrenIsExpanded(Node node)
            {
                var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(node));
                Assert.NotNull(container);
                if (node.Children?.Count > 0)
                {
                    Assert.True(container.IsExpanded);
                    foreach (var c in node.Children)
                    {
                        AssertEachItemWithChildrenIsExpanded(c);
                    }
                }
                else
                {
                    Assert.False(container.IsExpanded);
                }
            }
        }

        [Fact]
        public void Numpad_Slash_Should_Collapse_All_Children_Recursively()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var item = data[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

            Assert.NotNull(container);

            container.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Divide,
            });

            AssertEachItemWithChildrenIsCollapsed(item);

            void AssertEachItemWithChildrenIsCollapsed(Node node)
            {
                var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(node));
                Assert.NotNull(container);
                if (node.Children?.Count > 0)
                {
                    Assert.False(container.IsExpanded);
                    foreach (var c in node.Children)
                    {
                        AssertEachItemWithChildrenIsCollapsed(c);
                    }
                }
                else
                {
                    Assert.True(container.IsExpanded);
                }
            }
        }

        [Fact]
        public void Setting_SelectedItem_Should_Set_Container_Selected()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var item = data[0].Children[1].Children[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

            Assert.NotNull(container);

            target.SelectedItem = item;

            Assert.True(container.IsSelected);
        }

        [Fact]
        public void Setting_SelectedItem_Should_Raise_SelectedItemChanged_Event()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var item = data[0].Children[1].Children[0];

            var called = false;
            target.SelectionChanged += (s, e) =>
            {
                Assert.Empty(e.RemovedItems);
                Assert.Equal(1, e.AddedItems.Count);
                Assert.Same(item, e.AddedItems[0]);
                called = true;
            };

            target.SelectedItem = item;
            Assert.True(called);
        }

        [Fact]
        public void Removing_Selected_Root_Item_Should_Clear_Selection()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var item = data[0];

            target.SelectedItem = item;

            data.RemoveAt(0);

            Assert.Null(target.SelectedItem);
            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void Resetting_Root_Items_Should_Clear_Selection()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var item = data[0];

            target.SelectedItem = item;

            data.Clear();

            Assert.Null(target.SelectedItem);
            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void Removing_Selected_Child_Item_Should_Clear_Selection()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var item = data[0].Children[1];

            target.SelectedItem = item;

            data[0].Children.RemoveAt(1);

            Assert.Null(target.SelectedItem);
            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void Replacing_Selected_Child_Item_Should_Clear_Selection()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var item = data[0].Children[1];

            target.SelectedItem = item;

            data[0].Children[1] = new Node();

            Assert.Null(target.SelectedItem);
            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void Clearing_Child_Items_Should_Clear_Selection()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var item = data[0].Children[1];

            target.SelectedItem = item;

            data[0].Children.Clear();

            Assert.Null(target.SelectedItem);
            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void SelectedItem_Should_Be_Valid_When_SelectedItemChanged_Event_Raised()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            var item = data[0].Children[1].Children[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(item));

            Assert.NotNull(container);

            var called = false;
            target.SelectionChanged += (s, e) =>
            {
                Assert.Same(item, e.AddedItems[0]);
                Assert.Same(item, target.SelectedItem);
                called = true;
            };

            _mouse.Click(container);

            Assert.Equal(item, target.SelectedItem);
            Assert.True(container.IsSelected);
            Assert.True(called);
        }

        [Fact]
        public void Bound_SelectedItem_Should_Not_Be_Cleared_when_Changing_Selection()
        {
            using var app = Start();
            var dataContext = new TestDataContext();
            var target = CreateTarget();

            target.DataContext = dataContext;
            target.Bind(TreeView.ItemsSourceProperty, new Binding("Items"));
            target.Bind(TreeView.SelectedItemProperty, new Binding("SelectedItem"));

            var selectedValues = new List<object?>();

            dataContext.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(TestDataContext.SelectedItem))
                    selectedValues.Add(dataContext.SelectedItem);
            };

            selectedValues.Add(dataContext.SelectedItem);

            _mouse.Click(target.Presenter!.Panel!.Children[0], MouseButton.Left);
            _mouse.Click(target.Presenter!.Panel!.Children[2], MouseButton.Left);

            Assert.Equal(3, selectedValues.Count);
            Assert.Equal(new[] { null, "Item 0", "Item 2" }, selectedValues.ToArray());
        }

        [Fact]
        public void Expanding_SelectedItem_To_Be_Visible_Should_Result_In_Selected_Container()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, expandAll: false);

            target.SelectedItem = data[0].Children[1];

            var rootItem = Assert.IsType<TreeViewItem>(target.ContainerFromIndex(0));
            rootItem.IsExpanded = true;
            Layout(target);

            var container = Assert.IsType<TreeViewItem>(rootItem.ContainerFromIndex(1));
            Assert.True(container.IsSelected);
        }

        [Fact]
        public void LogicalChildren_Should_Be_Set()
        {
            using var app = Start();
            var target = CreateTarget(data: null);

            target.ItemsSource = new[] { "Foo", "Bar", "Baz " };
            Layout(target);

            var result = target.GetLogicalChildren()
                .OfType<TreeViewItem>()
                .Select(x => x.HeaderPresenter?.Child)
                .OfType<TextBlock>()
                .Select(x => x.Text)
                .ToList();

            Assert.Equal(new[] { "Foo", "Bar", "Baz " }, result);
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            using var app = Start();
            var items = new object[]
            {
                "Foo",
                new Node { Value = "Bar" },
                new TextBlock { Text = "Baz" },
                new TreeViewItem { Header = "Qux" },
            };

            var target = CreateTarget();
            var root = Assert.IsType<TestRoot>(target.GetVisualRoot());

            root.DataTemplates.Add(new FuncDataTemplate<Node>((x, _) => new Button { Content = x }));
            target.DataContext = "Base";
            target.ItemsSource = items;

            var dataContexts = target.Presenter!.Panel!.Children
                .Cast<Control>()
                .Select(x => x.DataContext)
                .ToList();

            Assert.Equal(
                new object[] { items[0], items[1], "Base", "Base" },
                dataContexts);
        }

        [Fact]
        public void Control_Item_Should_Not_Be_NameScope()
        {
            using var app = Start();
            var target = CreateTarget(data: null);
            var item = new TreeViewItem();

            target.Items!.Add(item);

            Assert.Same(item, target.LogicalChildren[0]);
            Assert.Null(NameScope.GetNameScope((TreeViewItem)item));
        }

        [Fact]
        public void Should_React_To_Children_Changing()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            Assert.Equal(new[] { "Root" }, ExtractItemHeader(target, 0));
            Assert.Equal(new[] { "Child1", "Child2", "Child3" }, ExtractItemHeader(target, 1));
            Assert.Equal(new[] { "Grandchild2a" }, ExtractItemHeader(target, 2));

            // Make sure that the binding to Node.Children does not get collected.
            GC.Collect();

            data[0].Children = new AvaloniaList<Node>
            {
                new Node
                {
                    Value = "NewChild1",
                }
            };

            Layout(target);

            Assert.Equal(new[] { "Root" }, ExtractItemHeader(target, 0));
            Assert.Equal(new[] { "NewChild1" }, ExtractItemHeader(target, 1));
        }

        [Fact]
        public void Keyboard_Navigation_Should_Move_To_Last_Selected_Node()
        {
            using var app = Start();
            var navigation = AvaloniaLocator.Current.GetRequiredService<IKeyboardNavigationHandler>();
            var data = CreateTestTreeData();

            var target = new TreeView
            {
                ItemsSource = data,
            };

            var button = new Button();

            var root = CreateRoot(new StackPanel
            {
                Children = { target, button },
            });
            var focus = root.FocusManager;

            root.LayoutManager.ExecuteInitialLayoutPass();
            ExpandAll(target);

            var item = data[0].Children[0];
            var node = target.TreeContainerFromItem(item);
            Assert.NotNull(node);

            target.SelectedItem = item;
            node.Focus();
            Assert.Same(node, focus.GetFocusedElement());

            navigation.Move(focus.GetFocusedElement()!, NavigationDirection.Next);
            Assert.Same(button, focus.GetFocusedElement());

            navigation.Move(focus.GetFocusedElement()!, NavigationDirection.Next);
            Assert.Same(node, focus.GetFocusedElement());
        }

        [Fact]
        public void Keyboard_Navigation_Should_Not_Crash_If_Selected_Item_Is_not_In_Tree()
        {
            using var app = Start();
            var data = CreateTestTreeData();

            var selectedNode = new Node { Value = "Out of Tree Selected Item" };

            var target = new TreeView
            {
                Template = CreateTreeViewTemplate(),
                ItemsSource = data,
                SelectedItem = selectedNode
            };

            var button = new Button();

            var root = CreateRoot(new StackPanel
            {
                Children = { target, button },
            });
            var focus = root.FocusManager;

            root.LayoutManager.ExecuteInitialLayoutPass();
            ExpandAll(target);

            var item = data[0].Children[0];
            var node = target.TreeContainerFromItem(item);
            Assert.NotNull(node);

            target.SelectedItem = selectedNode;
            node.Focus();
            Assert.Same(node, focus.GetFocusedElement());
        }

        [Fact]
        public void Pressing_SelectAll_Gesture_Should_Select_All_Nodes()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);
            var rootNode = data[0];
            var keymap = Application.Current!.PlatformSettings!.HotkeyConfiguration;
            var selectAllGesture = keymap.SelectAll.First();

            var keyEvent = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = selectAllGesture.Key,
                KeyModifiers = selectAllGesture.KeyModifiers
            };

            target.RaiseEvent(keyEvent);

            AssertAllChildContainersSelected(target, rootNode);
        }

        [Fact]
        public void Pressing_SelectAll_Gesture_With_Downward_Range_Selected_Should_Select_All_Nodes()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);
            var rootNode = data[0];
            var from = rootNode.Children[0];
            var to = rootNode.Children.Last();
            var fromContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
            var toContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

            ClickContainer(fromContainer, KeyModifiers.None);
            ClickContainer(toContainer, KeyModifiers.Shift);

            var keymap = Application.Current!.PlatformSettings!.HotkeyConfiguration;
            var selectAllGesture = keymap.SelectAll.First();

            var keyEvent = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = selectAllGesture.Key,
                KeyModifiers = selectAllGesture.KeyModifiers
            };

            target.RaiseEvent(keyEvent);

            AssertAllChildContainersSelected(target, rootNode);
        }

        [Fact]
        public void Pressing_SelectAll_Gesture_With_Upward_Range_Selected_Should_Select_All_Nodes()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);
            var rootNode = data[0];
            var from = rootNode.Children.Last();
            var to = rootNode.Children[0];
            var fromContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
            var toContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

            ClickContainer(fromContainer, KeyModifiers.None);
            ClickContainer(toContainer, KeyModifiers.Shift);

            var keymap = Application.Current!.PlatformSettings!.HotkeyConfiguration;
            var selectAllGesture = keymap.SelectAll.First();

            var keyEvent = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = selectAllGesture.Key,
                KeyModifiers = selectAllGesture.KeyModifiers
            };

            target.RaiseEvent(keyEvent);

            AssertAllChildContainersSelected(target, rootNode);
        }

        [Fact]
        public void Right_Click_On_SelectedItem_Should_Not_Clear_Existing_Selection()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);

            target.SelectAll();

            AssertAllChildContainersSelected(target, data[0]);
            Assert.Equal(5, target.SelectedItems.Count);

            _mouse.Click(target.Presenter!.Panel!.Children[0], MouseButton.Right);

            Assert.Equal(5, target.SelectedItems.Count);
        }

        [Fact]
        public void Right_Click_On_UnselectedItem_Should_Clear_Existing_Selection()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);
            var rootNode = data[0];
            var to = rootNode.Children[0];
            var then = rootNode.Children[1];
            var fromContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(rootNode));
            var toContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(to));
            var thenContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(then));

            ClickContainer(fromContainer, KeyModifiers.None);
            ClickContainer(toContainer, KeyModifiers.Shift);

            Assert.Equal(2, target.SelectedItems.Count);

            _mouse.Click(thenContainer, MouseButton.Right);

            Assert.Equal(1, target.SelectedItems.Count);
        }

        [Fact]
        public void Shift_Right_Click_Should_Not_Select_Multiple()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);
            var rootNode = data[0];
            var from = rootNode.Children[0];
            var to = rootNode.Children[1];
            var fromContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
            var toContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

            _mouse.Click(fromContainer);
            _mouse.Click(toContainer, MouseButton.Right, modifiers: KeyModifiers.Shift);

            Assert.Equal(1, target.SelectedItems.Count);
        }

        [Fact]
        public void Ctrl_Right_Click_Should_Not_Select_Multiple()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);
            var rootNode = data[0];
            var from = rootNode.Children[0];
            var to = rootNode.Children[1];
            var fromContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(from));
            var toContainer = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(to));

            _mouse.Click(fromContainer);
            _mouse.Click(toContainer, MouseButton.Right, modifiers: KeyModifiers.Control);

            Assert.Equal(1, target.SelectedItems.Count);
        }

        [Fact]
        public void TreeViewItems_Level_Should_Be_Set()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);

            Assert.Equal(0, GetItem(target, 0).Level);
            Assert.Equal(1, GetItem(target, 0, 0).Level);
            Assert.Equal(1, GetItem(target, 0, 1).Level);
            Assert.Equal(1, GetItem(target, 0, 2).Level);
            Assert.Equal(2, GetItem(target, 0, 1, 0).Level);
        }

        [Fact]
        public void TreeViewItems_Level_Should_Be_Set_For_Derived_TreeView()
        {
            using var app = Start();
            var tree = CreateTestTreeData();
            var target = new DerivedTreeView
            {
                Template = CreateTreeViewTemplate(),
                ItemsSource = tree,
            };

            var root = CreateRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();
            ExpandAll(target);

            Assert.Equal(0, GetItem(target, 0).Level);
            Assert.Equal(1, GetItem(target, 0, 0).Level);
            Assert.Equal(1, GetItem(target, 0, 1).Level);
            Assert.Equal(1, GetItem(target, 0, 2).Level);
            Assert.Equal(2, GetItem(target, 0, 1, 0).Level);
        }

        [Fact]
        public void Adding_Node_To_Removed_And_ReAdded_Parent_Should_Not_Crash()
        {
            // Issue #2985
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var parent = data[0];
            var node = parent.Children[1];

            parent.Children.Remove(node);
            parent.Children.Add(node);

            // #2985 causes ArgumentException here.
            node.Children.Add(new Node());
        }

        [Fact]
        public void Auto_Expanding_In_Style_Should_Not_Break_Range_Selection()
        {
            // Issue #2980.
            using var app = Start();

            var data = new List<Node>
            {
                new Node { Value = "Root1", },
                new Node { Value = "Root2", },
            };

            var style = new Style(x => x.OfType<TreeViewItem>())
            {
                Setters =
                {
                    new Setter(TreeViewItem.IsExpandedProperty, true),
                },
            };

            var target = CreateTarget(data: data, styles: new[] { style }, multiSelect: true);

            _mouse.Click(GetItem(target, 0));
            _mouse.Click(GetItem(target, 1), modifiers: KeyModifiers.Shift);
        }

        [Fact]
        public void Removing_TreeView_From_Root_Should_Preserve_TreeViewItems()
        {
            // Issue #3328
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var root = Assert.IsType<TestRoot>(target.GetVisualRoot());

            Assert.Equal(5, target.GetRealizedTreeContainers().Count());

            root.Child = null;

            Assert.Equal(5, target.GetRealizedTreeContainers().Count());
            Assert.Equal(1, target.Presenter!.Panel!.Children.Count);

            var rootNode = Assert.IsType<TreeViewItem>(target.Presenter.Panel.Children[0]);
            Assert.Equal(3, rootNode.GetRealizedContainers().Count());
            Assert.Equal(3, rootNode.Presenter!.Panel!.Children.Count);

            var child2Node = Assert.IsType<TreeViewItem>(rootNode.Presenter.Panel.Children[1]);
            Assert.Equal(1, child2Node.GetRealizedContainers().Count());
            Assert.Equal(1, child2Node.Presenter!.Panel!.Children.Count);
        }

        [Fact]
        public void Clearing_TreeView_Items_Clears_Index()
        {
            // Issue #3551
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data);
            var root = Assert.IsType<TestRoot>(target.GetVisualRoot());
            var rootNode = data[0];
            var container = Assert.IsType<TreeViewItem>(target.TreeContainerFromItem(rootNode));

            Assert.NotNull(container);

            root.Child = null;

            data.Clear();

            Assert.Empty(target.GetRealizedContainers());
        }

        [Fact]
        public void Can_Use_Derived_TreeViewItem()
        {
            var tree = CreateTestTreeData();
            var target = new DerivedTreeViewWithDerivedTreeViewItems
            {
                Template = CreateTreeViewTemplate(),
                ItemsSource = tree,
            };

            var root = CreateRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();
            ExpandAll(target);

            // Verify that all items are DerivedTreeViewItem
            foreach (var container in target.GetRealizedTreeContainers())
            {
                Assert.IsType<DerivedTreeViewItem>(container);
            }
        }

        [Fact]
        public void Can_Bind_Initial_Selected_State_Via_ItemContainerTheme()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var selected = new[] { data[0], data[0].Children[1] };

            foreach (var node in selected)
                node.IsSelected = true;

            var itemTheme = new ControlTheme(typeof(TreeViewItem))
            {
                BasedOn = CreateTreeViewItemControlTheme(),
                Setters =
                {
                    new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
                }
            };

            var target = CreateTarget(data: data, itemContainerTheme: itemTheme, multiSelect: true);

            AssertDataSelection(data, selected);
            AssertContainerSelection(target, selected);
            Assert.Equal(selected[0], target.SelectedItem);
            Assert.Equal(selected, target.SelectedItems);
        }

        [Fact]
        public void Can_Bind_Initial_Selected_State_Via_Style()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var selected = new[] { data[0], data[0].Children[1] };

            foreach (var node in selected)
                node.IsSelected = true;

            var style = new Style(x => x.OfType<TreeViewItem>())
            {
                Setters =
                {
                    new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
                }
            };

            var target = CreateTarget(data: data, multiSelect: true, styles: new[] { style });

            AssertDataSelection(data, selected);
            AssertContainerSelection(target, selected);
            Assert.Equal(selected[0], target.SelectedItem);
            Assert.Equal(selected, target.SelectedItems);
        }

        [Fact]
        public void Selection_State_Is_Updated_Via_IsSelected_Binding()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var selected = new[] { data[0], data[0].Children[1] };

            selected[0].IsSelected = true;

            var itemTheme = new ControlTheme(typeof(TreeViewItem))
            {
                BasedOn = CreateTreeViewItemControlTheme(),
                Setters =
                {
                    new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
                }
            };

            var target = CreateTarget(data: data, itemContainerTheme: itemTheme, multiSelect: true);

            selected[1].IsSelected = true;

            AssertDataSelection(data, selected);
            AssertContainerSelection(target, selected);
            Assert.Equal(selected[0], target.SelectedItem);
            Assert.Equal(selected, target.SelectedItems);
        }

        [Fact]
        public void Selection_State_Is_Updated_Via_IsSelected_Binding_On_Expand()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var selected = new[] { data[0], data[0].Children[1] };

            foreach (var node in selected)
                node.IsSelected = true;

            var itemTheme = new ControlTheme(typeof(TreeViewItem))
            {
                BasedOn = CreateTreeViewItemControlTheme(),
                Setters =
                {
                    new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
                }
            };

            var target = CreateTarget(
                data: data,
                expandAll: false,
                itemContainerTheme: itemTheme, 
                multiSelect: true);

            var rootContainer = Assert.IsType<TreeViewItem>(target.ContainerFromIndex(0));

            // Root TreeViewItem isn't expanded so selection for child won't have been picked
            // up by IsSelected binding yet.
            AssertContainerSelection(target, new[] { selected[0] });
            Assert.Equal(selected[0], target.SelectedItem);
            Assert.Equal(new[] { selected[0] }, target.SelectedItems);

            rootContainer.IsExpanded = true;
            Layout(target);

            // Root is expanded so now all expected items will be selected.
            AssertDataSelection(data, selected);
            AssertContainerSelection(target, selected);
            Assert.Equal(selected[0], target.SelectedItem);
            Assert.Equal(selected, target.SelectedItems);
        }

        [Fact]
        public void Selection_State_Is_Updated_Via_IsSelected_Binding_On_Expand_Single_Select()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var selected = new[] { data[0], data[0].Children[1] };

            foreach (var node in selected)
                node.IsSelected = true;

            var itemTheme = new ControlTheme(typeof(TreeViewItem))
            {
                BasedOn = CreateTreeViewItemControlTheme(),
                Setters =
                {
                    new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
                }
            };

            var target = CreateTarget(
                data: data,
                expandAll: false,
                itemContainerTheme: itemTheme);

            var rootContainer = Assert.IsType<TreeViewItem>(target.ContainerFromIndex(0));

            // Root TreeViewItem isn't expanded so selection for child won't have been picked
            // up by IsSelected binding yet.
            AssertContainerSelection(target, new[] { selected[0] });
            Assert.Equal(selected[0], target.SelectedItem);
            Assert.Equal(new[] { selected[0] }, target.SelectedItems);

            rootContainer.IsExpanded = true;
            Layout(target);

            // Root is expanded and newly revealed selected node will replace current selection
            // given that we're in SelectionMode == Single.
            selected = new[] { selected[1] };
            AssertDataSelection(data, selected);
            AssertContainerSelection(target, selected);
            Assert.Equal(selected[0], target.SelectedItem);
            Assert.Equal(selected, target.SelectedItems);
        }

        private static TreeView CreateTarget(Optional<IList<Node>?> data = default,
            bool expandAll = true,
            ControlTheme? itemContainerTheme = null,
            IDataTemplate? itemTemplate = null,
            bool multiSelect = false,
            IEnumerable<Style>? styles = null)
        {
            var target = new TreeView
            {
                ItemContainerTheme = itemContainerTheme,
                ItemsSource = data.HasValue ? data.Value : CreateTestTreeData(),
                ItemTemplate = itemTemplate,
                SelectionMode = multiSelect ? SelectionMode.Multiple : SelectionMode.Single,
            };

            var root = CreateRoot(target);

            if (styles is not null)
                root.Styles.AddRange(styles);

            root.LayoutManager.ExecuteInitialLayoutPass();

            if (expandAll)
            {
                ExpandAll(target);
            }

            return target;
        }

        private static TestRoot CreateRoot(Control child)
        {
            return new TestRoot
            {
                Resources =
                {
                    { typeof(TreeView), CreateTreeViewControlTheme() },
                    { typeof(TreeViewItem), CreateTreeViewItemControlTheme() },
                },
                DataTemplates =
                {
                    new TestTreeDataTemplate(),
                },
                Child = child,
            };
        }

        private static AvaloniaList<Node> CreateTestTreeData()
        {
            return new AvaloniaList<Node>
            {
                new Node
                {
                    Value = "Root",
                    Children = new AvaloniaList<Node>
                    {
                        new Node
                        {
                            Value = "Child1",
                        },
                        new Node
                        {
                            Value = "Child2",
                            Children = new AvaloniaList<Node>
                            {
                                new Node
                                {
                                    Value = "Grandchild2a",
                                },
                            },
                        },
                        new Node
                        {
                            Value = "Child3",
                        }
                    }
                }
            };
        }

        private static ControlTheme CreateTreeViewControlTheme()
        {
            return new ControlTheme(typeof(TreeView))
            {
                Setters =
                {
                    new Setter(TreeView.TemplateProperty, CreateTreeViewTemplate()),
                },
            };
        }

        private static ControlTheme CreateTreeViewItemControlTheme()
        {
            return new ControlTheme(typeof(TreeViewItem))
            {
                Setters =
                {
                    new Setter(TreeView.TemplateProperty, CreateTreeViewItemTemplate()),
                },
            };
        }

        private static IControlTemplate CreateTreeViewTemplate()
        {
            return new FuncControlTemplate<TreeView>((parent, scope) => new ItemsPresenter
            {
                Name = "PART_ItemsPresenter",
            }.RegisterInNameScope(scope));
        }

        private static IControlTemplate CreateTreeViewItemTemplate()
        {
            return new FuncControlTemplate<TreeViewItem>((parent, scope) => new Panel
            {
                Children =
                {
                    new Border
                    {
                        Name = "PART_Header",
                        Child = new ContentPresenter
                        {
                            Name = "PART_HeaderPresenter",
                            [~ContentPresenter.ContentProperty] = parent[~TreeViewItem.HeaderProperty],
                            [~ContentPresenter.ContentTemplateProperty] = parent[~TreeViewItem.HeaderTemplateProperty],
                        }.RegisterInNameScope(scope)
                    }.RegisterInNameScope(scope),
                    new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.IsVisibleProperty] = parent[~TreeViewItem.IsExpandedProperty],
                    }.RegisterInNameScope(scope)
                }
            });
        }

        private static void ExpandAll(TreeView tree)
        {
            foreach (var i in tree.GetRealizedContainers())
            {
                tree.ExpandSubTree((TreeViewItem)i);
            }
        }

        private static void CollapseAll(TreeView tree)
        {
            foreach (var i in tree.GetRealizedContainers())
            {
                tree.CollapseSubTree((TreeViewItem)i);
            }
        }

        private static TreeViewItem GetItem(TreeView target, params int[] indexes)
        {
            var c = (ItemsControl)target;

            foreach (var index in indexes)
            {
                var item = c.ItemsView[index]!;
                c = (ItemsControl)target.TreeContainerFromItem(item)!;
            }

            return (TreeViewItem)c;
        }

        private static List<string?> ExtractItemHeader(TreeView tree, int level)
        {
            return ExtractItemContent(tree.Presenter?.Panel, 0, level)
                .Select(x => x.HeaderPresenter?.Child)
                .OfType<TextBlock>()
                .Select(x => x.Text)
                .ToList();
        }

        private static IEnumerable<TreeViewItem> ExtractItemContent(Panel? panel, int currentLevel, int level)
        {
            if (panel is null)
                yield break;

            foreach (var c in panel.Children)
            {
                var container = Assert.IsType<TreeViewItem>(c);

                if (currentLevel == level)
                {
                    yield return container;
                }
                else if (container.Presenter?.Panel is { } childPanel)
                {
                    foreach (var child in ExtractItemContent(childPanel, currentLevel + 1, level))
                    {
                        yield return child;
                    }
                }
            }
        }

        private void Layout(Control c)
        {
            (c.GetVisualRoot() as ILayoutRoot)?.LayoutManager.ExecuteLayoutPass();
        }

        private void ClickContainer(Control container, KeyModifiers modifiers)
        {
            _mouse.Click(container, modifiers: modifiers);
        }

        private void AssertContainerSelection(TreeView treeView, params Node[] expected)
        {
            static void Evaluate(Control container, HashSet<Node> remaining)
            {
                var treeViewItem = Assert.IsType<TreeViewItem>(container);
                var node = (Node)container.DataContext!;

                Assert.Equal(remaining.Contains(node), treeViewItem.IsSelected);
                remaining.Remove(node);

                foreach (var child in treeViewItem.GetRealizedContainers())
                {
                    Evaluate(child, remaining);
                }
            }

            var remaining = expected.ToHashSet();
            foreach (var container in treeView.GetRealizedContainers())
                Evaluate(container, remaining);
            Assert.Empty(remaining);
        }

        private void AssertAllChildContainersSelected(TreeView treeView, Node node)
        {
            Assert.NotNull(node.Children);

            foreach (var child in node.Children)
            {
                var container = Assert.IsType<TreeViewItem>(treeView.TreeContainerFromItem(child));
                Assert.True(container.IsSelected);
            }
        }

        private void AssertDataSelection(IEnumerable<Node> data, params Node[] expected)
        {
            static void Evaluate(Node rootNode, HashSet<Node> remaining)
            {
                Assert.Equal(remaining.Contains(rootNode), rootNode.IsSelected);
                remaining.Remove(rootNode);

                if (rootNode.Children is null)
                    return;

                foreach (var child in rootNode.Children)
                {
                    Evaluate(child, remaining);
                }
            }

            var remaining = expected.ToHashSet();
            foreach (var node in data)
                Evaluate(node, remaining);
            Assert.Empty(remaining);
        }

        private IDisposable Start()
        {
            return UnitTestApplication.Start(
                TestServices.MockThreadingInterface.With(
                    focusManager: new FocusManager(),
                    fontManagerImpl: new HeadlessFontManagerStub(),
                    keyboardDevice: () => new KeyboardDevice(),
                    keyboardNavigation: () => new KeyboardNavigationHandler(),
                    inputManager: new InputManager(),
                    renderInterface: new HeadlessPlatformRenderInterface(),
                    textShaperImpl: new HeadlessTextShaperStub()));
        }

        private class Node : NotifyingBase
        {
            private IAvaloniaList<Node> _children = new AvaloniaList<Node>();
            private bool _isSelected;

            public string? Value { get; set; }

            public IAvaloniaList<Node> Children
            {
                get => _children;
                set
                {
                    _children = value;
                    RaisePropertyChanged(nameof(Children));
                }
            }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        RaisePropertyChanged();
                    }
                }
            }

            public override string ToString() => Value ?? string.Empty;
        }

        private class TestTreeDataTemplate : ITreeDataTemplate
        {
            public Control Build(object? param)
            {
                var node = (Node)param!;
                return new TextBlock { Text = node.Value };
            }

            public InstancedBinding ItemsSelector(object item)
            {
                var obs = BindingExpression.Create(item, o => ((Node)o).Children);
                return new InstancedBinding(obs, BindingMode.OneWay, BindingPriority.LocalValue);
            }

            public bool Match(object? data)
            {
                return data is Node;
            }
        }

        private class DerivedTreeView : TreeView
        {
        }

        private class DerivedTreeViewWithDerivedTreeViewItems : TreeView
        {
            protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
            {
                return new DerivedTreeViewItem();
            }
        }

        private class DerivedTreeViewItem : TreeViewItem
        {
        }

        private class TestDataContext : INotifyPropertyChanged
        {
            private string? _selectedItem;

            public TestDataContext()
            {
                Items = new ObservableCollection<string>(Enumerable.Range(0, 5).Select(i => $"Item {i}"));
            }

            public ObservableCollection<string>? Items { get; }

            public string? SelectedItem
            {
                get { return _selectedItem; }
                set
                {
                    _selectedItem = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

        }
    }
}
