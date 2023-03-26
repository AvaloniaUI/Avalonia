using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests
{
    public class TreeViewTests
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
            AssertChildrenSelected(target, rootNode);
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
            AssertChildrenSelected(target, rootNode);
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
            AssertChildrenSelected(target, rootNode);

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
            var focus = FocusManager.Instance!;
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

            root.LayoutManager.ExecuteInitialLayoutPass();
            ExpandAll(target);

            var item = data[0].Children[0];
            var node = target.TreeContainerFromItem(item);
            Assert.NotNull(node);

            target.SelectedItem = item;
            node.Focus();
            Assert.Same(node, focus.Current);

            navigation.Move(focus.Current!, NavigationDirection.Next);
            Assert.Same(button, focus.Current);

            navigation.Move(focus.Current!, NavigationDirection.Next);
            Assert.Same(node, focus.Current);
        }

        [Fact]
        public void Keyboard_Navigation_Should_Not_Crash_If_Selected_Item_Is_not_In_Tree()
        {
            using var app = Start();
            var focus = FocusManager.Instance!;
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

            root.LayoutManager.ExecuteInitialLayoutPass();
            ExpandAll(target);

            var item = data[0].Children[0];
            var node = target.TreeContainerFromItem(item);
            Assert.NotNull(node);

            target.SelectedItem = selectedNode;
            node.Focus();
            Assert.Same(node, focus.Current);
        }

        [Fact]
        public void Pressing_SelectAll_Gesture_Should_Select_All_Nodes()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);
            var rootNode = data[0];
            var keymap = AvaloniaLocator.Current.GetRequiredService<PlatformHotkeyConfiguration>();
            var selectAllGesture = keymap.SelectAll.First();

            var keyEvent = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = selectAllGesture.Key,
                KeyModifiers = selectAllGesture.KeyModifiers
            };

            target.RaiseEvent(keyEvent);

            AssertChildrenSelected(target, rootNode);
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

            var keymap = AvaloniaLocator.Current.GetRequiredService<PlatformHotkeyConfiguration>();
            var selectAllGesture = keymap.SelectAll.First();

            var keyEvent = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = selectAllGesture.Key,
                KeyModifiers = selectAllGesture.KeyModifiers
            };

            target.RaiseEvent(keyEvent);

            AssertChildrenSelected(target, rootNode);
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

            var keymap = AvaloniaLocator.Current.GetRequiredService<PlatformHotkeyConfiguration>();
            var selectAllGesture = keymap.SelectAll.First();

            var keyEvent = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = selectAllGesture.Key,
                KeyModifiers = selectAllGesture.KeyModifiers
            };

            target.RaiseEvent(keyEvent);

            AssertChildrenSelected(target, rootNode);
        }

        [Fact]
        public void Right_Click_On_SelectedItem_Should_Not_Clear_Existing_Selection()
        {
            using var app = Start();
            var data = CreateTestTreeData();
            var target = CreateTarget(data: data, multiSelect: true);

            target.SelectAll();

            AssertChildrenSelected(target, data[0]);
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

        private void AssertChildrenSelected(TreeView treeView, Node rootNode)
        {
            Assert.NotNull(rootNode.Children);

            foreach (var child in rootNode.Children)
            {
                var container = Assert.IsType<TreeViewItem>(treeView.TreeContainerFromItem(child));
                Assert.True(container.IsSelected);
            }
        }

        private IDisposable Start()
        {
            return UnitTestApplication.Start(
                TestServices.MockThreadingInterface.With(
                    focusManager: new FocusManager(),
                    fontManagerImpl: new MockFontManagerImpl(),
                    keyboardDevice: () => new KeyboardDevice(),
                    keyboardNavigation: new KeyboardNavigationHandler(),
                    inputManager: new InputManager(),
                    renderInterface: new MockPlatformRenderInterface(),
                    textShaperImpl: new MockTextShaperImpl()));
        }

        private class Node : NotifyingBase
        {
            private IAvaloniaList<Node> _children = new AvaloniaList<Node>();

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
                var obs = ExpressionObserver.Create(item, o => ((Node)o).Children);
                return InstancedBinding.OneWay(obs);
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
            protected internal override Control CreateContainerForItemOverride() => new DerivedTreeViewItem();
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
