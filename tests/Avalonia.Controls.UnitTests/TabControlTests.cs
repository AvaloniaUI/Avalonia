using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TabControlTests
    {
        static TabControlTests()
        {
            RuntimeHelpers.RunClassConstructor(typeof(RelativeSource).TypeHandle);
        }

        [Fact]
        public void First_Tab_Should_Be_Selected_By_Default()
        {
            TabItem selected;
            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items =
                {
                    (selected = new TabItem
                    {
                        Name = "first",
                        Content = "foo",
                    }),
                    new TabItem
                    {
                        Name = "second",
                        Content = "bar",
                    },
                }
            };

            target.ApplyTemplate();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(selected, target.SelectedItem);
        }

        [Fact]
        public void Pre_Selecting_TabItem_Should_Set_SelectedContent_After_It_Was_Added()
        {
            const string secondContent = "Second";
            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items =
                {
                    new TabItem { Header = "First"},
                    new TabItem { Header = "Second", Content = secondContent, IsSelected = true }
                },
            };

            ApplyTemplate(target);

            Assert.Equal(secondContent, target.SelectedContent);
        }

        [Fact]
        public void Logical_Children_Should_Be_TabItems()
        {
            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items =
                {
                    new TabItem
                    {
                        Content = "foo"
                    },
                    new TabItem
                    {
                        Content = "bar"
                    },
                }
            };

            Assert.Equal(target.Items, target.GetLogicalChildren().ToList());
            target.ApplyTemplate();
            Assert.Equal(target.Items, target.GetLogicalChildren().ToList());
        }

        [Fact]
        public void Removal_Should_Set_First_Tab()
        {
            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items =
                {
                    new TabItem
                    {
                        Name = "first",
                        Content = "foo",
                    },
                    new TabItem
                    {
                        Name = "second",
                        Content = "bar",
                    },
                    new TabItem
                    {
                        Name = "3rd",
                        Content = "barf",
                    },
                }
            };

            Prepare(target);
            target.SelectedItem = target.Items[1];

            var item = Assert.IsType<TabItem>(target.Items[1]);
            Assert.Same(item, target.SelectedItem);
            Assert.Equal(item.Content, target.SelectedContent);

            target.Items.RemoveAt(1);

            item = Assert.IsType<TabItem>(target.Items[0]);
            Assert.Same(item, target.SelectedItem);
            Assert.Equal(item.Content, target.SelectedContent);
        }

        [Fact]
        public void Removal_Should_Set_New_Item0_When_Item0_Selected()
        {
            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items =
                {
                    new TabItem
                    {
                        Name = "first",
                        Content = "foo",
                    },
                    new TabItem
                    {
                        Name = "second",
                        Content = "bar",
                    },
                    new TabItem
                    {
                        Name = "3rd",
                        Content = "barf",
                    },
                }
            };

            Prepare(target);
            target.SelectedItem = target.Items[0];

            var item = Assert.IsType<TabItem>(target.Items[0]);
            Assert.Same(item, target.SelectedItem);
            Assert.Equal(item.Content, target.SelectedContent);

            target.Items.RemoveAt(0);

            item = Assert.IsType<TabItem>(target.Items[0]);
            Assert.Same(item, target.SelectedItem);
            Assert.Equal(item.Content, target.SelectedContent);
        }

        [Fact]
        public void Removal_Should_Set_New_Item0_When_Item0_Selected_With_DataTemplate()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var collection = new ObservableCollection<Item>()
            {
                new Item("first"),
                new Item("second"),
                new Item("3rd"),
            };

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                ItemsSource = collection,
            };

            Prepare(target);
            target.SelectedItem = collection[0];

            Assert.Same(collection[0], target.SelectedItem);
            Assert.Equal(collection[0], target.SelectedContent);

            collection.RemoveAt(0);

            Assert.Same(collection[0], target.SelectedItem);
            Assert.Equal(collection[0], target.SelectedContent);
        }

        [Fact]
        public void TabItem_Templates_Should_Be_Set_Before_TabItem_ApplyTemplate()
        {
            var template = new FuncControlTemplate<TabItem>((x, __) => new Decorator());
            TabControl target;
            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<TabItem>())
                    {
                        Setters =
                        {
                            new Setter(TemplatedControl.TemplateProperty, template)
                        }
                    }
                },
                Child = (target = new TabControl
                {
                    Template = TabControlTemplate(),
                    Items = 
                    {
                        new TabItem
                        {
                            Name = "first",
                            Content = "foo",
                        },
                        new TabItem
                        {
                            Name = "second",
                            Content = "bar",
                        },
                        new TabItem
                        {
                            Name = "3rd",
                            Content = "barf",
                        },
                    },
                })
            };

            var collection = target.Items.Cast<TabItem>().ToList();
            Assert.Same(collection[0].Template, template);
            Assert.Same(collection[1].Template, template);
            Assert.Same(collection[2].Template, template);
        }

        [Fact]
        public void DataContexts_Should_Be_Correctly_Set()
        {
            var items = new object[]
            {
                "Foo",
                new Item("Bar"),
                new TextBlock { Text = "Baz" },
                new TabItem { Content = "Qux" },
                new TabItem { Content = new TextBlock { Text = "Bob" } }
            };

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = "Base",
                DataTemplates =
                {
                    new FuncDataTemplate<Item>((x, __) => new Button { Content = x })
                },
                ItemsSource = items,
            };

            ApplyTemplate(target);

            target.ContentPart.UpdateChild();
            var dataContext = ((TextBlock)target.ContentPart.Child).DataContext;
            Assert.Equal(items[0], dataContext);

            target.SelectedIndex = 1;
            target.ContentPart.UpdateChild();
            dataContext = ((Button)target.ContentPart.Child).DataContext;
            Assert.Equal(items[1], dataContext);

            target.SelectedIndex = 2;
            target.ContentPart.UpdateChild();
            dataContext = ((TextBlock)target.ContentPart.Child).DataContext;
            Assert.Equal("Base", dataContext);

            target.SelectedIndex = 3;
            target.ContentPart.UpdateChild();
            dataContext = ((TextBlock)target.ContentPart.Child).DataContext;
            Assert.Equal("Qux", dataContext);

            target.SelectedIndex = 4;
            target.ContentPart.UpdateChild();
            dataContext = target.ContentPart.DataContext;
            Assert.Equal("Base", dataContext);
        }

        /// <summary>
        /// Non-headered control items should result in TabItems with empty header.
        /// </summary>
        /// <remarks>
        /// If a TabControl is created with non IHeadered controls as its items, don't try to
        /// display the control in the header: if the control is part of the header then
        /// *that* control would also end up in the content region, resulting in dual-parentage 
        /// breakage.
        /// </remarks>
        [Fact]
        public void Non_IHeadered_Control_Items_Should_Be_Ignored()
        {
            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items =
                {
                    new TextBlock { Text = "foo" },
                    new TextBlock { Text = "bar" },
                },
            };

            ApplyTemplate(target);

            var logicalChildren = target.GetLogicalChildren();

            var result = logicalChildren
                .OfType<TabItem>()
                .Select(x => x.Header)
                .ToList();

            Assert.Equal(new object[] { null, null }, result);
        }

        [Fact]
        public void Should_Handle_Changing_To_TabItem_With_Null_Content()
        {
            TabControl target = new TabControl
            {
                Template = TabControlTemplate(),
                Items =
                {
                    new TabItem { Header = "Foo" },
                    new TabItem { Header = "Foo", Content = new Decorator() },
                    new TabItem { Header = "Baz" },
                },
            };

            ApplyTemplate(target);

            target.SelectedIndex = 2;

            var page = (TabItem)target.SelectedItem;

            Assert.Null(page.Content);
        }

        [Fact]
        public void DataTemplate_Created_Content_Should_Be_Logical_Child_After_ApplyTemplate()
        {
            TabControl target = new TabControl
            {
                Template = TabControlTemplate(),
                ContentTemplate = new FuncDataTemplate<string>((x, _) =>
                    new TextBlock { Tag = "bar", Text = x }),
                ItemsSource = new[] { "Foo" },
            };
            var root = new TestRoot(target);

            ApplyTemplate(target);
            target.ContentPart.UpdateChild();

            var content = Assert.IsType<TextBlock>(target.ContentPart.Child);
            Assert.Equal("bar", content.Tag);
            Assert.Same(target, content.GetLogicalParent());
            Assert.Single(target.GetLogicalChildren(), content);
        }

        [Fact]
        public void SelectedContentTemplate_Updates_After_New_ContentTemplate()
        {
            TabControl target = new TabControl
            {
                Template = TabControlTemplate(),
                ItemsSource = new[] { "Foo" },
            };
            var root = new TestRoot(target);

            ApplyTemplate(target);
            ((ContentPresenter)target.ContentPart).UpdateChild();

            Assert.Equal(null, Assert.IsType<TextBlock>(target.ContentPart.Child).Tag);

            target.ContentTemplate = new FuncDataTemplate<string>((x, _) =>
                    new TextBlock { Tag = "bar", Text = x });

            Assert.Equal("bar", Assert.IsType<TextBlock>(target.ContentPart.Child).Tag);
        }

        [Fact]
        public void Should_Not_Propagate_DataContext_To_TabItem_Content()
        {
            var dataContext = "DataContext";

            var tabItem = new TabItem();

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = dataContext,
                Items = { tabItem }
            };

            ApplyTemplate(target);

            Assert.NotEqual(dataContext, tabItem.Content);
        }

        [Fact]
        public void Can_Have_Empty_Tab_Control()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <TabControl Name='tabs' ItemsSource='{Binding Tabs}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var tabControl = window.FindControl<TabControl>("tabs");

                tabControl.DataContext = new { Tabs = new List<string>() };
                window.ApplyTemplate();

                Assert.Equal(0, tabControl.ItemsSource.Count());
            }
        }

        [Fact]
        public void Should_Have_Initial_SelectedValue()
        {
            var xaml = @"
        <TabControl
            xmlns='https://github.com/avaloniaui'
            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
            xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'
            x:DataType='TabItem'
            x:Name='tabs'
            Tag='World' 
            SelectedValue='{Binding $self.Tag}' 
            SelectedValueBinding='{Binding Header}'>
            <TabItem Header='Hello'/>
            <TabItem Header='World'/>
        </TabControl>";

            var tabControl = (TabControl)AvaloniaRuntimeXamlLoader.Load(xaml);

            Assert.Equal("World", tabControl.SelectedValue);
            Assert.Equal(1, tabControl.SelectedIndex);
        }

        [Fact]
        public void Tab_Navigation_Should_Move_To_First_TabItem_When_No_Anchor_Element_Selected()
        {
            var services = TestServices.StyledWindow.With(
                focusManager: new FocusManager(),
                keyboardDevice: () => new KeyboardDevice());
            using var app = UnitTestApplication.Start(services);

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items =
                {
                    new TabItem { Header = "foo" },
                    new TabItem { Header = "bar" },
                    new TabItem { Header = "baz" },
                }
            };

            var button = new Button
            {
                Content = "Button",
                [DockPanel.DockProperty] = Dock.Top,
            };

            var root = new TestRoot
            {
                Child = new DockPanel
                {
                    Children =
                    {
                        button,
                        target,
                    }
                }
            };

            var navigation = new KeyboardNavigationHandler();
            navigation.SetOwner(root);

            root.LayoutManager.ExecuteInitialLayoutPass();

            button.Focus();
            RaiseKeyEvent(button, Key.Tab);

            var item = target.ContainerFromIndex(0);
            Assert.Same(item, root.FocusManager.GetFocusedElement());
        }

        [Fact]
        public void Tab_Navigation_Should_Move_To_Anchor_TabItem()
        {
            var services = TestServices.StyledWindow.With(
                focusManager: new FocusManager(),
                keyboardDevice: () => new KeyboardDevice());
            using var app = UnitTestApplication.Start(services);

            var target = new TestTabControl
            {
                Template = TabControlTemplate(),
                Items =
                {
                    new TabItem { Header = "foo" },
                    new TabItem { Header = "bar" },
                    new TabItem { Header = "baz" },
                }
            };

            var button = new Button
            {
                Content = "Button",
                [DockPanel.DockProperty] = Dock.Top,
            };

            var root = new TestRoot
            {
                Width = 1000,
                Height = 1000,
                Child = new DockPanel
                {
                    Children =
                    {
                        button,
                        target,
                    }
                }
            };

            var navigation = new KeyboardNavigationHandler();
            navigation.SetOwner(root);

            root.LayoutManager.ExecuteInitialLayoutPass();

            button.Focus();
            target.Selection.AnchorIndex = 1;
            RaiseKeyEvent(button, Key.Tab);

            var item = target.ContainerFromIndex(1);
            Assert.Same(item, root.FocusManager.GetFocusedElement());

            RaiseKeyEvent(item, Key.Tab);

            Assert.Same(button, root.FocusManager.GetFocusedElement());

            target.Selection.AnchorIndex = 2;
            RaiseKeyEvent(button, Key.Tab);

            item = target.ContainerFromIndex(2);
            Assert.Same(item, root.FocusManager.GetFocusedElement());
        }

        [Fact]
        public void TabItem_Header_Should_Be_Settable_By_Style_When_DataContext_Is_Set()
        {
            var tabItem = new TabItem
            {
                DataContext = "Some DataContext"
            };

            _ = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<TabItem>())
                    {
                        Setters =
                        {
                            new Setter(HeaderedContentControl.HeaderProperty, "Header from style")
                        }
                    }
                },
                Child = tabItem
            };

            Assert.Equal("Header from style", tabItem.Header);
        }

        [Fact]
        public void TabItem_TabStripPlacement_Should_Be_Correctly_Set()
        {
            var items = new object[]
            {
                "Foo",
                new TabItem { Content = new TextBlock { Text = "Baz" } }
            };

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = "Base",
                ItemsSource = items
            };

            ApplyTemplate(target);

            var result = target.GetLogicalChildren()
                .OfType<TabItem>()
                .ToList();
            Assert.Collection(
                result,
                x => Assert.Equal(Dock.Top, x.TabStripPlacement),
                x => Assert.Equal(Dock.Top, x.TabStripPlacement)
            );

            target.TabStripPlacement = Dock.Right;
            result = target.GetLogicalChildren()
                .OfType<TabItem>()
                .ToList();
            Assert.Collection(
                result,
                x => Assert.Equal(Dock.Right, x.TabStripPlacement),
                x => Assert.Equal(Dock.Right, x.TabStripPlacement)
            );
        }
        
        [Fact]
        public void TabItem_TabStripPlacement_Should_Be_Correctly_Set_For_New_Items()
        {
            var items = new object[]
            {
                "Foo",
                new TabItem { Content = new TextBlock { Text = "Baz" } }
            };

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = "Base"
            };

            ApplyTemplate(target);

            target.ItemsSource = items;
            
            var result = target.GetLogicalChildren()
                .OfType<TabItem>()
                .ToList();
            Assert.Collection(
                result,
                x => Assert.Equal(Dock.Top, x.TabStripPlacement),
                x => Assert.Equal(Dock.Top, x.TabStripPlacement)
            );

            target.TabStripPlacement = Dock.Right;
            result = target.GetLogicalChildren()
                .OfType<TabItem>()
                .ToList();
            Assert.Collection(
                result,
                x => Assert.Equal(Dock.Right, x.TabStripPlacement),
                x => Assert.Equal(Dock.Right, x.TabStripPlacement)
            );
        }
        
        private static IControlTemplate TabControlTemplate()
        {
            return new FuncControlTemplate<TabControl>((parent, scope) =>
                new StackPanel
                {
                    Children =
                    {
                        new ItemsPresenter
                        {
                            Name = "PART_ItemsPresenter",
                        }.RegisterInNameScope(scope),
                        new ContentPresenter
                        {
                            Name = "PART_SelectedContentHost",
                            [!ContentPresenter.ContentProperty] = parent[!TabControl.SelectedContentProperty],
                            [!ContentPresenter.ContentTemplateProperty] = parent[!TabControl.SelectedContentTemplateProperty],
                        }.RegisterInNameScope(scope)
                    }
                });
        }

        private static IControlTemplate TabItemTemplate()
        {
            return new FuncControlTemplate<TabItem>((parent, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!TabItem.HeaderProperty],
                    [!ContentPresenter.ContentTemplateProperty] = parent[!TabItem.HeaderTemplateProperty]
                }.RegisterInNameScope(scope));
        }

        private static void Prepare(TabControl target)
        {
            ApplyTemplate(target);
            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));
        }

        private static void RaiseKeyEvent(Control target, Key key, KeyModifiers inputModifiers = 0)
        {
            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                KeyModifiers = inputModifiers,
                Key = key
            });
        }

        private static void ApplyTemplate(TabControl target)
        {
            target.ApplyTemplate();

            target.Presenter.ApplyTemplate();

            foreach (var tabItem in target.GetLogicalChildren().OfType<TabItem>())
            {
                tabItem.Template = TabItemTemplate();

                tabItem.ApplyTemplate();

                tabItem.Presenter.UpdateChild();
            }

            target.ContentPart.ApplyTemplate();
        }

        private class Item
        {
            public Item(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        private class TestTabControl : TabControl
        {
            protected override Type StyleKeyOverride => typeof(TabControl);
            public new ISelectionModel Selection => base.Selection;
        }
    }
}
