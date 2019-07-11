// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
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
                var target = CreateTarget();

                Assert.True(((ILogical)target).IsAttachedToLogicalTree);
            }
        }

        [Fact]
        public void Templated_Child_IsAttachedToLogicalTree_Is_True()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = CreateTarget();

                Assert.True(target.Presenter.IsAttachedToLogicalTree);
            }
        }

        [Fact]
        public void PopupRoot_StylingParent_Is_Popup()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new TemplatedControlWithPopup
                {
                    PopupContent = new Canvas(),
                };

                var root = new TestRoot { Child = target };

                target.ApplyTemplate();
                target.Popup.Open();

                Assert.Equal(target.Popup, ((IStyleHost)target.Popup.PopupRoot).StylingParent);
            }
        }

        [Fact]
        public void Attaching_PopupRoot_To_Parent_Logical_Tree_Raises_DetachedFromLogicalTree_And_AttachedToLogicalTree()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var child = new Decorator();
                var target = CreateTarget();
                var window = new Window();
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
                var target = CreateTarget();
                var window = new Window();
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
                var target = new TemplatedControlWithPopup
                {
                    PopupContent = new Canvas(),
                };

                var root = new TestRoot { Child = target };

                target.ApplyTemplate();
                target.Popup.Open();
                target.PopupContent = null;
            }
        }

        private PopupRoot CreateTarget()
        {
            var result = new PopupRoot
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

        private class TemplatedControlWithPopup : TemplatedControl
        {
            public static readonly AvaloniaProperty<Control> PopupContentProperty =
                AvaloniaProperty.Register<TemplatedControlWithPopup, Control>(nameof(PopupContent));

            public TemplatedControlWithPopup()
            {
                Template = new FuncControlTemplate<TemplatedControlWithPopup>((parent, _) =>
                    new Popup
                    {
                        [!Popup.ChildProperty] = parent[!TemplatedControlWithPopup.PopupContentProperty],
                    });
            }

            public Popup Popup { get; private set; }

            public Control PopupContent
            {
                get => GetValue(PopupContentProperty);
                set => SetValue(PopupContentProperty, value);
            }

            protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
            {
                Popup = (Popup)this.GetVisualChildren().Single();
            }
        }
    }
}
