using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests
{
    public class ItemsControlTests
    {
        [Fact]
        public void Setting_ItemsSource_Should_Populate_Items()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

            Assert.NotSame(target.ItemsSource, target.Items);
            Assert.Equal(target.ItemsSource, target.Items);
        }

        [Fact]
        public void Cannot_Set_ItemsSource_With_Items_Present()
        {
            using var app = Start();
            var target = CreateTarget();
            target.Items.Add("foo");

            Assert.Throws<InvalidOperationException>(() => target.ItemsSource = new[] { "baz" });
        }

        [Fact]
        public void Cannot_Modify_Items_When_ItemsSource_Set()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: Array.Empty<string>());

            Assert.Throws<InvalidOperationException>(() => target.Items.Add("foo"));
        }

        [Fact]
        public void Should_Use_ItemTemplate_To_Create_Control()
        {
            using var app = Start();
            var target = CreateTarget(
                itemsSource: new[] { "Foo" },
                itemTemplate: new FuncDataTemplate<string>((_, __) => new Canvas()));
            var container = GetContainer(target);

            Assert.IsType<Canvas>(container.Child);
        }

        [Fact]
        public void ItemTemplate_Can_Be_Changed()
        {
            using var app = Start();
            var target = CreateTarget(
                itemsSource: new[] { "Foo" },
                itemTemplate: new FuncDataTemplate<string>((_, __) => new Canvas()));
            var container = GetContainer(target);

            Assert.IsType<Canvas>(container.Child);

            target.ItemTemplate = new FuncDataTemplate<string>((_, __) => new Border());
            Layout(target);

            container = GetContainer(target);

            Assert.IsType<Border>(container.Child);
        }

        [Fact]
        public void Panel_Should_Have_TemplatedParent_Set_To_ItemsControl()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "Foo" });

            Assert.Equal(target, target.ItemsPanelRoot?.TemplatedParent);
        }

        [Fact]
        public void Panel_Should_Have_ItemsHost_Set_To_True()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "Foo" });

            Assert.True(target.ItemsPanelRoot?.IsItemsHost);
        }

        [Fact]
        public void Container_Should_Have_TemplatedParent_Set_To_Null()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "Foo" });

            var container = GetContainer(target);

            Assert.Null(container.TemplatedParent);
        }

        [Fact]
        public void Container_Should_Have_Theme_Set_To_ItemContainerTheme()
        {
            using var app = Start();
            var theme = new ControlTheme { TargetType = typeof(ContentPresenter) };
            var target = CreateTarget(
                itemsSource: new[] { "Foo" },
                itemContainerTheme: theme);

            var container = GetContainer(target);

            Assert.Same(container.Theme, theme);
        }

        [Fact]
        public void Container_Should_Have_Theme_Set_To_ItemContainerTheme_With_Base_TargetType()
        {
            using var app = Start();
            var theme = new ControlTheme { TargetType = typeof(Control) };
            var target = CreateTarget(
                itemsSource: new[] { "Foo" },
                itemContainerTheme: theme);

            var container = GetContainer(target);

            Assert.Same(container.Theme, theme);
        }

        [Fact]
        public void ItemContainerTheme_Can_Be_Changed()
        {
            using var app = Start();
            
            var theme1 = new ControlTheme 
            { 
                TargetType = typeof(ContentPresenter),
                Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Red) }
            };

            var theme2 = new ControlTheme
            {
                TargetType = typeof(ContentPresenter),
                Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Green) }
            };

            var target = CreateTarget(
                itemsSource: new[] { "Foo" },
                itemContainerTheme: theme1);

            var container = GetContainer(target);

            Assert.Same(container.Theme, theme1);
            Assert.Equal(container.Background, Brushes.Red);

            target.ItemContainerTheme = theme2;

            container = GetContainer(target);
            Assert.Same(container.Theme, theme2);
            Assert.Equal(container.Background, Brushes.Green);
        }

        [Fact]
        public void ItemContainerTheme_Can_Be_Changed_Virtualizing()
        {
            using var app = Start();

            var theme1 = new ControlTheme
            {
                TargetType = typeof(ContentPresenter),
                Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Red) }
            };

            var theme2 = new ControlTheme
            {
                TargetType = typeof(ContentPresenter),
                Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Green) }
            };

            var itemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());
            var target = CreateTarget(
                itemsSource: new[] { "Foo" },
                itemContainerTheme: theme1,
                itemsPanel: itemsPanel);

            var container = GetContainer(target);

            Assert.Same(container.Theme, theme1);
            Assert.Equal(container.Background, Brushes.Red);

            target.ItemContainerTheme = theme2;
            Layout(target);

            container = GetContainer(target);
            Assert.Same(container.Theme, theme2);
            Assert.Equal(container.Background, Brushes.Green);
        }

        [Fact]
        public void ItemContainerTheme_Can_Be_Cleared()
        {
            using var app = Start();

            var theme = new ControlTheme
            {
                TargetType = typeof(ContentPresenter),
                Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Red) }
            };

            var target = CreateTarget(
                itemsSource: new[] { "Foo" },
                itemContainerTheme: theme);

            var container = GetContainer(target);

            Assert.Same(container.Theme, theme);
            Assert.Equal(container.Background, Brushes.Red);

            target.ItemContainerTheme = null;

            container = GetContainer(target);
            Assert.Null(container.Theme);
            Assert.Null(container.Background);
        }

        [Fact]
        public void ItemContainerTheme_Should_Not_Override_LocalValue_Theme()
        {
            using var app = Start();

            var theme1 = new ControlTheme
            {
                TargetType = typeof(ContentPresenter),
                Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Red) }
            };

            var theme2 = new ControlTheme
            {
                TargetType = typeof(Control),
                Setters = { new Setter(ContentPresenter.BackgroundProperty, Brushes.Green) }
            };

            var items = new object[]
            {
                new ContentPresenter(),
                new ContentPresenter
                {
                    Theme = theme2
                },
            };

            var target = CreateTarget(
                itemsSource: items,
                itemContainerTheme: theme1);

            Assert.Same(theme1, GetContainer(target, 0).Theme);
            Assert.Same(theme2, GetContainer(target, 1).Theme);

            target.ItemContainerTheme = null;

            Assert.Null(GetContainer(target, 0).Theme);
            Assert.Same(theme2, GetContainer(target, 1).Theme);
        }

        [Fact]
        public void Container_Should_Have_LogicalParent_Set_To_ItemsControl()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);
            var target = new ItemsControl();
            var root = CreateRoot(target);
            var templatedParent = new Button();

            target.TemplatedParent = templatedParent;
            target.Template = CreateItemsControlTemplate();
            target.ItemsSource = new[] { "Foo" };

            root.LayoutManager.ExecuteInitialLayoutPass();

            var container = GetContainer(target);

            Assert.Equal(target, container.Parent);
        }

        [Fact]
        public void Control_Item_Should_Be_Logical_Child_Before_ApplyTemplate()
        {
            using var app = Start();
            var child = new Control();
            var target = CreateTarget(items: new[] { child }, performLayout: false);

            Assert.False(target.IsMeasureValid);
            Assert.Empty(target.GetVisualChildren());
            Assert.Equal(child.Parent, target);
            Assert.Equal(child.GetLogicalParent(), target);
            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Control_Item_Should_Be_Logical_Child_After_Layout()
        {
            using var app = Start();
            var child = new Control();
            var target = CreateTarget(items: new[] { child });

            Assert.True(target.IsMeasureValid);
            Assert.Single(target.GetVisualChildren());
            Assert.Equal(target, child.Parent);
            Assert.Equal(target, child.GetLogicalParent());
            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Added_Container_Should_Have_LogicalParent_Set_To_ItemsControl()
        {
            using var app = Start();
            var items = new ObservableCollection<Border>();
            var target = CreateTarget(itemsSource: items);

            var item = new Border();
            items.Add(item);

            Assert.Equal(target, item.Parent);
        }

        [Fact]
        public void Control_Item_Can_Be_Removed_From_Logical_Children_Before_ApplyTemplate()
        {
            using var app = Start();
            var child = new Control();
            var target = CreateTarget(items: new[] { child }, performLayout: false);

            Assert.False(target.IsMeasureValid);
            Assert.Empty(target.GetVisualChildren());
            Assert.Single(target.GetLogicalChildren());

            target.Items.RemoveAt(0);

            Assert.Null(child.Parent);
            Assert.Null(child.GetLogicalParent());
            Assert.Empty(target.GetLogicalChildren());
        }

        [Fact]
        public void Clearing_Items_Should_Clear_Child_Controls_Parent_Before_ApplyTemplate()
        {
            using var app = Start();
            var child = new Control();
            var target = CreateTarget(items: new[] { child }, performLayout: false);

            Assert.False(target.IsMeasureValid);
            Assert.Empty(target.GetVisualChildren());
            Assert.Single(target.GetLogicalChildren());

            target.Items.Clear();

            Assert.Null(child.Parent);
            Assert.Null(child.GetLogicalParent());
        }

        [Fact]
        public void Assigning_ItemsSource_Should_Not_Fire_LogicalChildren_CollectionChanged_Before_ApplyTemplate()
        {
            using var app = Start();
            var child = new Control();
            var target = CreateTarget(itemsSource: new[] { child }, performLayout: false);
            var called = false;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            var list = new AvaloniaList<Control>(new[] { child });
            target.ItemsSource = list;

            Assert.False(called);
        }

        [Fact]
        public void Removing_ItemsSource_Items_Should_Not_Fire_LogicalChildren_CollectionChanged_Before_ApplyTemplate()
        {
            using var app = Start();
            var items = new AvaloniaList<string> { "Foo", "Bar" };
            var target = CreateTarget(itemsSource: items, performLayout: false);
            var called = false;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            items.Remove("Bar");

            Assert.False(called);
        }

        [Fact]
        public void Changing_ItemsSource_Should_Not_Fire_LogicalChildren_CollectionChanged_Before_ApplyTemplate()
        {
            using var app = Start();
            var child = new Control();
            var target = CreateTarget(itemsSource: new[] { child }, performLayout: false);
            var called = false;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            var list = new AvaloniaList<Control>();
            target.ItemsSource = list;
            list.Add(child);

            Assert.False(called);
        }

        [Fact]
        public void Clearing_Items_Should_Clear_Child_Controls_Parent()
        {
            using var app = Start();
            var child = new Control();
            var target = CreateTarget(items: new[] { child });

            target.Items.Clear();

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Adding_Control_Item_Should_Make_Control_Appear_In_LogicalChildren()
        {
            using var app = Start();
            var child = new Control();
            var target = CreateTarget(items: new[] { child }, performLayout: false);

            // Should appear both before and after applying template.
            Assert.Equal(new ILogical[] { child }, target.GetLogicalChildren());

            Layout(target);

            Assert.Equal(new ILogical[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Adding_String_Item_Should_Make_ContentPresenter_Appear_In_LogicalChildren()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "Foo " });
            var logical = (ILogical)target;

            Assert.Equal(1, logical.LogicalChildren.Count);
            Assert.IsType<ContentPresenter>(logical.LogicalChildren[0]);
        }

        [Fact]
        public void Adding_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            using var app = Start();
            var target = CreateTarget();
            var called = false;

            target.Template = CreateItemsControlTemplate();
            target.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            var child = new Control();
            target.Items.Add(child);

            Assert.True(called);
        }

        [Fact]
        public void Clearing_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            using var app = Start();
            var child = new Control();
            var target = CreateTarget(items: new[] { child });
            var called = false;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            target.Items.Clear();

            Assert.True(called);
        }

        [Fact]
        public void LogicalChildren_Should_Not_Change_Instance_When_Template_Changed()
        {
            using var app = Start();
            var target = CreateTarget();
            var before = ((ILogical)target).LogicalChildren;

            target.Template = null;
            target.Template = CreateItemsControlTemplate();
            Layout(target);

            var after = ((ILogical)target).LogicalChildren;

            Assert.NotNull(before);
            Assert.NotNull(after);
            Assert.Same(before, after);
        }

        [Fact]
        public void Control_Item_Should_Be_Removed_From_LogicalChildren()
        {
            using var app = Start();
            var item = new Border();

            var items = new ObservableCollection<Control>();
            var target = CreateTarget(itemsSource: items);

            items.Add(item);
            items.Remove(item);

            Assert.Empty(target.LogicalChildren);
        }

        [Fact]
        public void Control_Item_Should_Be_Removed_From_LogicalChildren_Virtualizing()
        {
            using var app = Start();
            var item = new Border();

            var items = new ObservableCollection<Control>();
            var itemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());
            var target = CreateTarget(
                itemsPanel: itemsPanel,
                itemsSource: items);

            items.Add(item);
            Layout(target);

            items.Remove(item);

            Assert.Empty(target.LogicalChildren);
        }

        [Fact]
        public void Should_Clear_Containers_When_ItemsPresenter_Changes()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });
            var panel = Assert.IsAssignableFrom<Panel>(target.Presenter?.Panel);

            Assert.Equal(2, panel.Children.Count());

            target.Template = CreateItemsControlTemplate();
            target.ApplyTemplate();

            Assert.Empty(panel.Children);
        }

        [Fact]
        public void Empty_Class_Should_Initially_Be_Applied()
        {
            using var app = Start();
            var target = CreateTarget(performLayout: false);

            Assert.Contains(":empty", target.Classes);
        }

        [Fact]
        public void Empty_Class_Should_Be_Cleared_When_Items_Added()
        {
            using var app = Start();
            var target = CreateTarget(items: new[] { 1, 2, 3 }, performLayout: false);

            Assert.DoesNotContain(":empty", target.Classes);
        }

        [Fact]
        public void Empty_Class_Should_Be_Cleared_When_ItemsSource_Items_Added()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { 1, 2, 3 }, performLayout: false);

            Assert.DoesNotContain(":empty", target.Classes);
        }

        [Fact]
        public void Empty_Class_Should_Be_Set_When_ItemsSource_Collection_Cleared()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { 1, 2, 3 });

            target.ItemsSource = new int[0];

            Assert.Contains(":empty", target.Classes);
        }

        [Fact]
        public void Item_Count_Should_Be_Set_When_ItemsSource_Set()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { 1, 2, 3 });

            Assert.Equal(3, target.ItemCount);
        }

        [Fact]
        public void Item_Count_Should_Be_Set_When_Items_Changed()
        {
            using var app = Start();
            var items = new ObservableCollection<int>() { 1, 2, 3 };
            var target = CreateTarget(items: new[] { 1, 2, 3 });

            target.Items.Add(4);

            Assert.Equal(4, target.ItemCount);

            target.Items.Clear();

            Assert.Equal(0, target.ItemCount);
        }

        [Fact]
        public void Item_Count_Should_Be_Set_When_ItemsSource_Items_Changed()
        {
            using var app = Start();
            var items = new ObservableCollection<int>() { 1, 2, 3 };
            var target = CreateTarget(itemsSource: items);

            items.Add(4);

            Assert.Equal(4, target.ItemCount);

            items.Clear();

            Assert.Equal(0, target.ItemCount);
        }

        [Fact]
        public void Empty_Class_Should_Be_Set_When_Items_Collection_Cleared()
        {
            using var app = Start();
            var items = new ObservableCollection<int>() { 1, 2, 3 };
            var target = CreateTarget(itemsSource: items);

            items.Clear();

            Assert.Contains(":empty", target.Classes);
        }

        [Fact]
        public void Empty_Class_Should_Not_Be_Set_When_ItemsSource_Collection_Count_Increases()
        {
            using var app = Start();
            var items = new ObservableCollection<int>() { };
            var target = CreateTarget(itemsSource: items);

            items.Add(1);

            Assert.DoesNotContain(":empty", target.Classes);
        }

        [Fact]
        public void Single_Item_Class_Should_Be_Set_When_ItemsSource_Collection_Count_Increases_To_One()
        {
            using var app = Start();
            var items = new ObservableCollection<int>() { };
            var target = CreateTarget(itemsSource: items);

            items.Add(1);

            Assert.Contains(":singleitem", target.Classes);
        }

        [Fact]
        public void Empty_Class_Should_Not_Be_Set_When_ItemsSource_Collection_Cleared()
        {
            using var app = Start();
            var items = new ObservableCollection<int>() { 1, 2, 3 };
            var target = CreateTarget(itemsSource: items);

            items.Clear();

            Assert.DoesNotContain(":singleitem", target.Classes);
        }

        [Fact]
        public void Single_Item_Class_Should_Not_Be_Set_When_Items_Collection_Count_Increases_Beyond_One()
        {
            using var app = Start();
            var items = new ObservableCollection<int>() { 1 };
            var target = CreateTarget(itemsSource: items);

            items.Add(2);

            Assert.DoesNotContain(":singleitem", target.Classes);
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            using var app = Start();
            var items = new object[]
            {
                "Foo",
                new Item("Bar"),
                new TextBlock { Text = "Baz" },
                new ListBoxItem { Content = "Qux" },
            };
            var dataTemplate = new FuncDataTemplate<Item>((x, __) => new Button { Content = x });
            var target = CreateTarget(
                dataContext: "Base",
                itemsSource: items,
                itemTemplate: dataTemplate);
            var panel = Assert.IsAssignableFrom<Panel>(target.ItemsPanelRoot);
            var dataContexts = panel.Children
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
            using var app = Start();
            var items = new object[] { new TextBlock() };
            var target = CreateTarget(itemsSource: items);
            var item = target.LogicalChildren[0];

            Assert.Null(NameScope.GetNameScope((TextBlock)item));
        }

        [Fact]
        public void Focuses_Next_Item_On_Key_Down()
        {
            using var app = Start();
            var items = new object[]
            {
                new Button(),
                new Button(),
            };

            var target = CreateTarget(itemsSource: items);
            GetContainer<Button>(target).Focus();

            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Down,
            });

            var panel = Assert.IsAssignableFrom<Panel>(target.ItemsPanelRoot);
            var focusManager = ((IInputRoot)target.VisualRoot!).FocusManager;

            Assert.Equal(panel.Children[1], focusManager?.GetFocusedElement());
        }

        [Fact]
        public void Does_Not_Focus_Non_Focusable_Item_On_Key_Down()
        {
            using var app = Start();
            var items = new object[]
            {
                    new Button(),
                    new Button { Focusable = false },
                    new Button(),
            };

            var target = CreateTarget(itemsSource: items);
            GetContainer<Button>(target).Focus();

            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Down,
            });

            var panel = Assert.IsAssignableFrom<Panel>(target.ItemsPanelRoot);
            var focusManager = ((IInputRoot)target.VisualRoot!).FocusManager;

            Assert.Equal(panel.Children[2], focusManager?.GetFocusedElement());
        }

        [Fact]
        public void Detaching_Then_Reattaching_To_Logical_Tree_Twice_Does_Not_Throw()
        {
            // # Issue 3487
            using var app = Start();
            var target = CreateTarget(
                itemsSource: new[] { "foo", "bar" },
                itemTemplate: new FuncDataTemplate<string>((_, __) => new Canvas()));

            var root = Assert.IsType<TestRoot>(target.GetVisualRoot());

            root.Child = null;
            root.Child = target;

            root.LayoutManager.ExecuteLayoutPass();

            root.Child = null;
            root.Child = target;
        }

        [Fact]
        public void Should_Use_DisplayMemberBinding()
        {
            using var app = Start();
            var target = CreateTarget(
                itemsSource: new[] { "Foo" },
                displayMemberBinding: new Binding("Length"));

            var container = GetContainer(target);
            var textBlock = Assert.IsType<TextBlock>(container.Child);

            Assert.Equal(textBlock.Text, "3");
        }

        [Fact]
        public void DisplayMemberBinding_Can_Be_Changed()
        {
            using var app = Start();
            var target = CreateTarget(
                itemsSource: new[] { new Item("Foo", "Bar") },
                displayMemberBinding: new Binding("Value"));

            var container = GetContainer(target);
            var textBlock = Assert.IsType<TextBlock>(container.Child);

            Assert.Equal(textBlock.Text, "Bar");

            target.DisplayMemberBinding = new Binding("Caption");
            Layout(target);

            container = GetContainer(target);
            textBlock = Assert.IsType<TextBlock>(container.Child);
            
            Assert.Equal(textBlock.Text, "Foo");
        }

        [Fact]
        public void Cannot_Set_Both_DisplayMemberBinding_And_ItemTemplate_1()
        {
            using var app = Start();
            var target = CreateTarget(
                displayMemberBinding: new Binding("Length"));

            Assert.Throws<InvalidOperationException>(() =>
                target.ItemTemplate = new FuncDataTemplate<string>((_, _) => new TextBlock()));
        }

        [Fact]
        public void Cannot_Set_Both_DisplayMemberBinding_And_ItemTemplate_2()
        {
            using var app = Start();
            var target = CreateTarget(
                itemTemplate: new FuncDataTemplate<string>((_, _) => new TextBlock()));

            Assert.Throws<InvalidOperationException>(() => target.DisplayMemberBinding = new Binding("Length"));
        }

        [Fact]
        public void ContainerPrepared_Is_Raised_For_Each_Control_Item_Container()
        {
            using var app = Start();
            var items = new AvaloniaList<string>();
            var target = CreateTarget();
            var result = new List<Control>();
            var index = 0;

            target.ContainerPrepared += (s, e) =>
            {
                Assert.Equal(index++, e.Index);
                result.Add(e.Container);
            };

            target.Items.Add(new Button());
            target.Items.Add(new Button());
            target.Items.Add(new Button());

            Assert.Equal(3, result.Count);
            Assert.Equal(target.GetRealizedContainers(), result);
        }

        [Fact]
        public void ContainerPrepared_Is_Raised_For_Each_Item_Container()
        {
            using var app = Start();
            var items = new AvaloniaList<string>();
            var target = CreateTarget();
            var result = new List<Control>();
            var index = 0;

            target.ContainerPrepared += (s, e) =>
            {
                Assert.Equal(index++, e.Index);
                result.Add(e.Container);
            };

            target.Items.Add("Foo");
            target.Items.Add("Bar");
            target.Items.Add("Baz");

            Assert.Equal(3, result.Count);
            Assert.Equal(target.GetRealizedContainers(), result);
        }

        [Fact]
        public void ContainerPrepared_Is_Raised_For_Each_ItemsSource_Item_Container_On_Layout()
        {
            using var app = Start();
            var items = new AvaloniaList<string>();
            var target = CreateTarget(itemsSource: items);
            var result = new List<Control>();
            var index = 0;

            target.ContainerPrepared += (s, e) =>
            {
                Assert.Equal(index++, e.Index);
                result.Add(e.Container);
            };

            items.AddRange(new[] { "Foo", "Bar", "Baz" });

            Assert.Equal(3, result.Count);
            Assert.Equal(target.GetRealizedContainers(), result);
        }

        [Fact]
        public void ContainerIndexChanged_Is_Raised_When_Item_Added()
        {
            using var app = Start();
            var target = CreateTarget(items: new[] { "Foo", "Bar", "Baz" });
            var result = new List<Control>();
            var index = 1;

            target.ContainerIndexChanged += (s, e) =>
            {
                Assert.Equal(index++, e.OldIndex);
                Assert.Equal(index, e.NewIndex);
                result.Add(e.Container);
            };

            target.Items.Insert(1, "Qux");

            Assert.Equal(2, result.Count);
            Assert.Equal(target.GetRealizedContainers().Skip(2), result);
        }

        [Fact]
        public void ContainerClearing_Is_Raised_When_Item_Removed()
        {
            using var app = Start();
            var target = CreateTarget(items: new[] { "Foo", "Bar", "Baz" });
            var expected = target.ContainerFromIndex(1);
            var raised = 0;

            target.ContainerClearing += (s, e) =>
            {
                Assert.Same(expected, e.Container);
                ++raised;
            };

            target.Items.RemoveAt(1);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Handles_Recycling_Control_Items_Inside_Containers()
        {
            // Issue #10825
            using var app = Start();

            // The items must be controls but not of the container type.
            var items = Enumerable.Range(0, 100).Select(x => new TextBlock 
            { 
                Text = $"Item {x}",
                Width = 100,
                Height = 100,
            }).ToList();

            // Virtualization is required
            var itemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());

            // Create an ItemsControl which uses containers, and provide a scroll viewer.
            var target = CreateTarget<ItemsControlWithContainer>(
                items: items,
                itemsPanel: itemsPanel,
                scrollViewer: true);
            var scroll = target.FindAncestorOfType<ScrollViewer>();

            Assert.NotNull(scroll);
            Assert.Equal(10, target.GetRealizedContainers().Count());

            // Scroll so that half a container is visible: an extra container is generated.
            scroll.Offset = new(0, 2050);
            Layout(target);

            // Scroll so that the extra container is no longer needed and recycled.
            scroll.Offset = new(0, 2100);
            Layout(target);

            // Scroll back: issue #10825 triggered.
            scroll.Offset = new(0, 2000);
            Layout(target);
        }

        [Fact]
        public void ItemIsOwnContainer_Content_Should_Not_Be_Cleared_When_Removed()
        {
            // Issue #11128.
            using var app = Start();
            var item = new ContentPresenter { Content = "foo" };
            var target = CreateTarget(items: new[] { item });

            target.Items.RemoveAt(0);

            Assert.Equal("foo", item.Content);
        }

        private static ItemsControl CreateTarget(
            object? dataContext = null,
            IBinding? displayMemberBinding = null,
            IList? items = null,
            IList? itemsSource = null,
            ControlTheme? itemContainerTheme = null,
            IDataTemplate? itemTemplate = null,
            ITemplate<Panel?>? itemsPanel = null,
            IEnumerable<IDataTemplate>? dataTemplates = null,
            bool performLayout = true)
        {
            return CreateTarget<ItemsControl>(
                dataContext: dataContext,
                displayMemberBinding: displayMemberBinding,
                items: items,
                itemsSource: itemsSource,
                itemContainerTheme: itemContainerTheme,
                itemTemplate: itemTemplate,
                itemsPanel: itemsPanel,
                dataTemplates: dataTemplates,
                performLayout: performLayout);
        }

        private static T CreateTarget<T>(
            object? dataContext = null,
            IBinding? displayMemberBinding = null,
            IList? items = null,
            IList? itemsSource = null,
            ControlTheme? itemContainerTheme = null,
            IDataTemplate? itemTemplate = null,
            ITemplate<Panel?>? itemsPanel = null,
            IEnumerable<IDataTemplate>? dataTemplates = null,
            bool performLayout = true,
            bool scrollViewer = false)
                where T : ItemsControl, new()
        {
            var target = new T
            {
                DataContext = dataContext,
                DisplayMemberBinding = displayMemberBinding,
                ItemContainerTheme = itemContainerTheme,
                ItemTemplate = itemTemplate,
                ItemsSource = itemsSource,
            };

            if (items is not null)
            {
                foreach (var item in items)
                    target.Items.Add(item);
            }

            if (itemsPanel is not null)
                target.ItemsPanel = itemsPanel;

            var scroll = scrollViewer ? new ScrollViewer { Content = target } : null;
            var root = CreateRoot(scroll ?? (Control)target);

            if (dataTemplates is not null)
            {
                foreach (var dataTemplate in dataTemplates)
                    root.DataTemplates.Add(dataTemplate);
            }

            if (performLayout)
                root.LayoutManager.ExecuteInitialLayoutPass();

            return target;
        }

        private static TestRoot CreateRoot(Control child)
        {
            return new TestRoot
            {
                Resources =
                {
                    { typeof(ContentControl), CreateContentControlTheme() },
                    { typeof(ItemsControl), CreateItemsControlTheme() },
                    { typeof(ScrollViewer), CreateScrollViewerTheme() },
                },
                Child = child,
            };
        }

        private static ControlTheme CreateContentControlTheme()
        {
            return new ControlTheme(typeof(ContentControl))
            {
                Setters =
                {
                    new Setter(TreeView.TemplateProperty, CreateContentControlTemplate()),
                },
            };
        }

        private static FuncControlTemplate CreateContentControlTemplate()
        {
            return new FuncControlTemplate<ContentControl>((parent, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!ListBoxItem.ContentProperty],
                    [!ContentPresenter.ContentTemplateProperty] = parent[!ListBoxItem.ContentTemplateProperty],
                }.RegisterInNameScope(scope));
        }

        private static ControlTheme CreateItemsControlTheme()
        {
            return new ControlTheme(typeof(ItemsControl))
            {
                Setters =
                {
                    new Setter(TreeView.TemplateProperty, CreateItemsControlTemplate()),
                },
            };
        }

        private static FuncControlTemplate CreateItemsControlTemplate()
        {
            return new FuncControlTemplate<ItemsControl>((parent, scope) =>
            {
                return new Border
                {
                    Background = new Media.SolidColorBrush(0xffffffff),
                    Child = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsPanelProperty] = parent[~ItemsControl.ItemsPanelProperty],
                    }.RegisterInNameScope(scope)
                };
            });
        }

        private static ControlTheme CreateScrollViewerTheme()
        {
            return new ControlTheme(typeof(ScrollViewer))
            {
                Setters =
                {
                    new Setter(TreeView.TemplateProperty, CreateScrollViewerTemplate()),
                },
            };
        }

        private static FuncControlTemplate CreateScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((parent, scope) =>
                new Panel
                {
                    Children =
                    {
                        new ScrollContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                        }.RegisterInNameScope(scope),
                        new ScrollBar
                        {
                            Name = "verticalScrollBar",
                        }
                    }
                });
        }

        private static void Layout(Control c)
        {
            (c.GetVisualRoot() as ILayoutRoot)?.LayoutManager.ExecuteLayoutPass();
        }

        private static ContentPresenter GetContainer(ItemsControl target, int index = 0)
        {
            return Assert.IsType<ContentPresenter>(target.GetRealizedContainers().ElementAt(index));
        }

        private static T GetContainer<T>(ItemsControl target, int index = 0)
        {
            return Assert.IsType<T>(target.GetRealizedContainers().ElementAt(index));
        }

        public static IDisposable Start()
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

        private class ItemsControlWithContainer : ItemsControl
        {
            protected override Type StyleKeyOverride => typeof(ItemsControl);

            protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
            {
                return new ContainerControl();
            }

            protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
            {
                return NeedsContainer<ContainerControl>(item, out recycleKey);
            }
        }

        private class ContainerControl : ContentControl
        {
            protected override Type StyleKeyOverride => typeof(ContentControl);
        }

        private record Item(string Caption, string? Value = null);
    }
}
