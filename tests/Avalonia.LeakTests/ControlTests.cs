using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.LeakTests
{
    public class ControlTests : ScopedTestBase
    {
        [ReleaseFact]
        public void Canvas_Is_Freed()
        {
            using (Start())
            {
                static WeakReference Run()
                {
                    var canvas = new Canvas();
                    var window = new Window
                    {
                        Content = canvas
                    };

                    window.Show();

                    // Do a layout and make sure that Canvas gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Canvas>(window.Presenter!.Child);

                    // Clear the content and ensure the Canvas is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return new WeakReference(canvas);
                }

                var weakCanvas = Run();
                Assert.True(weakCanvas.IsAlive);

                CollectGarbage();

                Assert.False(weakCanvas.IsAlive);
            }
        }

        [ReleaseFact]
        public void Named_Canvas_Is_Freed()
        {
            using (Start())
            {
                static WeakReference Run()
                {
                    var scope = new NameScope();
                    var canvas = new Canvas { Name = "foo" };
                    var window = new Window
                    {
                        Content = canvas.RegisterInNameScope(scope)
                    };
                    NameScope.SetNameScope(window, scope);

                    window.Show();

                    // Do a layout and make sure that Canvas gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Canvas>(window.Find<Canvas>("foo"));
                    Assert.IsType<Canvas>(window.Presenter!.Child);

                    // Clear the content and ensure the Canvas is removed.
                    window.Content = null;
                    NameScope.SetNameScope(window, null);

                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return new WeakReference(canvas);
                }

                var weakCanvas = Run();
                Assert.True(weakCanvas.IsAlive);

                CollectGarbage();

                Assert.False(weakCanvas.IsAlive);
            }
        }

        [ReleaseFact]
        public void ScrollViewer_With_Content_Is_Freed()
        {
            using (Start())
            {
                static WeakReference Run()
                {
                    var canvas = new Canvas();
                    var window = new Window
                    {
                        Content = new ScrollViewer
                        {
                            Content = canvas
                        }
                    };

                    window.Show();

                    // Do a layout and make sure that ScrollViewer gets added to visual tree and its
                    // template applied.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<ScrollViewer>(window.Presenter!.Child);
                    Assert.IsType<Canvas>(((ScrollViewer)window.Presenter!.Child).Presenter!.Child);

                    // Clear the content and ensure the ScrollViewer is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return new WeakReference(canvas);
                }

                var weakCanvas = Run();
                Assert.True(weakCanvas.IsAlive);

                CollectGarbage();

                Assert.False(weakCanvas.IsAlive);
            }
        }

        [ReleaseFact]
        public void TextBox_Is_Freed()
        {
            using (Start())
            {
                static WeakReference Run()
                {
                    var textBox = new TextBox();
                    var window = new Window
                    {
                        Content = textBox
                    };

                    window.Show();

                    // Do a layout and make sure that TextBox gets added to visual tree and its 
                    // template applied.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<TextBox>(window.Presenter!.Child);
                    Assert.NotEmpty(window.Presenter.Child.GetVisualChildren());

                    // Clear the content and ensure the TextBox is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return new WeakReference(textBox);
                }

                var weakTextBox = Run();
                Assert.True(weakTextBox.IsAlive);

                CollectGarbage();

                Assert.False(weakTextBox.IsAlive);
            }
        }

        [ReleaseFact]
        public void TextBox_With_Xaml_Binding_Is_Freed()
        {
            using (Start())
            {
                static (WeakReference, WeakReference) Run()
                {
                    var node = new Node { Name = "foo" };
                    var window = new Window
                    {
                        DataContext = node,
                        Content = new TextBox()
                    };

                    var binding = new Binding
                    {
                        Path = "Name"
                    };

                    var textBox = (TextBox)window.Content;
                    textBox.Bind(TextBox.TextProperty, binding);

                    window.Show();

                    // Do a layout and make sure that TextBox gets added to visual tree and its
                    // Text property set.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<TextBox>(window.Presenter!.Child);
                    Assert.Equal("foo", ((TextBox)window.Presenter.Child).Text);

                    // Clear the content and DataContext and ensure the TextBox is removed.
                    window.Content = null;
                    window.DataContext = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return (new WeakReference(node), new WeakReference(textBox));
                }

                var (weakNode, weakTextBox) = Run();
                Assert.True(weakNode.IsAlive);
                Assert.True(weakTextBox.IsAlive);

                CollectGarbage();

                Assert.False(weakNode.IsAlive);
                Assert.False(weakTextBox.IsAlive);
            }
        }

        [Fact]
        public void TextBox_Class_Listeners_Are_Freed()
        {
            using (Start())
            {
                TextBox textBox;

                var window = new Window
                {
                    Content = textBox = new TextBox()
                };

                window.Show();

                // Do a layout and make sure that TextBox gets added to visual tree and its 
                // template applied.
                window.LayoutManager.ExecuteInitialLayoutPass();
                Assert.Same(textBox, window.Presenter!.Child);

                // Get the border from the TextBox template.
                var border = textBox.GetTemplateChildren().FirstOrDefault(x => x.Name == "border");

                // The TextBox should have subscriptions to its Classes collection from the
                // default theme.
                Assert.NotEqual(0, textBox.Classes.ListenerCount);

                // Clear the content and ensure the TextBox is removed.
                window.Content = null;
                window.LayoutManager.ExecuteLayoutPass();
                Assert.Null(window.Presenter.Child);

                // Check that the TextBox has no subscriptions to its Classes collection.
                Assert.Null(((INotifyCollectionChangedDebug)textBox.Classes).GetCollectionChangedSubscribers());
            }
        }

        [ReleaseFact]
        public void TreeView_Is_Freed()
        {
            using (Start())
            {
                static WeakReference Run()
                {
                    var nodes = new[]
                    {
                        new Node
                        {
                            Children = new[] { new Node() },
                        }
                    };

                    TreeView target;

                    var window = new Window
                    {
                        Content = target = new TreeView
                        {
                            DataTemplates =
                            {
                                new FuncTreeDataTemplate<Node>(
                                    (x, _) => new TextBlock { Text = x.Name },
                                    x => x.Children ?? [])
                            },
                            ItemsSource = nodes
                        }
                    };

                    window.Show();

                    // Do a layout and make sure that TreeViewItems get realized.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.Single(target.GetRealizedContainers());

                    // Clear the content and ensure the TreeView is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter!.Child);

                    return new WeakReference(target);
                }

                var weakTreeView = Run();
                Assert.True(weakTreeView.IsAlive);

                CollectGarbage();

                Assert.False(weakTreeView.IsAlive);
            }
        }

        [ReleaseFact]
        public void Slider_Is_Freed()
        {
            using (Start())
            {
                static WeakReference Run()
                {
                    var slider = new Slider();
                    var window = new Window
                    {
                        Content = slider
                    };

                    window.Show();

                    // Do a layout and make sure that Slider gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Slider>(window.Presenter!.Child);

                    // Clear the content and ensure the Slider is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return new WeakReference(slider);
                }

                var weakSlider = Run();
                Assert.True(weakSlider.IsAlive);

                CollectGarbage();

                Assert.False(weakSlider.IsAlive);
            }
        }

        [ReleaseFact]
        public void TabItem_Is_Freed()
        {
            using (Start())
            {
                static WeakReference Run()
                {
                    var tabItem = new TabItem();
                    var window = new Window
                    {
                        Content = new TabControl
                        {
                            ItemsSource = new[] { tabItem }
                        }
                    };

                    window.Show();

                    // Do a layout and make sure that TabControl and TabItem gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    var tabControl = Assert.IsType<TabControl>(window.Presenter!.Child);
                    Assert.IsType<TabItem>(tabControl.Presenter!.Panel!.Children[0]);

                    // Clear the items and ensure the TabItem is removed.
                    tabControl.ItemsSource = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Empty(tabControl.Presenter.Panel.Children);

                    return new WeakReference(tabItem);
                }

                var weakTabItem = Run();
                Assert.True(weakTabItem.IsAlive);

                CollectGarbage();

                Assert.False(weakTabItem.IsAlive);
            }
        }

        [Fact]
        public void RendererIsDisposed()
        {
            using (Start())
            {
                var screen1 = new Mock<Screen>(1.75, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 966)), true);
                var screens = new Mock<IScreenImpl>();
                screens.Setup(x => x.ScreenFromWindow(It.IsAny<IWindowBaseImpl>())).Returns(screen1.Object);

                var impl = new Mock<IWindowImpl>();
                impl.Setup(r => r.TryGetFeature(It.IsAny<Type>())).Returns((object?)null);
                impl.SetupGet(x => x.RenderScaling).Returns(1);
                impl.SetupProperty(x => x.Closed);
                impl.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
                impl.Setup(x => x.Dispose()).Callback(() => impl.Object.Closed!());
                impl.Setup(x => x.TryGetFeature(It.Is<Type>(t => t == typeof(IScreenImpl)))).Returns(screens.Object);

                AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatform>()
                    .ToConstant(new MockWindowingPlatform(() => impl.Object));
                var window = new Window()
                {
                    Content = new Button()
                };
                window.Show();
                window.Close();
                Assert.True(((CompositingRenderer)window.Renderer).IsDisposed);
            }
        }

        [ReleaseFact]
        public void Control_With_Style_RenderTransform_Is_Freed()
        {
            // # Issue #3545
            using (Start())
            {
                static WeakReference Run()
                {
                    var window = new Window
                    {
                        Styles =
                        {
                            new Style(x => x.OfType<Canvas>())
                            {
                                Setters =
                                {
                                    new Setter
                                    {
                                        Property = Visual.RenderTransformProperty,
                                        Value = new RotateTransform(45),
                                    }
                                }
                            }
                        },
                        Content = new Canvas()
                    };

                    window.Show();

                    // Do a layout and make sure that Canvas gets added to visual tree with
                    // its render transform.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    var canvas = Assert.IsType<Canvas>(window.Presenter!.Child);
                    Assert.IsType<RotateTransform>(canvas.RenderTransform);

                    // Clear the content and ensure the Canvas is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return new WeakReference(canvas);
                }

                var weakCanvas = Run();
                Assert.True(weakCanvas.IsAlive);

                CollectGarbage();

                Assert.False(weakCanvas.IsAlive);
            }
        }

        [ReleaseFact]
        public void Attached_ContextMenu_Is_Freed()
        {
            using (Start())
            {
                (WeakReference, WeakReference, WeakReference) AttachShowAndDetachContextMenu(Control control)
                {
                    var menuItem1 = new MenuItem { Header = "Foo" };
                    var menuItem2 = new MenuItem { Header = "Foo" };
                    var contextMenu = new ContextMenu
                    {
                        Items =
                        {
                            menuItem1,
                            menuItem2
                        }
                    };

                    control.ContextMenu = contextMenu;
                    contextMenu.Open(control);
                    contextMenu.Close();
                    control.ContextMenu = null;

                    return (new WeakReference(menuItem1), new WeakReference(menuItem2), new WeakReference(contextMenu));
                }

                var window = new Window { Focusable = true };
                window.Show();

                Assert.Same(window, window.FocusManager!.GetFocusedElement());

                var (weakMenuItem1, weakMenuItem2, weakContextMenu) = AttachShowAndDetachContextMenu(window);
                Assert.True(weakMenuItem1.IsAlive);
                Assert.True(weakMenuItem2.IsAlive);
                Assert.True(weakContextMenu.IsAlive);

                Mock.Get(window.PlatformImpl!).Invocations.Clear();
                CollectGarbage();

                Assert.False(weakMenuItem1.IsAlive);
                Assert.False(weakMenuItem2.IsAlive);
                Assert.False(weakContextMenu.IsAlive);
            }
        }
        
        [ReleaseFact]
        public void Attached_Control_From_ContextMenu_Is_Freed()
        {
            using (Start())
            {
                var contextMenu = new ContextMenu();

                WeakReference Run()
                {
                    var textBlock = new TextBlock
                    {
                        ContextMenu = contextMenu
                    };
                    var window = new Window
                    {
                        Content = textBlock
                    };

                    window.Show();

                    // Do a layout and make sure that TextBlock gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<TextBlock>(window.Presenter!.Child);

                    // Clear the content and ensure the TextBlock is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return new WeakReference(textBlock);
                }

                var weakTextBlock = Run();
                Assert.True(weakTextBlock.IsAlive);

                CollectGarbage();

                Assert.False(weakTextBlock.IsAlive);
            }
        }

        [ReleaseFact]
        public void Standalone_ContextMenu_Is_Freed()
        {
            using (Start())
            {
                (WeakReference, WeakReference, WeakReference) BuildAndShowContextMenu(Control control)
                {
                    var menuItem1 = new MenuItem { Header = "Foo" };
                    var menuItem2 = new MenuItem { Header = "Foo" };
                    var contextMenu = new ContextMenu
                    {
                        Items =
                        {
                            menuItem1,
                            menuItem2
                        }
                    };

                    contextMenu.Open(control);
                    contextMenu.Close();

                    return (new WeakReference(menuItem1), new WeakReference(menuItem2), new WeakReference(contextMenu));
                }

                var window = new Window { Focusable = true };
                window.Show();

                Assert.Same(window, window.FocusManager!.GetFocusedElement());

                var (weakMenuItem1, weakMenuItem2, weakContextMenu1) = BuildAndShowContextMenu(window);
                var (weakMenuItem3, weakMenuItem4, weakContextMenu2) = BuildAndShowContextMenu(window);

                Assert.True(weakMenuItem1.IsAlive);
                Assert.True(weakMenuItem2.IsAlive);
                Assert.True(weakContextMenu1.IsAlive);
                Assert.True(weakMenuItem3.IsAlive);
                Assert.True(weakMenuItem4.IsAlive);
                Assert.True(weakContextMenu2.IsAlive);

                Mock.Get(window.PlatformImpl!).Invocations.Clear();
                CollectGarbage();

                Assert.False(weakMenuItem1.IsAlive);
                Assert.False(weakMenuItem2.IsAlive);
                Assert.False(weakContextMenu1.IsAlive);
                Assert.False(weakMenuItem3.IsAlive);
                Assert.False(weakMenuItem4.IsAlive);
                Assert.False(weakContextMenu2.IsAlive);
            }
        }

        [ReleaseFact]
        public void Path_Is_Freed()
        {
            using (Start())
            {
                var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };

                WeakReference Run()
                {
                    var path = new Path
                    {
                        Data = geometry
                    };
                    var window = new Window
                    {
                        Content = path
                    };

                    window.Show();

                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Path>(window.Presenter!.Child);

                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return new WeakReference(path);
                }

                var weakPath = Run();
                Assert.True(weakPath.IsAlive);

                CollectGarbage();

                Assert.False(weakPath.IsAlive);
            }
        }
        
        [ReleaseFact]
        public void Polyline_WithObservableCollectionPointsBinding_Is_Freed()
        {
            using (Start())
            {
                var observableCollection = new ObservableCollection<Point>(){new()};

                WeakReference Run()
                {
                    var polyline = new Polyline
                    {
                        Points = observableCollection
                    };
                    var window = new Window
                    {
                        Content = polyline
                    };

                    window.Show();

                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Polyline>(window.Presenter!.Child);

                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return new WeakReference(polyline);
                }

                var weakPolyline = Run();
                Assert.True(weakPolyline.IsAlive);

                CollectGarbage();

                Assert.False(weakPolyline.IsAlive);

                // We are keeping collection alive to simulate a resource that outlives the control.
                GC.KeepAlive(observableCollection);
            }
        }

        [ReleaseFact]
        public void ElementName_Binding_In_DataTemplate_Is_Freed()
        {
            using (Start())
            {
                // Height of ListBoxItem content + padding.
                const double ListBoxItemHeight = 12;
                const double TextBoxHeight = 25;

                var items = new ObservableCollection<int>(Enumerable.Range(0, 10));
                NameScope ns;
                TextBox tb;
                ListBox lb;

                var weakCanvases = new List<WeakReference>();

                var window = new Window
                {
                    [NameScope.NameScopeProperty] = ns = new NameScope(),
                    Width = 100,
                    Height = (items.Count * ListBoxItemHeight) + TextBoxHeight,
                    Content = new DockPanel
                    {
                        Children =
                        {
                            (tb = new TextBox
                            {
                                Name = "tb",
                                Text = "foo",
                                Height = TextBoxHeight,
                                [DockPanel.DockProperty] = Dock.Top,
                            }),
                            (lb = new ListBox
                            {
                                ItemsSource = items,
                                ItemTemplate = new FuncDataTemplate<int>((_, _) =>
                                {
                                    var canvas = new Canvas
                                    {
                                        Width = 10,
                                        Height = 10,
                                        [!Control.TagProperty] = new Binding
                                        {
                                            ElementName = "tb",
                                            Path = "Text",
                                            NameScope = new WeakReference<INameScope?>(ns),
                                        }
                                    };
                                    weakCanvases.Add(new WeakReference(canvas));
                                    return canvas;
                                }),
                                Padding = new Thickness(0),
                            })
                        }
                    }
                };

                tb.RegisterInNameScope(ns);

                window.Show();
                window.LayoutManager.ExecuteInitialLayoutPass();

                void AssertInitialItemState()
                {
                    var item0 = (ListBoxItem)lb.GetRealizedContainers().First();
                    var canvas0 = (Canvas)item0.Presenter!.Child!;
                    Assert.Equal("foo", canvas0.Tag);
                }

                Assert.Equal(10, lb.GetRealizedContainers().Count());
                AssertInitialItemState();

                items.Clear();
                window.LayoutManager.ExecuteLayoutPass();

                Assert.Empty(lb.GetRealizedContainers());
                Assert.Equal(10, weakCanvases.Count);

                foreach (var weakReference in weakCanvases)
                    Assert.True(weakReference.IsAlive);

                CollectGarbage();

                foreach (var weakReference in weakCanvases)
                    Assert.False(weakReference.IsAlive);
            }
        }

        [ReleaseFact]
        public void HotKeyManager_Should_Release_Reference_When_Control_Detached()
        {
            using (Start())
            {
                static WeakReference Run()
                {
                    var gesture1 = new KeyGesture(Key.A, KeyModifiers.Control);
                    var tl = new Window
                    {
                        Content = new ItemsControl(),
                    };

                    tl.Show();

                    var button = new Button();
                    tl.Content = button;
                    tl.Template = CreateWindowTemplate();
                    tl.ApplyTemplate();
                    tl.Presenter!.ApplyTemplate();
                    HotKeyManager.SetHotKey(button, gesture1);

                    // Detach the button from the logical tree, so there is no reference to it
                    tl.Content = null;
                    tl.ApplyTemplate();

                    return new WeakReference(button);
                }

                var weakButton = Run();
                Assert.True(weakButton.IsAlive);

                CollectGarbage();

                Assert.False(weakButton.IsAlive);
            }
        }

        [ReleaseFact]
        public void HotKeyManager_Should_Release_Reference_When_Control_In_Item_Template_Detached()
        {
            using (Start())
            {
                static List<WeakReference> Run()
                {
                    var gesture1 = new KeyGesture(Key.A, KeyModifiers.Control);

                    var tl = new Window { SizeToContent = SizeToContent.WidthAndHeight, IsVisible = true };
                    var lm = tl.LayoutManager;
                    tl.Show();

                    var keyGestures = new AvaloniaList<KeyGesture> { gesture1 };
                    var weakButtons = new List<WeakReference>();
                    var listBox = new ListBox
                    {
                        Width = 100,
                        Height = 100,
                        // Create a button with binding to the KeyGesture in the template and add it to references list
                        ItemTemplate = new FuncDataTemplate(typeof(KeyGesture), (o, _) =>
                        {
                            var keyGesture = o as KeyGesture;
                            var button = new Button
                            {
                                DataContext = keyGesture,
                                [!Button.HotKeyProperty] = new Binding("")
                            };
                            weakButtons.Add(new WeakReference(button));
                            return button;
                        })
                    };
                    // Add the listbox and render it
                    tl.Content = listBox;
                    lm.ExecuteInitialLayoutPass();
                    listBox.ItemsSource = keyGestures;
                    lm.ExecuteLayoutPass();

                    // Let the button detach when clearing the source items
                    keyGestures.Clear();
                    lm.ExecuteLayoutPass();

                    // Add it again to double check,and render
                    keyGestures.Add(gesture1);
                    lm.ExecuteLayoutPass();

                    keyGestures.Clear();
                    lm.ExecuteLayoutPass();

                    return weakButtons;
                }

                var weakButtons = Run();

                Assert.NotEmpty(weakButtons);

                foreach (var weakReference in weakButtons)
                    Assert.True(weakReference.IsAlive);

                CollectGarbage();

                foreach (var weakReference in weakButtons)
                    Assert.False(weakReference.IsAlive);
            }
        }

        [ReleaseFact]
        public void ToolTip_Is_Freed()
        {
            using (Start())
            {
                static (WeakReference, WeakReference) Run()
                {
                    var window = new Window();
                    TextBlock? textBlock = null;
                    var source = new Button
                    {
                        Template = new FuncControlTemplate<Button>((_, _) =>
                        {
                            Assert.Null(textBlock);
                            textBlock = new TextBlock
                            {
                                [~TextBlock.TextProperty] =
                                    new TemplateBinding(ContentControl.ContentProperty)
                            };
                            return new Decorator
                            {
                                [ToolTip.TipProperty] = textBlock
                            };
                        }),
                    };

                    window.Content = source;
                    window.Show();

                    var templateChild = (Decorator)source.GetVisualChildren().Single();
                    ToolTip.SetIsOpen(templateChild, true);
                    var toolTip = templateChild.GetValue(ToolTip.ToolTipProperty);
                    Assert.NotNull(toolTip);

                    ToolTip.SetIsOpen(templateChild, false);

                    // Detach the button from the logical tree, so there is no reference to it
                    window.Content = null;
                    
                    // Mock keep reference on a Popup via InvocationsCollection. So let's clear it before. 
                    Mock.Get(window.PlatformImpl!).Invocations.Clear();
                    
                    return (new WeakReference(toolTip), new WeakReference(textBlock));
                }

                var (weakTooltip, weakTextBlock) = Run();
                Assert.True(weakTooltip.IsAlive);
                Assert.True(weakTextBlock.IsAlive);

                CollectGarbage();

                Assert.False(weakTooltip.IsAlive);
                Assert.False(weakTextBlock.IsAlive);
            }
        }
        
        [ReleaseFact]
        public void Flyout_Is_Freed()
        {
            using (Start())
            {
                static (WeakReference, WeakReference) Run()
                {
                    var window = new Window();
                    var source = new Button
                    {
                        Template = new FuncControlTemplate<Button>((_, _) =>
                            new Button
                            {
                                Flyout = new Flyout
                                {
                                    Content = new TextBlock
                                    {
                                        [~TextBlock.TextProperty] = new TemplateBinding(ContentControl.ContentProperty)
                                    }
                                }
                            }),
                    };

                    window.Content = source;
                    window.Show();

                    var templateChild = (Button)source.GetVisualChildren().Single();
                    var flyout = Assert.IsType<Flyout>(templateChild.Flyout);
                    var textBlock = Assert.IsType<TextBlock>(flyout.Content);

                    flyout.ShowAt(templateChild);
                    
                    flyout.Hide();

                    // Detach the button from the logical tree, so there is no reference to it
                    window.Content = null;

                    // Mock keep reference on a Popup via InvocationsCollection. So let's clear it before. 
                    Mock.Get(window.PlatformImpl!).Invocations.Clear();
                    
                    return (new WeakReference(flyout), new WeakReference(textBlock));
                }

                var (weakFlyout, weakTextBlock) = Run();
                Assert.True(weakFlyout.IsAlive);
                Assert.True(weakTextBlock.IsAlive);

                CollectGarbage();

                Assert.False(weakFlyout.IsAlive);
                Assert.False(weakTextBlock.IsAlive);
            }
        }
        
        [ReleaseFact]
        public void LayoutTransformControl_Is_Freed()
        {
            using (Start())
            {
                var transform = new RotateTransform { Angle = 90 };

                (WeakReference, WeakReference) Run()
                {
                    var canvas = new Canvas();
                    var layoutTransformControl = new LayoutTransformControl
                    {
                        LayoutTransform = transform,
                        Child = canvas
                    };
                    var window = new Window
                    {
                        Content = layoutTransformControl
                    };

                    window.Show();

                    // Do a layout and make sure that LayoutTransformControl gets added to visual tree
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<LayoutTransformControl>(window.Presenter!.Child);
                    Assert.NotEmpty(window.Presenter.Child.GetVisualChildren());

                    // Clear the content and ensure the LayoutTransformControl is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return (new WeakReference(layoutTransformControl), new WeakReference(canvas));
                }

                var (weakLayoutTransformControl, weakCanvas) = Run();
                Assert.True(weakLayoutTransformControl.IsAlive);
                Assert.True(weakCanvas.IsAlive);

                CollectGarbage();

                Assert.False(weakLayoutTransformControl.IsAlive);
                Assert.False(weakCanvas.IsAlive);

                // We are keeping transform alive to simulate a resource that outlives the control.
                GC.KeepAlive(transform);
            }
        }

        private static FuncControlTemplate CreateWindowTemplate()
        {
            return new FuncControlTemplate<Window>((parent, scope) =>
            {
                return new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
                }.RegisterInNameScope(scope);
            });
        }

        private IDisposable Start()
        {
            static void Cleanup()
            {
                // KeyboardDevice holds a reference to the focused item.
                KeyboardDevice.Instance?.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
                
                // Empty the dispatcher queue.
                Dispatcher.UIThread.RunJobs();
            }

            return new CompositeDisposable
            {
                Disposable.Create(Cleanup),
                UnitTestApplication.Start(TestServices.StyledWindow.With(
                    keyboardDevice: () => new KeyboardDevice(),
                    inputManager: new InputManager()))
            };
        }

        private static void CollectGarbage()
        {
            // Process all Loaded events to free control reference(s)
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            GC.Collect();

            Dispatcher.UIThread.RunJobs();
            GC.Collect();
        }

        private class Node
        {
            public string? Name { get; set; }
            public IEnumerable<Node>? Children { get; set; }
        }

    }
}
