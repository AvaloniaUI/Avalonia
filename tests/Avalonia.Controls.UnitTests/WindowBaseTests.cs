// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Subjects;
using Moq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class WindowBaseTests
    {
        [Fact]
        public void Impl_ClientSize_Should_Be_Set_After_Layout_Pass()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = Mock.Of<IWindowBaseImpl>(x => x.Scaling == 1);

                Mock.Get(impl).Setup(x => x.Resize(It.IsAny<Size>())).Callback(() => { });

                var target = new TestWindowBase(impl)
                {
                    Template = CreateTemplate(),
                    Content = new TextBlock
                    {
                        Width = 321,
                        Height = 432,
                    },
                    IsVisible = true,
                };

                LayoutManager.Instance.ExecuteInitialLayoutPass(target);

                Mock.Get(impl).Verify(x => x.Resize(new Size(321, 432)));
            }
        }


        [Fact]
        public void Activate_Should_Call_Impl_Activate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<IWindowBaseImpl>();
                var target = new TestWindowBase(impl.Object);

                target.Activate();

                impl.Verify(x => x.Activate());
            }
        }

        [Fact]
        public void Impl_Activate_Should_Call_Raise_Activated_Event()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<IWindowBaseImpl>();
                impl.SetupAllProperties();

                bool raised = false;
                var target = new TestWindowBase(impl.Object);
                target.Activated += (s, e) => raised = true;

                impl.Object.Activated();

                Assert.True(raised);
            }
        }


        [Fact]
        public void Impl_Deactivate_Should_Call_Raise_Deativated_Event()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<IWindowBaseImpl>();
                impl.SetupAllProperties();

                bool raised = false;
                var target = new TestWindowBase(impl.Object);
                target.Deactivated += (s, e) => raised = true;

                impl.Object.Deactivated();

                Assert.True(raised);
            }
        }

        private FuncControlTemplate<TestWindowBase> CreateTemplate()
        {
            return new FuncControlTemplate<TestWindowBase>(x =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty],
                });
        }

        private class TestWindowBase : WindowBase
        {
            public bool IsClosed { get; private set; }

            public TestWindowBase(IWindowBaseImpl impl)
                : base(impl)
            {
            }

            protected override void HandleApplicationExiting()
            {
                base.HandleApplicationExiting();
                IsClosed = true;
            }
        }
    }
}
