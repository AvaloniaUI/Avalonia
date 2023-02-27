using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ItemsControlTests
    {
        [Fact]
        public void Should_Use_ItemTemplate_To_Create_Control()
        {
            var target = new ItemsControl
            {
                Template = GetTemplate(),
                ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
            };

            target.Items = new[] { "Foo" };
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var container = (ContentPresenter)target.Presenter.Panel.Children[0];
            container.UpdateChild();

            Assert.IsType<Canvas>(container.Child);
        }

        [Fact]
        public void ItemTemplate_Can_Be_Changed()
        {
            var target = new ItemsControl
            {
                Template = GetTemplate(),
                ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
            };

            target.Items = new[] { "Foo" };
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var container = (ContentPresenter)target.Presenter.Panel.Children[0];
            container.UpdateChild();

            Assert.IsType<Canvas>(container.Child);

            target.ItemTemplate = new FuncDataTemplate<string>((_, __) => new Border());
            container = (ContentPresenter)target.Presenter.Panel.Children[0];
            container.UpdateChild();

            Assert.IsType<Border>(container.Child);
        }

        [Fact]
        public void Panel_Should_Have_TemplatedParent_Set_To_ItemsControl()
        {
            var target = new ItemsControl();

            target.Template = GetTemplate();
            target.Items = new[] { "Foo" };
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.Equal(target, target.Presenter.Panel.TemplatedParent);
        }

        [Fact]
        public void Container_Should_Have_TemplatedParent_Set_To_Null()
        {
            var target = new ItemsControl();

            target.Template = GetTemplate();
            target.Items = new[] { "Foo" };
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var container = (ContentPresenter)target.Presenter.Panel.Children[0];

            Assert.Null(container.TemplatedParent);
        }

        [Fact]
        public void Container_Should_Have_Theme_Set_To_ItemContainerTheme()
        {
            var theme = new ControlTheme { TargetType = typeof(ContentPresenter) };
            var target = new ItemsControl
            {
                ItemContainerTheme = theme,
            };

            target.Template = GetTemplate();
            target.Items = new[] { "Foo" };
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var container = (ContentPresenter)target.Presenter.Panel.Children[0];

            Assert.Same(container.Theme, theme);
        }

        [Fact]
        public void Container_Should_Have_LogicalParent_Set_To_ItemsControl()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var root = new Window();
                var target = new ItemsControl();

                root.Content = target;

                var templatedParent = new Button();
                target.SetValue(StyledElement.TemplatedParentProperty, templatedParent);
                target.Template = GetTemplate();

                target.Items = new[] { "Foo" };

                root.ApplyTemplate();
                target.ApplyTemplate();
                target.Presenter.ApplyTemplate();

                var container = (ContentPresenter)target.Presenter.Panel.Children[0];

                Assert.Equal(target, container.Parent);
            }
        }

        [Fact]
        public void Control_Item_Should_Be_Logical_Child_Before_ApplyTemplate()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { child };

            Assert.Equal(child.Parent, target);
            Assert.Equal(child.GetLogicalParent(), target);
            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Control_Item_Should_Be_Logical_Child_After_Layout()
        {
            var target = new ItemsControl
            {
                Template = GetTemplate(),
            };
            var root = new TestRoot(target);
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { child };
            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Equal(target, child.Parent);
            Assert.Equal(target, child.GetLogicalParent());
            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Added_Container_Should_Have_LogicalParent_Set_To_ItemsControl()
        {
            var item = new Border();
            var items = new ObservableCollection<Border>();

            var target = new ItemsControl
            {
                Template = GetTemplate(),
                Items = items,
            };

            var root = new TestRoot(true, target);

            root.Measure(new Size(100, 100));
            root.Arrange(new Rect(0, 0, 100, 100));

            items.Add(item);

            Assert.Equal(target, item.Parent);
        }

        [Fact]
        public void Control_Item_Should_Be_Removed_From_Logical_Children_Before_ApplyTemplate()
        {
            var target = new ItemsControl();
            var child = new Control();
            var items = new AvaloniaList<Control>(child);

            target.Template = GetTemplate();
            target.Items = items;
            items.RemoveAt(0);

            Assert.Null(child.Parent);
            Assert.Null(child.GetLogicalParent());
            Assert.Empty(target.GetLogicalChildren());
        }

        [Fact]
        public void Clearing_Items_Should_Clear_Child_Controls_Parent_Before_ApplyTemplate()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { child };
            target.Items = null;

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Clearing_Items_Should_Clear_Child_Controls_Parent()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { child };
            target.ApplyTemplate();
            target.Items = null;

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Adding_Control_Item_Should_Make_Control_Appear_In_LogicalChildren()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { child };

            // Should appear both before and after applying template.
            Assert.Equal(new ILogical[] { child }, target.GetLogicalChildren());

            target.ApplyTemplate();

            Assert.Equal(new ILogical[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Adding_String_Item_Should_Make_ContentPresenter_Appear_In_LogicalChildren()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { "Foo" };
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var logical = (ILogical)target;
            Assert.Equal(1, logical.LogicalChildren.Count);
            Assert.IsType<ContentPresenter>(logical.LogicalChildren[0]);
        }

        [Fact]
        public void Setting_Items_To_Null_Should_Remove_LogicalChildren()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = GetTemplate();
            target.Items = new[] { "Foo" };
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            Assert.NotEmpty(target.GetLogicalChildren());

            target.Items = null;

            Assert.Equal(new ILogical[0], target.GetLogicalChildren());
        }


        [Fact]
        public void Setting_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var child = new Control();
            var called = false;

            target.Template = GetTemplate();
            target.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            target.Items = new[] { child };

            Assert.True(called);
        }

        [Fact]
        public void Setting_Items_To_Null_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var child = new Control();
            var called = false;

            target.Template = GetTemplate();
            target.Items = new[] { child };
            target.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            target.Items = null;

            Assert.True(called);
        }

        [Fact]
        public void Changing_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var child = new Control();
            var called = false;

            target.Template = GetTemplate();
            target.Items = new[] { child };
            target.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            target.Items = new[] { "Foo" };

            Assert.True(called);
        }

        [Fact]
        public void Adding_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var items = new AvaloniaList<string> { "Foo" };
            var called = false;

            target.Template = GetTemplate();
            target.Items = items;
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            items.Add("Bar");

            Assert.True(called);
        }

        [Fact]
        public void Removing_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var items = new AvaloniaList<string> { "Foo", "Bar" };
            var called = false;

            target.Template = GetTemplate();
            target.Items = items;
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            items.Remove("Bar");

            Assert.True(called);
        }

        [Fact]
        public void LogicalChildren_Should_Not_Change_Instance_When_Template_Changed()
        {
            var target = new ItemsControl()
            {
                Template = GetTemplate(),
            };

            var before = ((ILogical)target).LogicalChildren;

            target.Template = null;
            target.Template = GetTemplate();

            var after = ((ILogical)target).LogicalChildren;

            Assert.NotNull(before);
            Assert.NotNull(after);
            Assert.Same(before, after);
        }

        [Fact]
        public void Should_Clear_Containers_When_ItemsPresenter_Changes()
        {
            var target = new ItemsControl
            {
                Items = new[] { "foo", "bar" },
                Template = GetTemplate(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var panel = target.Presenter.Panel;
            Assert.Equal(2, panel.Children.Count());

            target.Template = GetTemplate();
            target.ApplyTemplate();

            Assert.Empty(panel.Children);
        }

        [Fact]
        public void Empty_Class_Should_Initially_Be_Applied()
        {
            var target = new ItemsControl()
            {
                Template = GetTemplate(),
            };

            Assert.Contains(":empty", target.Classes);
        }

        [Fact]
        public void Empty_Class_Should_Be_Cleared_When_Items_Added()
        {
            var target = new ItemsControl()
            {
                Template = GetTemplate(),
                Items = new[] { 1, 2, 3 },
            };

            Assert.DoesNotContain(":empty", target.Classes);
        }

        [Fact]
        public void Empty_Class_Should_Be_Set_When_Items_Not_Set()
        {
            var target = new ItemsControl()
            {
                Template = GetTemplate(),
            };

            Assert.Contains(":empty", target.Classes);
        }

        [Fact]
        public void Empty_Class_Should_Be_Set_When_Empty_Collection_Set()
        {
            var target = new ItemsControl()
            {
                Template = GetTemplate(),
                Items = new[] { 1, 2, 3 },
            };

            target.Items = new int[0];

            Assert.Contains(":empty", target.Classes);
        }

        [Fact]
        public void Item_Count_Should_Be_Set_When_Items_Added()
        {
            var target = new ItemsControl()
            {
                Template = GetTemplate(),
                Items = new[] { 1, 2, 3 },
            };

            Assert.Equal(3, target.ItemCount);
        }

        [Fact]
        public void Item_Count_Should_Be_Set_When_Items_Changed()
        {
            var items = new ObservableCollection<int>() { 1, 2, 3 };

            var target = new ItemsControl()
            {
                Template = GetTemplate(),
                Items = items,
            };

            items.Add(4);

            Assert.Equal(4, target.ItemCount);

            items.Clear();

            Assert.Equal(0, target.ItemCount);
        }

        [Fact]
        public void Empty_Class_Should_Be_Set_When_Items_Collection_Cleared()
        {
            var items = new ObservableCollection<int>() { 1, 2, 3 };

            var target = new ItemsControl()
            {
                Template = GetTemplate(),
                Items = items,
            };

            items.Clear();

            Assert.Contains(":empty", target.Classes);
        }

        [Fact]
        public void Empty_Class_Should_Not_Be_Set_When_Items_Collection_Count_Increases()
        {
            var items = new ObservableCollection<int>() { };

            var target = new ItemsControl()
            {
                Template = GetTemplate(),
                Items = items,
            };

            items.Add(1);

            Assert.DoesNotContain(":empty", target.Classes);
        }

        [Fact]
        public void Single_Item_Class_Should_Be_Set_When_Items_Collection_Count_Increases_To_One()
        {
            var items = new ObservableCollection<int>() { };

            var target = new ItemsControl()
            {
                Template = GetTemplate(),
                Items = items,
            };

            items.Add(1);

            Assert.Contains(":singleitem", target.Classes);
        }

        [Fact]
        public void Empty_Class_Should_Not_Be_Set_When_Items_Collection_Cleared()
        {
            var items = new ObservableCollection<int>() { 1, 2, 3 };

            var target = new ItemsControl()
            {
                Template = GetTemplate(),
                Items = items,
            };

            items.Clear();

            Assert.DoesNotContain(":singleitem", target.Classes);
        }

        [Fact]
        public void Single_Item_Class_Should_Not_Be_Set_When_Items_Collection_Count_Increases_Beyond_One()
        {
            var items = new ObservableCollection<int>() { 1 };

            var target = new ItemsControl()
            {
                Template = GetTemplate(),
                Items = items,
            };

            items.Add(2);

            Assert.DoesNotContain(":singleitem", target.Classes);
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            var items = new object[]
            {
                "Foo",
                new Item("Bar"),
                new TextBlock { Text = "Baz" },
                new ListBoxItem { Content = "Qux" },
            };

            var target = new ItemsControl
            {
                Template = GetTemplate(),
                DataContext = "Base",
                DataTemplates =
                {
                    new FuncDataTemplate<Item>((x, __) => new Button { Content = x })
                },
                Items = items,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var dataContexts = target.Presenter.Panel.Children
                .Do(x => (x as ContentPresenter)?.UpdateChild())
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
            var items = new object[]
            {
                new TextBlock(),
            };

            var target = new ItemsControl
            {
                Template = GetTemplate(),
                Items = items,
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var item = target.Presenter.Panel.LogicalChildren[0];
            Assert.Null(NameScope.GetNameScope((TextBlock)item));
        }

        [Fact]
        public void Focuses_Next_Item_On_Key_Down()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var items = new object[]
                {
                    new Button(),
                    new Button(),
                };

                var target = new ItemsControl
                {
                    Template = GetTemplate(),
                    Items = items,
                };

                var root = new TestRoot { Child = target };

                target.ApplyTemplate();
                target.Presenter.ApplyTemplate();
                target.Presenter.Panel.Children[0].Focus();

                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Down,
                });

                Assert.Equal(
                    target.Presenter.Panel.Children[1],
                    FocusManager.Instance.Current);
            }
        }

        [Fact]
        public void Does_Not_Focus_Non_Focusable_Item_On_Key_Down()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var items = new object[]
                {
                    new Button(),
                    new Button { Focusable = false },
                    new Button(),
                };

                var target = new ItemsControl
                {
                    Template = GetTemplate(),
                    Items = items,
                };

                var root = new TestRoot { Child = target };

                target.ApplyTemplate();
                target.Presenter.ApplyTemplate();
                target.Presenter.Panel.Children[0].Focus();

                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Down,
                });

                Assert.Equal(
                    target.Presenter.Panel.Children[2],
                    FocusManager.Instance.Current);
            }
        }

        [Fact]
        public void Detaching_Then_Reattaching_To_Logical_Tree_Twice_Does_Not_Throw()
        {
            // # Issue 3487
            var target = new ItemsControl
            {
                Template = GetTemplate(),
                Items = new[] { "foo", "bar" },
                ItemTemplate = new FuncDataTemplate<string>((_, __) => new Canvas()),
            };

            var root = new TestRoot(target);
            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            root.Child = null;
            root.Child = target;

            target.Measure(Size.Infinity);

            root.Child = null;
            root.Child = target;
        }
        
        [Fact]
        public void Should_Use_DisplayMemberBinding()
        {
            var target = new ItemsControl
            {
                Template = GetTemplate(),
                DisplayMemberBinding = new Binding("Length")
            };

            target.Items = new[] { "Foo" };
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var container = (ContentPresenter)target.Presenter.Panel.Children[0];
            container.UpdateChild();

            Assert.Equal(container.Child!.GetValue(TextBlock.TextProperty), "3");
        }

        [Fact]
        public void DisplayMemberBinding_Can_Be_Changed()
        {
            var target = new ItemsControl
            {
                Template = GetTemplate(),
                DisplayMemberBinding = new Binding("Value")
            };

            target.Items = new[] { new Item("Foo", "Bar") };
            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();

            var container = (ContentPresenter)target.Presenter.Panel.Children[0];
            container.UpdateChild();

            Assert.Equal(container.Child!.GetValue(TextBlock.TextProperty), "Bar");

            target.DisplayMemberBinding = new Binding("Caption");
            
            container = (ContentPresenter)target.Presenter.Panel.Children[0];
            container.UpdateChild();

            Assert.Equal(container.Child!.GetValue(TextBlock.TextProperty), "Foo");
        }

        [Fact]
        public void Cannot_Set_Both_DisplayMemberBinding_And_ItemTemplate_1()
        {
            var target = new ItemsControl
            {
                Template = GetTemplate(),
                DisplayMemberBinding = new Binding("Length")
            };

            Assert.Throws<InvalidOperationException>(() => 
                target.ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock()));
        }

        [Fact]
        public void Cannot_Set_Both_DisplayMemberBinding_And_ItemTemplate_2()
        {
            var target = new ItemsControl
            {
                Template = GetTemplate(),
                ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock()),
            };

            Assert.Throws<InvalidOperationException>(() => target.DisplayMemberBinding = new Binding("Length"));
        }

        private class Item
        {
            public Item(string value)
            {
                Value = value;
            }

            public Item(string caption, string value)
            {
                Caption = caption;
                Value = value;
            }

            public string Caption { get; }
            public string Value { get; }
        }

        private static FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<ItemsControl>((parent, scope) =>
            {
                return new Border
                {
                    Background = new Media.SolidColorBrush(0xffffffff),
                    Child = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                    }.RegisterInNameScope(scope)
                };
            });
        }
    }
}
