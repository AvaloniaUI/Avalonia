using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class PopupRootTests
    {
        [Fact]
        public void PopupRoot_IsAttachedToLogicalTree_Is_True()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = CreateTarget(new Window());

                Assert.True(((ILogical)target).IsAttachedToLogicalTree);
            }
        }

        [Fact]
        public void Templated_Child_IsAttachedToLogicalTree_Is_True()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = CreateTarget(new Window());

                Assert.True(((ILogical)target.Presenter).IsAttachedToLogicalTree);
            }
        }

        [Fact]
        public void PopupRoot_StylingParent_Is_Popup()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();
                var target = new TemplatedControlWithPopup
                {
                    PopupContent = new Canvas(),
                };
                window.Content = target;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();
                target.ApplyTemplate();
                target.Popup.Open();

                Assert.Equal(target.Popup, ((IStyleHost)target.Popup.Host).StylingParent);
            }
        }

        [Fact]
        public void PopupRoot_Should_Have_Template_Applied()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();
                var target = new Popup {Placement = PlacementMode.Pointer};
                var child = new Control();

                window.Content = target;
                window.ApplyTemplate();
                target.Open();

               
                Assert.Single(((Visual)target.Host).GetVisualChildren());

                var templatedChild = ((Visual)target.Host).GetVisualChildren().Single();
                
                Assert.IsType<LayoutTransformControl>(templatedChild);

                var panel = templatedChild.GetVisualChildren().Single();

                Assert.IsType<Panel>(panel);

                var visualLayerManager = panel.GetVisualChildren().Skip(1).Single();

                Assert.IsType<VisualLayerManager>(visualLayerManager);

                var contentPresenter = visualLayerManager.VisualChildren.Single();
                Assert.IsType<ContentPresenter>(contentPresenter);
                
                
                Assert.Equal((PopupRoot)target.Host, ((Control)templatedChild).TemplatedParent);
                Assert.Equal((PopupRoot)target.Host, ((Control)contentPresenter).TemplatedParent);
            }
        }
        
        [Fact]
        public void PopupRoot_Should_Have_Null_VisualParent()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new Popup() {PlacementTarget = new Window()};

                target.Open();

                Assert.Null(((Visual)target.Host).GetVisualParent());
            }
        }
        
        [Fact]
        public void Attaching_PopupRoot_To_Parent_Logical_Tree_Raises_DetachedFromLogicalTree_And_AttachedToLogicalTree()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var child = new Decorator();
                var window = new Window();
                var target = CreateTarget(window);
                var detachedCount = 0;
                var attachedCount = 0;

                target.Content = child;

                target.DetachedFromLogicalTree += (s, e) => ++detachedCount;
                child.DetachedFromLogicalTree += (s, e) => ++detachedCount;
                target.AttachedToLogicalTree += (s, e) => ++attachedCount;
                child.AttachedToLogicalTree += (s, e) => ++attachedCount;

                ((ISetLogicalParent)target).SetParent(window);

                Assert.Equal(2, detachedCount);
                Assert.Equal(2, attachedCount);
            }
        }

        [Fact]
        public void Detaching_PopupRoot_From_Parent_Logical_Tree_Raises_DetachedFromLogicalTree_And_AttachedToLogicalTree()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var child = new Decorator();
                var window = new Window();
                var target = CreateTarget(window);
                var detachedCount = 0;
                var attachedCount = 0;

                target.Content = child;
                ((ISetLogicalParent)target).SetParent(window);

                target.DetachedFromLogicalTree += (s, e) => ++detachedCount;
                child.DetachedFromLogicalTree += (s, e) => ++detachedCount;
                target.AttachedToLogicalTree += (s, e) => ++attachedCount;
                child.AttachedToLogicalTree += (s, e) => ++attachedCount;

                ((ISetLogicalParent)target).SetParent(null);

                // Despite being detached from the parent logical tree, we're still attached to a
                // logical tree as PopupRoot itself is a logical tree root.
                Assert.True(((ILogical)target).IsAttachedToLogicalTree);
                Assert.True(((ILogical)child).IsAttachedToLogicalTree);
                Assert.Equal(2, detachedCount);
                Assert.Equal(2, attachedCount);
            }
        }

        [Fact]
        public void Clearing_Content_Of_Popup_In_ControlTemplate_Doesnt_Crash()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();
                var target = new TemplatedControlWithPopup
                {
                    PopupContent = new Canvas(),
                };
                window.Content = target;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();
                target.ApplyTemplate();
                target.Popup.Open();
                target.PopupContent = null;
            }
        }

        [Fact]
        public void Child_Should_Be_Measured_With_MaxAutoSizeHint()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var child = new ChildControl();
                var window = new Window();
                var popupImpl = MockWindowingPlatform.CreatePopupMock(window.PlatformImpl);
                popupImpl.Setup(x => x.MaxAutoSizeHint).Returns(new Size(1200, 1000));
                var target = CreateTarget(window, popupImpl.Object);
                
                target.Content = child;
                target.Show();

                Assert.Equal(1, child.MeasureSizes.Count);
                Assert.Equal(new Size(1200, 1000), child.MeasureSizes[0]);
            }
        }

        [Fact]
        public void Child_Should_Be_Measured_With_Width_Height_When_Set()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var child = new ChildControl();
                var window = new Window();
                var target = CreateTarget(window);

                target.Width = 500;
                target.Height = 600;
                target.Content = child;
                target.Show();

                Assert.Equal(1, child.MeasureSizes.Count);
                Assert.Equal(new Size(500, 600), child.MeasureSizes[0]);
            }
        }

        [Fact]
        public void Child_Should_Be_Measured_With_MaxWidth_MaxHeight_When_Set()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var child = new ChildControl();
                var window = new Window();
                var target = CreateTarget(window);

                target.MaxWidth = 500;
                target.MaxHeight = 600;
                target.Content = child;
                target.Show();

                Assert.Equal(1, child.MeasureSizes.Count);
                Assert.Equal(new Size(500, 600), child.MeasureSizes[0]);
            }
        }

        [Fact]
        public void Should_Not_Have_Offset_On_Bounds_When_Content_Larger_Than_Max_Window_Size()
        {
            // Issue #3784.
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();
                var popupImpl = MockWindowingPlatform.CreatePopupMock(window.PlatformImpl);

                var child = new Canvas
                {
                    Width = 400,
                    Height = 1344,
                };

                var target = CreateTarget(window, popupImpl.Object);
                target.Content = child;

                target.Show();

                Assert.Equal(new Size(400, 1024), target.Bounds.Size);

                // Issue #3784 causes this to be (0, 160) which makes no sense as Window has no
                // parent control to be offset against.
                Assert.Equal(new Point(0, 0), target.Bounds.Position);
            }
        }

        [Fact]
        public void MinWidth_MinHeight_Should_Be_Respected()
        {
            // Issue #3796
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();
                var popupImpl = MockWindowingPlatform.CreatePopupMock(window.PlatformImpl);

                var target = CreateTarget(window, popupImpl.Object);
                target.MinWidth = 400;
                target.MinHeight = 800;
                target.Content = new Border
                {
                    Width = 100,
                    Height = 100,
                };

                target.Show();

                Assert.Equal(new Rect(0, 0, 400, 800), target.Bounds);
                Assert.Equal(new Size(400, 800), target.ClientSize);
                Assert.Equal(new Size(400, 800), target.PlatformImpl.ClientSize);
            }
        }

        [Fact]
        public void Setting_Width_Should_Resize_WindowImpl()
        {
            // Issue #3796
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();
                var popupImpl = MockWindowingPlatform.CreatePopupMock(window.PlatformImpl);
                var positioner = new Mock<IPopupPositioner>();
                popupImpl.Setup(x => x.PopupPositioner).Returns(positioner.Object);

                var target = CreateTarget(window, popupImpl.Object);
                target.Width = 400;
                target.Height = 800;

                target.Show();

                Assert.Equal(400, target.Width);
                Assert.Equal(800, target.Height);

                target.Width = 410;
                target.LayoutManager.ExecuteLayoutPass();

                positioner.Verify(x => 
                    x.Update(It.Is<PopupPositionerParameters>(x => x.Size.Width == 410)));
                Assert.Equal(410, target.Width);
            }
        }

        private static PopupRoot CreateTarget(TopLevel popupParent, IPopupImpl impl = null)
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

        private class TemplatedControlWithPopup : Avalonia.Controls.Primitives.TemplatedControl
        {
            public static readonly StyledProperty<Control> PopupContentProperty =
                AvaloniaProperty.Register<TemplatedControlWithPopup, Control>(nameof(PopupContent));

            public TemplatedControlWithPopup()
            {
                Template = new FuncControlTemplate<TemplatedControlWithPopup>((parent, _) =>
                    new Popup
                    {
                        [!Popup.ChildProperty] = parent[!TemplatedControlWithPopup.PopupContentProperty],
                        PlacementTarget = parent
                    });
            }

            public Popup Popup { get; private set; }

            public Control PopupContent
            {
                get => GetValue(PopupContentProperty);
                set => SetValue(PopupContentProperty, value);
            }

            protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
            {
                Popup = (Popup)this.GetVisualChildren().Single();
            }
        }

        private class ChildControl : Control
        {
            public List<Size> MeasureSizes { get; } = new List<Size>();

            protected override Size MeasureOverride(Size availableSize)
            {
                MeasureSizes.Add(availableSize);
                return base.MeasureOverride(availableSize);
            }
        }
    }
}
