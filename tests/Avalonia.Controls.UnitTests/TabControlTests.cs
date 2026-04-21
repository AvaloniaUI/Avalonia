using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TabControlTests : ScopedTestBase
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
            using var app = Start();
            var items = new object[]
            {
                "Foo",
                new Item("Bar"),
                new TextBlock { Text = "Baz" },
                new TabItem { Content = "Qux" },
                new TabItem { Content = new TextBlock { Text = "Bob" } },
                new TabItem { DataContext = "Rob", Content = new TextBlock { Text = "Bob" } },
            };

            var target = new TabControl
            {
                DataContext = "Base",
                ItemsSource = items,
            };

            var root = CreateRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            var dataContext = ((TextBlock)target.ContentPart!.Child!).DataContext;
            Assert.Equal(items[0], dataContext);

            target.SelectedIndex = 1;
            dataContext = ((Button)target.ContentPart.Child).DataContext;
            Assert.Equal(items[1], dataContext);

            target.SelectedIndex = 2;
            dataContext = ((TextBlock)target.ContentPart.Child).DataContext;
            Assert.Equal("Base", dataContext);

            target.SelectedIndex = 3;
            dataContext = ((TextBlock)target.ContentPart.Child).DataContext;
            Assert.Equal("Qux", dataContext);

            target.SelectedIndex = 4;
            dataContext = target.ContentPart.DataContext;
            Assert.Equal("Base", dataContext);

            target.SelectedIndex = 5;
            dataContext = target.ContentPart.Child.DataContext;
            Assert.Equal("Rob", dataContext);
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

            Assert.Equal(new object?[] { null, null }, result);
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

            var page = Assert.IsType<TabItem>(target.SelectedItem);

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
            target.ContentPart!.UpdateChild();

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
            target.ContentPart!.UpdateChild();

            Assert.Equal(null, Assert.IsType<TextBlock>(target.ContentPart.Child).Tag);

            target.ContentTemplate = new FuncDataTemplate<string>((x, _) =>
                    new TextBlock { Tag = "bar", Text = x });

            Assert.Equal("bar", Assert.IsType<TextBlock>(target.ContentPart.Child).Tag);
        }

        [Fact]
        public void Previous_ContentTemplate_Is_Not_Reused_When_TabItem_Changes()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            int templatesBuilt = 0;

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                Items =
                {
                    TabItemFactory("First tab content"),
                    TabItemFactory("Second tab content"),
                },
            };

            var root = new TestRoot(target);
            ApplyTemplate(target);

            target.SelectedIndex = 0;
            target.SelectedIndex = 1;

            Assert.Equal(2, templatesBuilt);

            TabItem TabItemFactory(object content) => new()
            {
                Content = content,
                ContentTemplate = new FuncDataTemplate<object>((actual, ns) =>
                {
                    Assert.Equal(content, actual);
                    templatesBuilt++;
                    return new Border();
                })
            };
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
                var tabControl = window.GetControl<TabControl>("tabs");

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
            Assert.NotNull(item);
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

        [Theory]
        [InlineData(Key.A, "a", 1)]
        [InlineData(Key.L, "l", 2)]
        [InlineData(Key.D, "d", 0)]
        public void Should_TabControl_Recognizes_AccessKey(Key accessKey, string accessKeySymbol, int selectedTabIndex)
        {
            var kd = new KeyboardDevice();
            using (UnitTestApplication.Start(TestServices.StyledWindow
                       .With(
                           accessKeyHandler: () => new AccessKeyHandler(),
                           keyboardDevice: () => kd)
                   ))
            {
                var impl = CreateMockTopLevelImpl();

                var tabControl = new TabControl()
                {
                    Template = TabControlTemplate(),
                    Items =
                    {
                        new TabItem
                        {
                            Header = "General",
                        },
                        new TabItem { Header = "_Arch" },
                        new TabItem { Header = "_Leaf"},
                        new TabItem { Header = "_Disabled", IsEnabled = false },
                    }
                };
                kd.SetFocusedElement((TabItem?)tabControl.Items[selectedTabIndex], NavigationMethod.Unspecified, KeyModifiers.None);
                
                var root = new TestTopLevel(impl.Object)
                {
                    Template = CreateTemplate(),
                    Content = tabControl,
                };

                root.ApplyTemplate();
                root.Presenter!.UpdateChild();
                ApplyTemplate(tabControl);

                KeyDown(root, Key.LeftAlt);
                KeyDown(root, accessKey, accessKeySymbol, KeyModifiers.Alt);
                KeyUp(root, accessKey, accessKeySymbol, KeyModifiers.Alt);
                KeyUp(root, Key.LeftAlt);

                Assert.Equal(selectedTabIndex, tabControl.SelectedIndex);
            }

            static FuncControlTemplate<TestTopLevel> CreateTemplate()
            {
                return new FuncControlTemplate<TestTopLevel>((x, scope) =>
                    new ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty),
                        [~ContentPresenter.ContentTemplateProperty] = new TemplateBinding(ContentControl.ContentTemplateProperty)
                    }.RegisterInNameScope(scope));
            }

            static Mock<ITopLevelImpl> CreateMockTopLevelImpl(bool setupProperties = false)
            {
                var topLevel = new Mock<ITopLevelImpl>();
                if (setupProperties)
                    topLevel.SetupAllProperties();
                topLevel.Setup(x => x.RenderScaling).Returns(1);
                topLevel.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
                return topLevel;
            }

            static void KeyDown(IInputElement target, Key key, string? keySymbol = null, KeyModifiers modifiers = KeyModifiers.None)
            {
                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = key,
                    KeySymbol = keySymbol,
                    KeyModifiers = modifiers,
                });
            }

            static void KeyUp(IInputElement target, Key key, string? keySymbol = null, KeyModifiers modifiers = KeyModifiers.None)
            {
                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyUpEvent,
                    Key = key,
                    KeySymbol = keySymbol,
                    KeyModifiers = modifiers,
                });
            }
        }

        [Fact]
        public void PageTransition_Is_Null_By_Default()
        {
            var target = new TabControl { Template = TabControlTemplate() };
            Assert.Null(target.PageTransition);
        }

        [Fact]
        public void PageTransition_Round_Trips()
        {
            var transition = new CrossFade(TimeSpan.FromMilliseconds(100));
            var target = new TabControl
            {
                Template = TabControlTemplate(),
                PageTransition = transition,
            };
            Assert.Same(transition, target.PageTransition);
        }

        [Fact]
        public void PageTransition_Start_Is_Called_When_Tab_Switches()
        {
            using var app = Start();

            var transition = new Mock<IPageTransition>();
            transition
                .Setup(t => t.Start(
                    It.IsAny<Visual?>(), It.IsAny<Visual?>(),
                    It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var target = new TabControl
            {
                PageTransition = transition.Object,
                Items =
                {
                    new TabItem { Name = "first", Content = "Alpha" },
                    new TabItem { Name = "second", Content = "Beta" },
                },
            };

            var root = CreateRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            // Switch tab — triggers shouldTransition = true and InvalidateArrange
            target.SelectedIndex = 1;

            // Execute layout pass to invoke ArrangeOverride, which fires the transition
            root.LayoutManager.ExecuteLayoutPass();

            transition.Verify(
                t => t.Start(
                    It.IsAny<Visual?>(), It.IsAny<Visual?>(),
                    It.Is<bool>(f => f),   // forward = true (index 1 > 0)
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public void PageTransition_Forward_Is_False_When_Switching_To_Earlier_Tab()
        {
            using var app = Start();

            var transition = new Mock<IPageTransition>();
            transition
                .Setup(t => t.Start(
                    It.IsAny<Visual?>(), It.IsAny<Visual?>(),
                    It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var target = new TabControl
            {
                PageTransition = transition.Object,
                Items =
                {
                    new TabItem { Name = "first", Content = "Alpha" },
                    new TabItem { Name = "second", Content = "Beta" },
                    new TabItem { Name = "third", Content = "Gamma" },
                },
            };

            var root = CreateRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            // Go forward to tab 2
            target.SelectedIndex = 2;
            root.LayoutManager.ExecuteLayoutPass();

            // Now go backward to tab 0
            target.SelectedIndex = 0;
            root.LayoutManager.ExecuteLayoutPass();

            transition.Verify(
                t => t.Start(
                    It.IsAny<Visual?>(), It.IsAny<Visual?>(),
                    It.Is<bool>(f => !f),  // forward = false (index 0 < 2)
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public void Interrupted_PageTransition_Can_Select_Original_Control_Before_Previous_Transition_Completes()
        {
            using var app = Start();

            var firstPage = new ContentPage { Content = "Alpha" };
            var secondPage = new ContentPage { Content = "Beta" };
            var starts = new List<(object? FromContent, object? ToContent, bool Forward)>();
            var transitionGate = new TaskCompletionSource();
            var transition = new Mock<IPageTransition>();
            transition
                .Setup(t => t.Start(
                    It.IsAny<Visual?>(), It.IsAny<Visual?>(),
                    It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<Visual?, Visual?, bool, CancellationToken>((from, to, forward, _) =>
                {
                    starts.Add((
                        (from as ContentPresenter)?.Content,
                        (to as ContentPresenter)?.Content,
                        forward));
                })
                .Returns(transitionGate.Task);

            var target = new TabControl
            {
                PageTransition = transition.Object,
                Items =
                {
                    new TabItem { Name = "first", Content = firstPage },
                    new TabItem { Name = "second", Content = secondPage },
                },
            };

            var root = CreateRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            target.SelectedIndex = 1;
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Single(starts);
            Assert.Same(firstPage, starts[0].FromContent);
            Assert.Same(secondPage, starts[0].ToContent);
            Assert.True(starts[0].Forward);

            var exception = Record.Exception(() => target.SelectedIndex = 0);

            Assert.Null(exception);

            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(2, starts.Count);
            Assert.Null(starts[1].FromContent);
            Assert.Same(firstPage, starts[1].ToContent);
            Assert.False(starts[1].Forward);
            Assert.Same(firstPage, target.SelectedContent);
        }

        [Fact]
        public void Pending_PageTransition_Can_Select_Original_Control_Before_Transition_Starts()
        {
            using var app = Start();

            var firstPage = new ContentPage { Content = "Alpha" };
            var secondPage = new ContentPage { Content = "Beta" };
            var starts = new List<(object? FromContent, object? ToContent, bool Forward)>();
            var transition = new Mock<IPageTransition>();
            transition
                .Setup(t => t.Start(
                    It.IsAny<Visual?>(), It.IsAny<Visual?>(),
                    It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<Visual?, Visual?, bool, CancellationToken>((from, to, forward, _) =>
                {
                    starts.Add((
                        (from as ContentPresenter)?.Content,
                        (to as ContentPresenter)?.Content,
                        forward));
                })
                .Returns(Task.CompletedTask);

            var target = new TabControl
            {
                PageTransition = transition.Object,
                Items =
                {
                    new TabItem { Name = "first", Content = firstPage },
                    new TabItem { Name = "second", Content = secondPage },
                },
            };

            var root = CreateRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            target.SelectedIndex = 1;
            var exception = Record.Exception(() => target.SelectedIndex = 0);

            Assert.Null(exception);

            root.LayoutManager.ExecuteLayoutPass();

            Assert.Single(starts);
            Assert.Null(starts[0].FromContent);
            Assert.Same(firstPage, starts[0].ToContent);
            Assert.False(starts[0].Forward);
            Assert.Same(firstPage, target.SelectedContent);
        }

        [Fact]
        public void Interrupted_PageTransition_Clears_Reused_Control_From_Owning_SelectedContentHost()
        {
            using var app = Start();

            var firstPage = new ContentPage { Content = "Alpha" };
            var secondPage = new ContentPage { Content = "Beta" };
            var transition = new Mock<IPageTransition>();
            transition
                .Setup(t => t.Start(
                    It.IsAny<Visual?>(), It.IsAny<Visual?>(),
                    It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var target = new TabControl
            {
                Items =
                {
                    new TabItem { Name = "first", Content = firstPage },
                    new TabItem { Name = "second", Content = secondPage },
                },
            };

            var root = CreateRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            target.SelectedIndex = 1;
            root.LayoutManager.ExecuteLayoutPass();

            var primary = target.GetVisualDescendants()
                .OfType<ContentPresenter>()
                .Single(x => x.Name == "PART_SelectedContentHost");
            var secondary = target.GetVisualDescendants()
                .OfType<ContentPresenter>()
                .Single(x => x.Name == "PART_SelectedContentHost2");

            // Simulate the stale presenter ownership that can happen when tab changes
            // interrupt a transition: the page is still parented by the named content
            // host, but the active field no longer points at that host.
            primary.SetContentWithDataContext(firstPage, null);
            secondary.IsVisible = false;
            SetPrivateField(target, "_contentPart", secondary);
            SetPrivateField(target, "_contentPresenter2", secondary);

            target.PageTransition = transition.Object;
            var exception = Record.Exception(() => target.SelectedIndex = 0);

            Assert.Null(exception);
            Assert.Same(firstPage, target.SelectedContent);
            Assert.Null(primary.Content);
            Assert.Same(firstPage, secondary.Content);
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
                        new Panel
                        {
                            Children =
                            {
                                new ContentPresenter
                                {
                                    Name = "PART_SelectedContentHost2",
                                    IsVisible = false,
                                }.RegisterInNameScope(scope),
                                new ContentPresenter
                                {
                                    Name = "PART_SelectedContentHost",
                                }.RegisterInNameScope(scope),
                            }
                        }
                    }
                });
        }

        private static void SetPrivateField<T>(TabControl target, string name, T value)
        {
            var field = typeof(TabControl).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(target, value);
        }

        private static IControlTemplate TabItemTemplate()
        {
            return new FuncControlTemplate<TabItem>((parent, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [~ContentPresenter.ContentProperty] = new TemplateBinding(TabItem.HeaderProperty),
                    [~ContentPresenter.ContentTemplateProperty] = new TemplateBinding(TabItem.HeaderTemplateProperty),
                    RecognizesAccessKey = true,
                }.RegisterInNameScope(scope));
        }

        private static IControlTemplate TabItemWithIconTemplate()
        {
            return new FuncControlTemplate<TabItem>((parent, scope) =>
                new StackPanel
                {
                    Children =
                    {
                        new ContentPresenter
                        {
                            Name = "PART_IconPresenter",
                            [~ContentPresenter.ContentProperty] = new TemplateBinding(TabItem.IconProperty),
                            [~ContentPresenter.ContentTemplateProperty] = new TemplateBinding(TabItem.IconTemplateProperty),
                        }.RegisterInNameScope(scope),
                        new ContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                            [~ContentPresenter.ContentProperty] = new TemplateBinding(TabItem.HeaderProperty),
                            [~ContentPresenter.ContentTemplateProperty] = new TemplateBinding(TabItem.HeaderTemplateProperty),
                            RecognizesAccessKey = true,
                        }.RegisterInNameScope(scope),
                    }
                });
        }

        private static ControlTheme CreateTabControlControlTheme()
        {
            return new ControlTheme(typeof(TabControl))
            {
                Setters =
                {
                    new Setter(TabControl.TemplateProperty, TabControlTemplate()),
                },
            };
        }

        private static ControlTheme CreateTabItemControlTheme()
        {
            return new ControlTheme(typeof(TabItem))
            {
                Setters =
                {
                    new Setter(TabItem.TemplateProperty, TabItemTemplate()),
                },
            };
        }
        
        private static TestRoot CreateRoot(Control child)
        {
            return new TestRoot
            {
                Resources =
                {
                    { typeof(TabControl), CreateTabControlControlTheme() },
                    { typeof(TabItem), CreateTabItemControlTheme() },
                },
                DataTemplates =
                {
                    new FuncDataTemplate<Item>((x, _) => new Button { Content = x.Value })
                },
                Child = child,
            };
        }

        private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
        {
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

            target.Presenter!.ApplyTemplate();

            foreach (var tabItem in target.GetLogicalChildren().OfType<TabItem>())
            {
                tabItem.Template = TabItemTemplate();

                tabItem.ApplyTemplate();

                tabItem.Presenter!.UpdateChild();
            }

            target.ContentPart!.ApplyTemplate();
        }

        private IDisposable Start()
        {
            return UnitTestApplication.Start(
                TestServices.MockThreadingInterface.With(
                    fontManagerImpl: new HeadlessFontManagerStub(),
                    keyboardDevice: () => new KeyboardDevice(),
                    keyboardNavigation: () => new KeyboardNavigationHandler(),
                    inputManager: new InputManager(),
                    renderInterface: new HeadlessPlatformRenderInterface(),
                    textShaperImpl: new HarfBuzzTextShaper(),
                    assetLoader: new StandardAssetLoader()));
        }

        [Fact]
        public void Switching_Tab_Should_Preserve_DataContext_Binding_On_UserControl_Content()
        {
            // Issue #18280: When switching tabs, a UserControl inside a TabItem has its
            // DataContext set to null, causing two-way bindings on child controls (like
            // DataGrid.SelectedItem) to propagate null back to the view model.
            // Verify that after switching away and back, the DataContext binding still
            // resolves correctly.
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var viewModel = new TabDataContextViewModel { SelectedItem = "Item1" };

            // Create a UserControl with an explicit DataContext binding,
            // matching the issue scenario.
            var userControl = new UserControl
            {
                [~UserControl.DataContextProperty] = new Binding("SelectedItem"),
            };

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = viewModel,
                Items =
                {
                    new TabItem
                    {
                        Header = "Tab1",
                        Content = userControl,
                    },
                    new TabItem
                    {
                        Header = "Tab2",
                        Content = "Other content",
                    },
                },
            };

            var root = new TestRoot(target);
            Prepare(target);

            // Verify initial state
            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("Item1", userControl.DataContext);

            // Switch to second tab and back
            target.SelectedIndex = 1;
            target.SelectedIndex = 0;

            // The UserControl's DataContext binding should still resolve correctly.
            Assert.Equal("Item1", userControl.DataContext);

            // Verify the binding is still live by changing the source property.
            viewModel.SelectedItem = "Item2";
            Assert.Equal("Item2", userControl.DataContext);
        }

        [Fact]
        public void TabItem_Child_DataContext_Binding_Should_Work()
        {
            // Issue #20845: When a DataContext binding is placed on the child of a TabItem,
            // the DataContext is null. The binding hasn't resolved when the content's
            // DataContext is captured in UpdateSelectedContent, so the captured value is null.
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var viewModel = new MainViewModel();

            var tab1View = new UserControl();
            tab1View.Bind(UserControl.DataContextProperty, new Binding("Tab1"));

            // Add a child TextBlock that binds to a property on Tab1ViewModel.
            var textBlock = new TextBlock();
            textBlock.Bind(TextBlock.TextProperty, new Binding("Name"));
            tab1View.Content = textBlock;

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = viewModel,
                Items =
                {
                    new TabItem
                    {
                        Header = "Tab1",
                        Content = tab1View,
                    },
                },
            };

            var root = new TestRoot(target);
            Prepare(target);

            // The UserControl's DataContext should be the Tab1ViewModel.
            Assert.Same(viewModel.Tab1, tab1View.DataContext);

            // The TextBlock should display the Name from Tab1ViewModel.
            Assert.Equal("Tab 1 message here", textBlock.Text);
        }

        [Fact]
        public void TabItem_Child_With_DataContext_Binding_Should_Propagate_To_Children()
        {
            // Issue #20845 (comment): Putting the DataContext binding on the TabItem itself
            // is also broken. The child should inherit the TabItem's DataContext.
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var viewModel = new MainViewModel();

            var textBlock = new TextBlock();
            textBlock.Bind(TextBlock.TextProperty, new Binding("Name"));
            var tab1View = new UserControl { Content = textBlock };

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = viewModel,
                Items =
                {
                    new TabItem
                    {
                        Header = "Tab1",
                        [~TabItem.DataContextProperty] = new Binding("Tab1"),
                        Content = tab1View,
                    },
                },
            };

            var root = new TestRoot(target);
            Prepare(target);

            // The TabItem's DataContext should be the Tab1ViewModel.
            var tabItem = (TabItem)target.Items[0]!;
            Assert.Same(viewModel.Tab1, tabItem.DataContext);

            // The UserControl should inherit the TabItem's DataContext.
            Assert.Same(viewModel.Tab1, tab1View.DataContext);

            // The TextBlock should display the Name from Tab1ViewModel.
            Assert.Equal("Tab 1 message here", textBlock.Text);
        }

        [Fact]
        public void Switching_Tabs_Should_Not_Null_Out_DataContext_Bound_Properties()
        {
            // Issue #20845: DataContext binding should survive tab switches.
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var viewModel = new MainViewModel();

            var tab1View = new UserControl();
            tab1View.Bind(UserControl.DataContextProperty, new Binding("Tab1"));
            var textBlock = new TextBlock();
            textBlock.Bind(TextBlock.TextProperty, new Binding("Name"));
            tab1View.Content = textBlock;

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = viewModel,
                Items =
                {
                    new TabItem
                    {
                        Header = "Tab1",
                        Content = tab1View,
                    },
                    new TabItem
                    {
                        Header = "Tab2",
                        Content = "Other content",
                    },
                },
            };

            var root = new TestRoot(target);
            Prepare(target);

            Assert.Same(viewModel.Tab1, tab1View.DataContext);
            Assert.Equal("Tab 1 message here", textBlock.Text);

            // Switch to tab 2 and back
            target.SelectedIndex = 1;
            target.SelectedIndex = 0;

            // DataContext binding should still be resolved correctly.
            Assert.Same(viewModel.Tab1, tab1View.DataContext);
            Assert.Equal("Tab 1 message here", textBlock.Text);
        }

        [Fact]
        public void Content_Should_Not_Temporarily_Get_Wrong_DataContext_When_Switching_Tabs()
        {
            // When ContentPart.Content is set, ContentPresenter.UpdateChild clears its
            // DataContext before we can set it to the container's DataContext. This causes
            // the content to briefly inherit TabControl's DataContext instead of TabItem's.
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var viewModel = new MainViewModel();

            var tab1View = new UserControl();
            var tab2View = new UserControl();

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = viewModel,
                Items =
                {
                    new TabItem
                    {
                        Header = "Tab1",
                        [~TabItem.DataContextProperty] = new Binding("Tab1"),
                        Content = tab1View,
                    },
                    new TabItem
                    {
                        Header = "Tab2",
                        [~TabItem.DataContextProperty] = new Binding("Tab2"),
                        Content = tab2View,
                    },
                },
            };

            var root = new TestRoot(target);
            Prepare(target);

            Assert.Same(viewModel.Tab1, tab1View.DataContext);

            // Track all DataContext values the new content receives during the switch.
            var dataContexts = new List<object?>();
            tab2View.PropertyChanged += (s, e) =>
            {
                if (e.Property == StyledElement.DataContextProperty)
                    dataContexts.Add(e.NewValue);
            };

            target.SelectedIndex = 1;

            // tab2View should only have received the correct DataContext (Tab2ViewModel).
            // It should NOT have temporarily received the TabControl's DataContext (MainViewModel).
            Assert.All(dataContexts, dc => Assert.Same(viewModel.Tab2, dc));
            Assert.Same(viewModel.Tab2, tab2View.DataContext);
        }

        [Fact]
        public void Transition_Should_Not_Apply_New_DataContext_To_Old_Content()
        {
            // When a PageTransition is set, the old content stays in ContentPart while the
            // new content goes into _contentPresenter2. The DataContext subscription for the
            // new container should not update ContentPart's DataContext (which still holds
            // the old content).
            using var app = Start();

            var viewModel = new MainViewModel();

            var tab1View = new UserControl();
            var tab2View = new UserControl();

            var transition = new Mock<IPageTransition>();
            transition
                .Setup(t => t.Start(
                    It.IsAny<Visual?>(), It.IsAny<Visual?>(),
                    It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var target = new TabControl
            {
                PageTransition = transition.Object,
                DataContext = viewModel,
                Items =
                {
                    new TabItem
                    {
                        Header = "Tab1",
                        [~TabItem.DataContextProperty] = new Binding("Tab1"),
                        Content = tab1View,
                    },
                    new TabItem
                    {
                        Header = "Tab2",
                        [~TabItem.DataContextProperty] = new Binding("Tab2"),
                        Content = tab2View,
                    },
                },
            };

            var root = CreateRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Same(viewModel.Tab1, tab1View.DataContext);

            // Track all DataContext values the OLD content receives during the transition.
            var oldContentDataContexts = new List<object?>();
            tab1View.PropertyChanged += (s, e) =>
            {
                if (e.Property == StyledElement.DataContextProperty)
                    oldContentDataContexts.Add(e.NewValue);
            };

            // Switch tab — triggers transition
            target.SelectedIndex = 1;
            root.LayoutManager.ExecuteLayoutPass();

            // The old content (tab1View) should NOT have received Tab2's DataContext.
            Assert.DoesNotContain(viewModel.Tab2, oldContentDataContexts);
        }

        [Fact]
        public void ContentTemplate_With_Control_Content_Should_Set_DataContext_To_Content()
        {
            // When a TabItem has a ContentTemplate and its Content is a Control, the
            // ContentPresenter should set DataContext = content (so the template can bind
            // to the control's properties), not the TabItem's DataContext.
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var viewModel = new MainViewModel();
            var userControl = new UserControl { Tag = "my-content" };

            TextBlock? templateChild = null;
            var contentTemplate = new FuncDataTemplate<UserControl>((x, _) =>
            {
                templateChild = new TextBlock();
                templateChild.Bind(TextBlock.TextProperty, new Binding("Tag"));
                return templateChild;
            });

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = viewModel,
                Items =
                {
                    new TabItem
                    {
                        Header = "Tab1",
                        [~TabItem.DataContextProperty] = new Binding("Tab1"),
                        ContentTemplate = contentTemplate,
                        Content = userControl,
                    },
                },
            };

            var root = new TestRoot(target);
            Prepare(target);

            // The ContentPresenter's DataContext should be the content (UserControl),
            // not the TabItem's DataContext (Tab1ViewModel), because ContentTemplate is set.
            Assert.Same(userControl, target.ContentPart!.DataContext);
            Assert.NotNull(templateChild);
            Assert.Equal("my-content", templateChild!.Text);
        }

        [Fact]
        public void ContentTemplate_With_Control_Content_Should_Set_DataContext_To_Content_After_Tab_Switch()
        {
            // Same as above but verifies the behavior after switching tabs.
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var viewModel = new MainViewModel();
            var userControl = new UserControl { Tag = "my-content" };

            TextBlock? templateChild = null;
            var contentTemplate = new FuncDataTemplate<UserControl>((x, _) =>
            {
                templateChild = new TextBlock();
                templateChild.Bind(TextBlock.TextProperty, new Binding("Tag"));
                return templateChild;
            });

            var target = new TabControl
            {
                Template = TabControlTemplate(),
                DataContext = viewModel,
                Items =
                {
                    new TabItem
                    {
                        Header = "Tab1",
                        [~TabItem.DataContextProperty] = new Binding("Tab1"),
                        ContentTemplate = contentTemplate,
                        Content = userControl,
                    },
                    new TabItem
                    {
                        Header = "Tab2",
                        Content = "Other content",
                    },
                },
            };

            var root = new TestRoot(target);
            Prepare(target);

            Assert.Same(userControl, target.ContentPart!.DataContext);

            // Switch away and back.
            target.SelectedIndex = 1;
            target.SelectedIndex = 0;

            // DataContext should still be the content, not the TabItem's DataContext.
            Assert.Same(userControl, target.ContentPart!.DataContext);
            Assert.NotNull(templateChild);
            Assert.Equal("my-content", templateChild!.Text);
        }

        private class TabDataContextViewModel : NotifyingBase
        {
            private string? _selectedItem;

            public string? SelectedItem
            {
                get => _selectedItem;
                set => SetField(ref _selectedItem, value);
            }
        }

        private class MainViewModel
        {
            public Tab1ViewModel Tab1 { get; set; } = new();
            public Tab2ViewModel Tab2 { get; set; } = new();
        }

        private class Tab1ViewModel
        {
            public string Name { get; set; } = "Tab 1 message here";
        }

        private class Tab2ViewModel
        {
            public string Name { get; set; } = "Tab 2 message here";
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

        [Fact]
        public void TabItem_IconTemplate_Creates_Content_From_NonControl_Icon()
        {
            var tabItem = new TabItem
            {
                Icon = "home",
                IconTemplate = new FuncDataTemplate<object>((val, _) =>
                    new TextBlock { Text = (string)val }),
                Template = TabItemWithIconTemplate(),
            };

            var root = new TestRoot { Child = tabItem };
            tabItem.ApplyTemplate();
            tabItem.Presenter!.UpdateChild();

            var iconPresenter = tabItem.GetTemplateChildren().OfType<ContentPresenter>().First(x => x.Name == "PART_IconPresenter");
            Assert.NotNull(iconPresenter);
            Assert.Equal("home", iconPresenter!.Content);
            Assert.NotNull(iconPresenter.ContentTemplate);

            iconPresenter.UpdateChild();
            var textBlock = iconPresenter.Child as TextBlock;
            Assert.NotNull(textBlock);
            Assert.Equal("home", textBlock!.Text);
        }

        [Fact]
        public void TabItem_Icon_Without_Template_Renders_Control_Directly()
        {
            var icon = new Avalonia.Controls.Shapes.Path
            {
                Data = new Avalonia.Media.EllipseGeometry { Rect = new Rect(0, 0, 10, 10) }
            };
            var tabItem = new TabItem
            {
                Icon = icon,
                Template = TabItemWithIconTemplate(),
            };

            var root = new TestRoot { Child = tabItem };
            tabItem.ApplyTemplate();
            tabItem.Presenter!.UpdateChild();

            var iconPresenter = tabItem.GetTemplateChildren().OfType<ContentPresenter>().First(x => x.Name == "PART_IconPresenter");
            Assert.NotNull(iconPresenter);
            Assert.Same(icon, iconPresenter!.Content);
            Assert.Null(iconPresenter.ContentTemplate);
        }

        [Fact]
        public void TabItem_Icon_Change_Updates_Presenter_Content()
        {
            var tabItem = new TabItem
            {
                Icon = "first",
                Template = TabItemWithIconTemplate(),
            };

            var root = new TestRoot { Child = tabItem };
            tabItem.ApplyTemplate();
            tabItem.Presenter!.UpdateChild();

            var iconPresenter = tabItem.GetTemplateChildren().OfType<ContentPresenter>().First(x => x.Name == "PART_IconPresenter");
            Assert.Equal("first", iconPresenter!.Content);

            tabItem.Icon = "second";
            Assert.Equal("second", iconPresenter.Content);
        }

        [Fact]
        public void TabItem_IndicatorTemplate_DefaultIsNull()
        {
            var tabItem = new TabItem();
            Assert.Null(tabItem.IndicatorTemplate);
        }

        [Fact]
        public void TabItem_IndicatorTemplate_RoundTrips()
        {
            var template = new FuncDataTemplate<object>((_, _) => new Border());
            var tabItem = new TabItem { IndicatorTemplate = template };
            Assert.Same(template, tabItem.IndicatorTemplate);
        }

        [Fact]
        public void TabItem_IndicatorTemplate_CanBeSetToNull()
        {
            var template = new FuncDataTemplate<object>((_, _) => new Border());
            var tabItem = new TabItem { IndicatorTemplate = template };
            tabItem.IndicatorTemplate = null;
            Assert.Null(tabItem.IndicatorTemplate);
        }

        [Fact]
        public void TabControl_IndicatorTemplate_DefaultIsNull()
        {
            var tc = new TabControl();
            Assert.Null(tc.IndicatorTemplate);
        }

        [Fact]
        public void TabControl_IndicatorTemplate_RoundTrips()
        {
            var template = new FuncDataTemplate<object>((_, _) => new Border());
            var tc = new TabControl { IndicatorTemplate = template };
            Assert.Same(template, tc.IndicatorTemplate);
        }

        [Fact]
        public void TabControl_IndicatorTemplate_CanBeSetToNull()
        {
            var template = new FuncDataTemplate<object>((_, _) => new Border());
            var tc = new TabControl { IndicatorTemplate = template };
            tc.IndicatorTemplate = null;
            Assert.Null(tc.IndicatorTemplate);
        }

        [Fact]
        public void TabControl_IndicatorTemplate_DoesNotOverwrite_UserSetTabItemIndicatorTemplate()
        {
            var tabItems = new[]
            {
                new TabItem { Header = "A" },
                new TabItem { Header = "B" },
            };
            var userTemplate = new FuncDataTemplate<object>((_, _) => new Border());
            tabItems[0].IndicatorTemplate = userTemplate;

            var tabControlTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock());
            var tc = new TabControl
            {
                ItemsSource = tabItems,
                IndicatorTemplate = tabControlTemplate,
                Template = new FuncControlTemplate<TabControl>((_, scope) =>
                {
                    var ip = new ItemsPresenter { Name = "PART_ItemsPresenter" };
                    scope.Register("PART_ItemsPresenter", ip);
                    var cp = new ContentPresenter { Name = "PART_SelectedContentHost" };
                    scope.Register("PART_SelectedContentHost", cp);
                    return new Panel { Children = { ip, cp } };
                })
            };

            var root = new TestRoot { Child = tc };
            tc.ApplyTemplate();
            tc.Presenter?.ApplyTemplate();

            // TabItem with a local value must keep it
            Assert.Same(userTemplate, tabItems[0].IndicatorTemplate);
            // TabItem without a local value gets the TabControl template
            Assert.Same(tabControlTemplate, tabItems[1].IndicatorTemplate);
        }
    }
}
