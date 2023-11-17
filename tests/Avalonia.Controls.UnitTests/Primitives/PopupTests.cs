using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Moq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;
using Avalonia.Input;
using Avalonia.Rendering;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class PopupTests
    {
        protected bool UsePopupHost;

        [Fact]
        public void Popup_Open_Without_Target_Should_Attach_Itself_Later()
        {
            using (CreateServices())
            {
                int openedEvent = 0;
                var target = new Popup();
                target.Opened += (s, a) => openedEvent++;
                target.IsOpen = true;

                var window = PreparedWindow(target);
                window.Show();
                Assert.Equal(1, openedEvent);
            }
        }

        [Fact]
        public void Popup_Without_TopLevel_Shouldnt_Call_Open()
        {
            int openedEvent = 0;
            var target = new Popup();
            target.Opened += (s, a) => openedEvent++;
            target.IsOpen = true;

            Assert.Equal(0, openedEvent);
        }

        [Fact]
        public void Opening_Popup_Shouldnt_Throw_When_Not_In_Visual_Tree()
        {
            var target = new Popup();
            target.IsOpen = true;
        }

        [Fact]
        public void Opening_Popup_Shouldnt_Throw_When_In_Tree_Without_TopLevel()
        {
            Control c = new Control();
            var target = new Popup();
            ((ISetLogicalParent)target).SetParent(c);
            target.IsOpen = true;
        }

        [Fact]
        public void Setting_Child_Should_Set_Child_Controls_LogicalParent()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;

            Assert.Equal(child.Parent, target);
            Assert.Equal(((ILogical)child).LogicalParent, target);
        }

        [Fact]
        public void Clearing_Child_Should_Clear_Child_Controls_Parent()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;
            target.Child = null;

            Assert.Null(child.Parent);
            Assert.Null(((ILogical)child).LogicalParent);
        }

        [Fact]
        public void Child_Control_Should_Appear_In_LogicalChildren()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;

            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Clearing_Child_Should_Remove_From_LogicalChildren()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;
            target.Child = null;

            Assert.Equal(new ILogical[0], ((ILogical)target).LogicalChildren.ToList());
        }

        [Fact]
        public void Setting_Child_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Popup();
            var child = new Control();
            var called = false;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            target.Child = child;

            Assert.True(called);
        }

        [Fact]
        public void Clearing_Child_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Popup();
            var child = new Control();
            var called = false;

            target.Child = child;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            target.Child = null;

            Assert.True(called);
        }

        [Fact]
        public void Changing_Child_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new Popup();
            var child1 = new Control();
            var child2 = new Control();
            var called = false;

            target.Child = child1;

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            target.Child = child2;

            Assert.True(called);
        }

        [Fact]
        public void Setting_Child_Should_Not_Set_Childs_VisualParent()
        {
            var target = new Popup();
            var child = new Control();

            target.Child = child;

            Assert.Null(((Visual)child).VisualParent);
        }

        [Fact]
        public void PopupRoot_Should_Initially_Be_Null()
        {
            using (CreateServices())
            {
                var target = new Popup();

                Assert.Null(((Visual)target.Host));
            }
        }

        [Fact]
        public void PopupRoot_Should_Have_Popup_As_LogicalParent()
        {
            using (CreateServices())
            {
                var target = new Popup() {PlacementTarget = PreparedWindow()};

                target.Open();

                Assert.Equal(target, ((Visual)target.Host).Parent);
                Assert.Equal(target, ((Visual)target.Host).GetLogicalParent());
            }
        }

        [Fact]
        public void PopupRoot_Should_Be_Detached_From_Logical_Tree_When_Popup_Is_Detached()
        {
            using (CreateServices())
            {
                var target = new Popup() {Placement = PlacementMode.Pointer};
                var root = PreparedWindow(target);

                target.Open();

                var popupRoot = (ILogical)((Visual)target.Host);

                Assert.True(popupRoot.IsAttachedToLogicalTree);
                root.Content = null;
                Assert.False(((ILogical)target).IsAttachedToLogicalTree);
            }
        }

        [Fact]
        public void Popup_Open_Should_Raise_Single_Opened_Event()
        {
            using (CreateServices())
            {
                var window = PreparedWindow();
                var target = new Popup() {Placement = PlacementMode.Pointer};

                window.Content = target;

                int openedCount = 0;

                target.Opened += (sender, args) =>
                {
                    openedCount++;
                };

                target.Open();

                Assert.Equal(1, openedCount);
            }
        }

        [Fact]
        public void Popup_Close_Should_Raise_Single_Closed_Event()
        {
            using (CreateServices())
            {
                var window = PreparedWindow();
                var target = new Popup() {Placement = PlacementMode.Pointer};

                window.Content = target;
                window.ApplyTemplate();
                target.Open();

                int closedCount = 0;

                target.Closed += (sender, args) =>
                {
                    closedCount++;
                };

                target.Close();

                Assert.Equal(1, closedCount);
            }
        }

        [Fact]
        public void Popup_Close_On_Closed_Popup_Should_Not_Raise_Closed_Event()
        {
            using (CreateServices())
            {
                var window = PreparedWindow();
                var target = new Popup() { Placement = PlacementMode.Pointer };

                window.Content = target;
                window.ApplyTemplate();
                
                int closedCount = 0;

                target.Closed += (sender, args) =>
                {
                    closedCount++;
                };

                target.Close();
                target.Close();
                target.Close();
                target.Close();

                Assert.Equal(0, closedCount);
            }
        }

        [Fact]
        public void ContentControl_With_Popup_In_Template_Should_Set_TemplatedParent()
        {
            // Test uses OverlayPopupHost default template
            using (CreateServices())
            {
                PopupContentControl target;
                var root = PreparedWindow(target = new PopupContentControl
                {
                    Content = new Border(),
                    Template = new FuncControlTemplate<PopupContentControl>(PopupContentControlTemplate),
                });
                root.Show();

                target.ApplyTemplate();

                var popup = (Popup)target.GetTemplateChildren().First(x => x.Name == "popup");
                popup.Open();

                var popupRoot = (Control)popup.Host;
                popupRoot.Measure(Size.Infinity);
                popupRoot.Arrange(new Rect(popupRoot.DesiredSize));

                var children = popupRoot.GetVisualDescendants().ToList();
                var types = children.Select(x => x.GetType().Name).ToList();

                if (UsePopupHost)
                {
                    Assert.Equal(
                        new[]
                        {
                            "LayoutTransformControl",
                            "VisualLayerManager",
                            "ContentPresenter",
                            "ContentPresenter",
                            "Border",
                        },
                        types);
                }
                else
                {
                    Assert.Equal(
                        new[]
                        {
                            "LayoutTransformControl",
                            "Panel",
                            "Border",
                            "VisualLayerManager",
                            "ContentPresenter",
                            "ContentPresenter",
                            "Border",
                        },
                        types);
                }

                var templatedParents = children
                    .OfType<Control>()
                    .Select(x => x.TemplatedParent).ToList();

                if (UsePopupHost)
                {
                    Assert.Equal(
                        new object[]
                        {
                            popupRoot,
                            popupRoot,
                            popupRoot,
                            target,
                            null,
                        },
                        templatedParents);
                }
                else
                {
                    Assert.Equal(
                        new object[]
                        {
                            popupRoot,
                            popupRoot,
                            popupRoot,
                            popupRoot,
                            popupRoot,
                            target,
                            null,
                        },
                        templatedParents);
                }
            }
        }

        [Fact]
        public void ItemsControl_With_Popup_In_Template_Should_Set_TemplatedParent()
        {
            // Test uses OverlayPopupHost default template
            using (CreateServices())
            {
                PopupItemsControl target;
                var item = new Border();
                var root = PreparedWindow(target = new PopupItemsControl
                {
                    Items = { item },
                    Template = new FuncControlTemplate<PopupItemsControl>(PopupItemsControlTemplate),
                }); ;
                root.Show();

                target.ApplyTemplate();

                var popup = (Popup)target.GetTemplateChildren().First(x => x.Name == "popup");
                popup.Open();

                var popupRoot = (Control)popup.Host;
                popupRoot.Measure(Size.Infinity);
                popupRoot.Arrange(new Rect(popupRoot.DesiredSize));

                var children = popupRoot.GetVisualDescendants().ToList();
                var types = children.Select(x => x.GetType().Name).ToList();

                if (UsePopupHost)
                {
                    Assert.Equal(
                        new[]
                        {
                            "LayoutTransformControl",
                            "VisualLayerManager",
                            "ContentPresenter",
                            "ItemsPresenter",
                            "StackPanel",
                            "Border",
                        },
                        types);
                }
                else
                {
                    Assert.Equal(
                        new[]
                        {
                            "LayoutTransformControl",
                            "Panel",
                            "Border",
                            "VisualLayerManager",
                            "ContentPresenter",
                            "ItemsPresenter",
                            "StackPanel",
                            "Border",
                        },
                        types);
                }

                var templatedParents = children
                    .OfType<Control>()
                    .Select(x => x.TemplatedParent).ToList();

                if (UsePopupHost)
                {
                    Assert.Equal(
                        new object[]
                        {
                            popupRoot,
                            popupRoot,
                            popupRoot,
                            target,
                            target,
                            null,
                        },
                        templatedParents);
                }
                else
                {
                    Assert.Equal(
                        new object[]
                        {
                            popupRoot,
                            popupRoot,
                            popupRoot,
                            popupRoot,
                            popupRoot,
                            target,
                            target,
                            null,
                        },
                        templatedParents);
                }
            }
        }

        [Fact]
        public void Should_Not_Overwrite_TemplatedParent_Of_Item_In_ItemsControl_With_Popup_On_Second_Open()
        {
            // Test uses OverlayPopupHost default template
            using (CreateServices())
            {
                PopupItemsControl target;
                var item = new Border();
                var root = PreparedWindow(target = new PopupItemsControl
                {
                    Items = { item },
                    Template = new FuncControlTemplate<PopupItemsControl>(PopupItemsControlTemplate),
                });
                root.Show();

                target.ApplyTemplate();

                var popup = (Popup)target.GetTemplateChildren().First(x => x.Name == "popup");
                popup.Open();

                var popupRoot = (Control)popup.Host;
                popupRoot.Measure(Size.Infinity);
                popupRoot.Arrange(new Rect(popupRoot.DesiredSize));

                Assert.Null(item.TemplatedParent);

                popup.Close();
                popup.Open();

                Assert.Null(item.TemplatedParent);
            }
        }

        [Fact]
        public void DataContextBeginUpdate_Should_Not_Be_Called_For_Controls_That_Dont_Inherit()
        {
            using (CreateServices())
            {
                TestControl child;
                var popup = new Popup
                {
                    Child = child = new TestControl(),
                    DataContext = "foo",
                    PlacementTarget = PreparedWindow()
                };

                var beginCalled = false;
                child.DataContextBeginUpdate += (s, e) => beginCalled = true;

                // Test for #1245. Here, the child's logical parent is the popup but it's not yet
                // attached to a visual tree because the popup hasn't been opened.
                Assert.Same(popup, ((ILogical)child).LogicalParent);
                Assert.Same(popup, child.InheritanceParent);
                Assert.Null(child.GetVisualRoot());

                popup.Open();

                // #1245 was caused by the fact that DataContextBeginUpdate was called on `target`
                // when the PopupRoot was created, even though PopupRoot isn't the
                // InheritanceParent of child.
                Assert.False(beginCalled);
            }
        }
        
        [Fact]
        public void Popup_Host_Type_Should_Match_Platform_Preference()
        {
            using (CreateServices())
            {
                var target = new Popup() {PlacementTarget = PreparedWindow()};

                target.Open();
                if (UsePopupHost)
                    Assert.IsType<OverlayPopupHost>(target.Host);
                else
                    Assert.IsType<PopupRoot>(target.Host);
            }
        }

        [Fact]
        public void OverlayDismissEventPassThrough_Should_Pass_Event_To_Window_Contents()
        {
            using (CreateServices())
            {
                var compositor = RendererMocks.CreateDummyCompositor();
                var platform = AvaloniaLocator.Current.GetRequiredService<IWindowingPlatform>();
                var windowImpl = Mock.Get(platform.CreateWindow());
                windowImpl.Setup(x => x.Compositor).Returns(compositor);
                var hitTester = new Mock<IHitTester>();

                var window = new Window(windowImpl.Object)
                {
                    HitTesterOverride = hitTester.Object
                };
                window.ApplyStyling();
                window.ApplyTemplate();

                var target = new Popup() 
                { 
                    PlacementTarget = window ,
                    IsLightDismissEnabled = true,
                    OverlayDismissEventPassThrough = true,
                };

                var raised = 0;
                var border = new Border();
                window.Content = border;

                hitTester.Setup(x =>
                    x.HitTestFirst(new Point(10, 15), window, It.IsAny<Func<Visual, bool>>()))
                    .Returns(border);

                border.PointerPressed += (s, e) =>
                {
                    Assert.Same(border, e.Source);
                    ++raised;
                };

                target.Open();
                Assert.True(target.IsOpen);

                var e = CreatePointerPressedEventArgs(window, new Point(10, 15));
                var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
                overlay.RaiseEvent(e);

                Assert.Equal(1, raised);
                Assert.False(target.IsOpen);
            }
        }

        [Fact]
        public void Focusable_Controls_In_Popup_Should_Get_Focus()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();

                var tb = new TextBox();
                var b = new Button();
                var p = new Popup
                {
                    PlacementTarget = window,
                    Child = new StackPanel
                    {
                        Children =
                        {
                            tb,
                            b
                        }
                    }
                };
                ((ISetLogicalParent)p).SetParent(p.PlacementTarget);
                window.Show();

                p.Open();

                if(p.Host is OverlayPopupHost host)
                {
                    //Need to measure/arrange for visual children to show up
                    //in OverlayPopupHost
                    host.Measure(Size.Infinity);
                    host.Arrange(new Rect(host.DesiredSize));
                }

                tb.Focus();

                var focusManager = TopLevel.GetTopLevel(tb)!.FocusManager;
                tb = Assert.IsType<TextBox>(focusManager.GetFocusedElement());

                //Ensure focus remains in the popup
                var nextFocus = KeyboardNavigationHandler.GetNext(tb, NavigationDirection.Next);

                Assert.True(nextFocus == b);

                p.Close();
            }
        }

        [Fact]
        public void Closing_Popup_Sets_Focus_On_PlacementTarget()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();
                window.Focusable = true;

                var tb = new TextBox();
                var p = new Popup
                {
                    PlacementTarget = window,
                    Child = tb
                };

                window.Content = p;
                window.Show();
                window.Focus();
                p.Open();

                if (p.Host is OverlayPopupHost host)
                {
                    //Need to measure/arrange for visual children to show up
                    //in OverlayPopupHost
                    host.Measure(Size.Infinity);
                    host.Arrange(new Rect(host.DesiredSize));
                }

                tb.Focus();

                p.Close();

                var focusManager = window.FocusManager;
                var focus = focusManager.GetFocusedElement();
                Assert.Same(window, focus);
            }
        }

        [Fact]
        public void Prog_Close_Popup_NoLightDismiss_Doesnt_Move_Focus_To_PlacementTarget()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();

                var windowTB = new TextBox();
                window.Content = windowTB;

                var popupTB = new TextBox();
                var p = new Popup
                {
                    PlacementTarget = window,
                    IsLightDismissEnabled = false,
                    Child = popupTB
                };
                ((ISetLogicalParent)p).SetParent(p.PlacementTarget);
                window.Show();

                p.Open();

                if (p.Host is OverlayPopupHost host)
                {
                    //Need to measure/arrange for visual children to show up
                    //in OverlayPopupHost
                    host.Measure(Size.Infinity);
                    host.Arrange(new Rect(host.DesiredSize));
                }

                popupTB.Focus();

                windowTB.Focus();

                var focusManager = window.FocusManager;
                var focus = focusManager.GetFocusedElement();

                Assert.True(focus == windowTB);

                p.Close();

                Assert.True(focus == windowTB);
            }
        }

        [Fact]
        public void Popup_Should_Not_Follow_Placement_Target_On_Window_Move_If_Pointer()
        {
            using (CreateServices())
            {
                var popup = new Popup
                {
                    Width = 400,
                    Height = 200,
                    Placement = PlacementMode.Pointer
                };
                var window = PreparedWindow(popup);
                window.Show();
                popup.Open();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender);

                var raised = false;
                if (popup.Host is PopupRoot popupRoot)
                {
                    popupRoot.PositionChanged += (_, args) =>
                    {
                        raised = true;
                    };

                }
                else if (popup.Host is OverlayPopupHost overlayPopupHost)
                {
                    overlayPopupHost.PropertyChanged += (_, args) =>
                    {
                        if (args.Property == Canvas.TopProperty
                            || args.Property == Canvas.LeftProperty)
                        {
                            raised = true;
                        }
                    };
                }
                window.Position = new PixelPoint(10, 10);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender);
                Assert.False(raised);
            }
        }

        [Fact]
        public void Popup_Should_Follow_Placement_Target_On_Window_Resize()
        {
            using (CreateServices())
            {

                var placementTarget = new Panel()
                {
                    Width = 10,
                    Height = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var popup = new Popup()
                {
                    PlacementTarget = placementTarget,
                    Placement = PlacementMode.Bottom,
                    Width = 10,
                    Height = 10
                };
                ((ISetLogicalParent)popup).SetParent(popup.PlacementTarget);

                var window = PreparedWindow(placementTarget);
                window.Show();
                popup.Open();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender);

                // The target's initial placement is (395,295) which is a 10x10 panel centered in a 800x600 window
                Assert.Equal(placementTarget.Bounds, new Rect(395D, 295D, 10, 10));

                var raised = false;
                // Resizing the window to 700x500 must move the popup to (345,255) as this is the new
                // location of the placement target
                if (popup.Host is PopupRoot popupRoot)
                {
                    popupRoot.PositionChanged += (_, args) =>
                    {
                        Assert.Equal(new PixelPoint(345, 255), args.Point);
                        raised = true;
                    };

                }
                else if (popup.Host is OverlayPopupHost overlayPopupHost)
                {
                    overlayPopupHost.PropertyChanged += (_, args) =>
                    {
                        if ((args.Property == Canvas.TopProperty
                            || args.Property == Canvas.LeftProperty)
                            && Canvas.GetLeft(overlayPopupHost) == 345
                            && Canvas.GetTop(overlayPopupHost) == 255)
                        {
                            raised = true;
                        }
                    };
                }
                window.PlatformImpl?.Resize(new Size(700D, 500D), WindowResizeReason.Unspecified);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender);
                Assert.True(raised);
            }
        }

        [Fact]
        public void Popup_Should_Not_Follow_Placement_Target_On_Window_Resize_If_Pointer_If_Pointer()
        {
            using (CreateServices())
            {

                var placementTarget = new Panel()
                {
                    Width = 10,
                    Height = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var popup = new Popup()
                {
                    PlacementTarget = placementTarget,
                    Placement = PlacementMode.Pointer,
                    Width = 10,
                    Height = 10
                };
                ((ISetLogicalParent)popup).SetParent(popup.PlacementTarget);

                var window = PreparedWindow(placementTarget);
                window.Show();
                popup.Open();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender);

                // The target's initial placement is (395,295) which is a 10x10 panel centered in a 800x600 window
                Assert.Equal(placementTarget.Bounds, new Rect(395D, 295D, 10, 10));

                var raised = false;
                if (popup.Host is PopupRoot popupRoot)
                {
                    popupRoot.PositionChanged += (_, args) =>
                    {
                        raised = true;
                    };

                }
                else if (popup.Host is OverlayPopupHost overlayPopupHost)
                {
                    overlayPopupHost.PropertyChanged += (_, args) =>
                    {
                        if (args.Property == Canvas.TopProperty
                            || args.Property == Canvas.LeftProperty)
                        {
                            raised = true;
                        }
                    };
                }
                window.PlatformImpl?.Resize(new Size(700D, 500D), WindowResizeReason.Unspecified);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.AfterRender);
                Assert.False(raised);
            }
        }

        [Fact]
        public void Popup_Should_Follow_Placement_Target_On_Target_Moved()
        {
            using (CreateServices())
            {
                var placementTarget = new Panel()
                {
                    Width = 10,
                    Height = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var popup = new Popup()
                {
                    PlacementTarget = placementTarget,
                    Placement = PlacementMode.Bottom,
                    Width = 10,
                    Height = 10
                };
                ((ISetLogicalParent)popup).SetParent(popup.PlacementTarget);

                var window = PreparedWindow(placementTarget);
                window.Show();
                popup.Open();
                Dispatcher.UIThread.RunJobs();

                // The target's initial placement is (395,295) which is a 10x10 panel centered in a 800x600 window
                Assert.Equal(placementTarget.Bounds, new Rect(395D, 295D, 10, 10));

                var raised = false;
                // Margin will move placement target
                if (popup.Host is PopupRoot popupRoot)
                {
                    popupRoot.PositionChanged += (_, args) =>
                    {
                        Assert.Equal(new PixelPoint(400, 305), args.Point);
                        raised = true;
                    };

                }
                else if (popup.Host is OverlayPopupHost overlayPopupHost)
                {
                    overlayPopupHost.PropertyChanged += (_, args) =>
                    {
                        if ((args.Property == Canvas.TopProperty
                            || args.Property == Canvas.LeftProperty)
                            && Canvas.GetLeft(overlayPopupHost) == 400
                            && Canvas.GetTop(overlayPopupHost) == 305)
                        {
                            raised = true;
                        }
                    };
                }
                placementTarget.Margin = new Thickness(10, 0, 0, 0);
                Dispatcher.UIThread.RunJobs();
                Assert.True(raised);
            }
        }

        [Fact]
        public void Popup_Should_Not_Follow_Placement_Target_On_Target_Moved_If_Pointer()
        {
            using (CreateServices())
            {

                var placementTarget = new Panel()
                {
                    Width = 10,
                    Height = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var popup = new Popup()
                {
                    PlacementTarget = placementTarget,
                    Placement = PlacementMode.Pointer,
                    Width = 10,
                    Height = 10
                };
                ((ISetLogicalParent)popup).SetParent(popup.PlacementTarget);

                var window = PreparedWindow(placementTarget);
                window.Show();
                popup.Open();
                Dispatcher.UIThread.RunJobs();

                // The target's initial placement is (395,295) which is a 10x10 panel centered in a 800x600 window
                Assert.Equal(placementTarget.Bounds, new Rect(395D, 295D, 10, 10));

                var raised = false;
                if (popup.Host is PopupRoot popupRoot)
                {
                    popupRoot.PositionChanged += (_, args) =>
                    {
                        raised = true;
                    };

                }
                else if (popup.Host is OverlayPopupHost overlayPopupHost)
                {
                    overlayPopupHost.PropertyChanged += (_, args) =>
                    {
                        if (args.Property == Canvas.TopProperty
                            || args.Property == Canvas.LeftProperty)
                        {
                            raised = true;
                        }
                    };
                }
                placementTarget.Margin = new Thickness(10, 0, 0, 0);
                Dispatcher.UIThread.RunJobs();
                Assert.False(raised);
            }
        }

        [Fact]
        public void Popup_Should_Follow_Popup_Root_Placement_Target()
        {
            // When the placement target of a popup is another popup (e.g. nested menu items), the child popup must
            // follow the parent popup if it moves (due to root window movement or resize)
            using (CreateServices())
            {
                // The child popup is placed directly over the parent popup for position testing
                var parentPopup = new Popup() { Width = 10, Height = 10 };
                var childPopup = new Popup() {
                    Width = 20,
                    Height = 20,
                    PlacementTarget = parentPopup, 
                    Placement = PlacementMode.AnchorAndGravity,
                    PlacementAnchor = PopupAnchor.TopLeft,
                    PlacementGravity = PopupGravity.BottomRight
                };
                ((ISetLogicalParent)childPopup).SetParent(childPopup.PlacementTarget);
                
                var window = PreparedWindow(parentPopup);
                window.Show();
                parentPopup.Open();
                childPopup.Open();
                
                if (childPopup.Host is PopupRoot popupRoot)
                {
                    var raised = false;
                    popupRoot.PositionChanged += (_, args) =>
                    {
                        // The parent's initial placement is (395,295) which is a 10x10 popup centered
                        // in a 800x600 window. When the window is moved, the child's final placement is (405, 305)
                        // which is the parent's placement moved 10 pixels left and down.
                        Assert.Equal(new PixelPoint(405, 305), args.Point);
                        raised = true;
                    };

                    window.Position = new PixelPoint(10, 10);
                    Assert.True(raised);
                }
            }            
        }

        [Fact]
        public void Events_Should_Be_Routed_To_Popup_Parent()
        {
            using (CreateServices())
            {
                var popupContent = new Border();
                var popup = new Popup { Child = popupContent };
                var popupParent = new Border { Child = popup };
                var root = PreparedWindow(popupParent);
                var raised = 0;

                root.LayoutManager.ExecuteInitialLayoutPass();
                popup.Open();
                root.LayoutManager.ExecuteLayoutPass();

                var ev = new RoutedEventArgs(Button.ClickEvent);

                popupParent.AddHandler(Button.ClickEvent, (s, e) => ++raised);
                popupContent.RaiseEvent(ev);

                Assert.Equal(1, raised);
            }
        }

        [Fact]
        public void GetPosition_On_Control_In_Popup_Called_From_Parent_Should_Return_Valid_Coordinates()
        {
            // This test only applies when using a PopupRoot host and not an overlay popup.
            if (UsePopupHost)
                return;

            using (CreateServices())
            {
                var popupContent = new Border() { Height = 100, Width = 100, Background = Brushes.Red };
                var popup = new Popup {  Child = popupContent, HorizontalOffset = 40, VerticalOffset = 40, Placement = PlacementMode.AnchorAndGravity,
                    PlacementAnchor = PopupAnchor.TopLeft, PlacementGravity = PopupGravity.BottomRight};
                var popupParent = new Border { Child = popup };
                var root = PreparedWindow(popupParent);

                popup.Open();

                // Verify that the popup is positioned at 40,40 as descibed by the Horizontal/
                // VerticalOffset: 10,10 becomes 50,50 in screen coordinates.
                Assert.Equal(new PixelPoint(50, 50), popupContent.PointToScreen(new Point(10, 10)));

                // The popup parent is positioned at 0,0 in screen coordinates so client and
                // screen coordinates are the same.
                Assert.Equal(new PixelPoint(10, 10), popupParent.PointToScreen(new Point(10, 10)));

                // The event will be raised on the popup content at 50,50 (90,90 in screen coordinates)
                var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
                var ev = new PointerPressedEventArgs(
                    popupContent,
                    pointer,
                    popupContent.VisualRoot as PopupRoot,
                    new Point(50 , 50),
                    0,
                    new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
                    KeyModifiers.None);

                var contentRaised = 0;
                var parentRaised = 0;

                // The event is raised on the popup content in popup coordinates.
                popupContent.AddHandler(Button.PointerPressedEvent, (s, e) =>
                {
                    ++contentRaised;
                    Assert.Equal(new Point(50, 50), e.GetPosition(popupContent));
                });

                // The event is raised on the parent in root coordinates (which in this case are 
                // the same as screen coordinates).
                popupParent.AddHandler(Button.PointerPressedEvent, (s, e) =>
                {
                    ++parentRaised;
                    Assert.Equal(new Point(90, 90), e.GetPosition(popupParent));
                });

                popupContent.RaiseEvent(ev);

                Assert.Equal(1, contentRaised);
                Assert.Equal(1, parentRaised);
            }
        }

        private static PopupRoot CreateRoot(TopLevel popupParent, IPopupImpl impl = null)
        {
            impl ??= popupParent.PlatformImpl.CreatePopup();

            var result = new PopupRoot(popupParent, impl)
            {
                Template = new FuncControlTemplate<PopupRoot>((parent, scope) =>
                    new ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [!ContentPresenter.ContentProperty] = parent[!PopupRoot.ContentProperty],
                    }.RegisterInNameScope(scope)),
            };

            result.ApplyTemplate();

            return result;
        }

        private IDisposable CreateServices()
        {
            return UnitTestApplication.Start(TestServices.StyledWindow.With(
                windowingPlatform: CreateMockWindowingPlatform()));
        }

        private IDisposable CreateServicesWithFocus()
        {
            return UnitTestApplication.Start(TestServices.StyledWindow.With(
                windowingPlatform: CreateMockWindowingPlatform(),
                focusManager: new FocusManager(),
                keyboardDevice: () => new KeyboardDevice()));
        }

       
        private static PointerPressedEventArgs CreatePointerPressedEventArgs(Window source, Point p)
        {
            var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            return new PointerPressedEventArgs(
                source,
                pointer,
                source,
                p,
                0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None);
        }

        private MockWindowingPlatform CreateMockWindowingPlatform()
        {
            return new MockWindowingPlatform(() =>
            {
                var mock = MockWindowingPlatform.CreateWindowMock();
                
                mock.Setup(x => x.CreatePopup()).Returns(() =>
                {
                    if (UsePopupHost)
                        return null;
                    return MockWindowingPlatform.CreatePopupMock(mock.Object).Object;
                });

                return mock.Object;
            }, null);
        }

        private static Window PreparedWindow(object content = null)
        {
            var w = new Window { Content = content };
            w.Show();
            w.ApplyStyling();
            w.ApplyTemplate();
            return w;
        }

        private static Control PopupContentControlTemplate(PopupContentControl control, INameScope scope)
        {
            return new Popup
            {
                Name = "popup",
                PlacementTarget = control,
                Child = new ContentPresenter
                {
                    [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
                }
            }.RegisterInNameScope(scope);
        }

        private static Control PopupItemsControlTemplate(PopupItemsControl control, INameScope scope)
        {
            return new Popup
            {
                Name = "popup",
                PlacementTarget = control,
                Child = new ItemsPresenter(),
            }.RegisterInNameScope(scope);
        }

        private class PopupContentControl : ContentControl
        {
        }

        private class PopupItemsControl : ItemsControl
        {
        }

        private class TestControl : Decorator
        {
            public event EventHandler DataContextBeginUpdate;

            public new AvaloniaObject InheritanceParent => base.InheritanceParent;

            protected override void OnDataContextBeginUpdate()
            {
                DataContextBeginUpdate?.Invoke(this, EventArgs.Empty);
                base.OnDataContextBeginUpdate();
            }
        }
    }

    public class PopupTestsWithPopupRoot : PopupTests
    {
        public PopupTestsWithPopupRoot()
        {
            UsePopupHost = true;
        }
    }
}
