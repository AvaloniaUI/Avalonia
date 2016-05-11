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
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TopLevelTests
    {
        [Fact]
        public void ClientSize_Should_Be_Set_On_Construction()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.Setup(x => x.ClientSize).Returns(new Size(123, 456));

                var target = new TestTopLevel(impl.Object);

                Assert.Equal(new Size(123, 456), target.ClientSize);
            }
        }

        [Fact]
        public void Width_Should_Not_Be_Set_On_Construction()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.Setup(x => x.ClientSize).Returns(new Size(123, 456));

                var target = new TestTopLevel(impl.Object);

                Assert.Equal(double.NaN, target.Width);
            }
        }

        [Fact]
        public void Height_Should_Not_Be_Set_On_Construction()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.Setup(x => x.ClientSize).Returns(new Size(123, 456));

                var target = new TestTopLevel(impl.Object);

                Assert.Equal(double.NaN, target.Height);
            }
        }

        [Fact]
        public void Layout_Pass_Should_Not_Be_Automatically_Scheduled()
        {
            var services = TestServices.StyledWindow.With(layoutManager: Mock.Of<ILayoutManager>());

            using (UnitTestApplication.Start(services))
            {
                var impl = new Mock<ITopLevelImpl>();
                var target = new TestTopLevel(impl.Object);

                // The layout pass should be scheduled by the derived class.
                var layoutManagerMock = Mock.Get(LayoutManager.Instance);
                layoutManagerMock.Verify(x => x.ExecuteLayoutPass(), Times.Never);
            }
        }

        [Fact]
        public void Bounds_Should_Be_Set_After_Layout_Pass()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupProperty(x => x.ClientSize);
                impl.SetupProperty(x => x.Resized);
                impl.SetupGet(x => x.Scaling).Returns(1);

                var target = new TestTopLevel(impl.Object)
                {
                    Template = CreateTemplate(),
                    Content = new TextBlock
                    {
                        Width = 321,
                        Height = 432,
                    }
                };

                LayoutManager.Instance.ExecuteInitialLayoutPass(target);

                Assert.Equal(new Rect(0, 0, 321, 432), target.Bounds);
            }
        }

        [Fact]
        public void Impl_ClientSize_Should_Be_Set_After_Layout_Pass()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = Mock.Of<ITopLevelImpl>(x => x.Scaling == 1);

                var target = new TestTopLevel(impl)
                {
                    Template = CreateTemplate(),
                    Content = new TextBlock
                    {
                        Width = 321,
                        Height = 432,
                    }
                };

                LayoutManager.Instance.ExecuteInitialLayoutPass(target);

                Mock.Get(impl).VerifySet(x => x.ClientSize = new Size(321, 432));
            }
        }

        [Fact]
        public void Width_And_Height_Should_Not_Be_Set_After_Layout_Pass()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.Setup(x => x.ClientSize).Returns(new Size(123, 456));

                var target = new TestTopLevel(impl.Object);
                LayoutManager.Instance.ExecuteLayoutPass();

                Assert.Equal(double.NaN, target.Width);
                Assert.Equal(double.NaN, target.Height);
            }
        }

        [Fact]
        public void Width_And_Height_Should_Be_Set_After_Window_Resize_Notification()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();
                impl.Setup(x => x.ClientSize).Returns(new Size(123, 456));

                // The user has resized the window, so we can no longer auto-size.
                var target = new TestTopLevel(impl.Object);
                impl.Object.Resized(new Size(100, 200));

                Assert.Equal(100, target.Width);
                Assert.Equal(200, target.Height);
            }
        }

        [Fact]
        public void Activate_Should_Call_Impl_Activate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                var target = new TestTopLevel(impl.Object);

                target.Activate();

                impl.Verify(x => x.Activate());
            }
        }

        [Fact]
        public void Impl_Activate_Should_Call_Raise_Activated_Event()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();

                bool raised = false;
                var target = new TestTopLevel(impl.Object);
                target.Activated += (s, e) => raised = true;

                impl.Object.Activated();

                Assert.True(raised);
            }
        }

        [Fact]
        public void Impl_Close_Should_Call_Raise_Closed_Event()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();

                bool raised = false;
                var target = new TestTopLevel(impl.Object);
                target.Closed += (s, e) => raised = true;

                impl.Object.Closed();

                Assert.True(raised);
            }
        }

        [Fact]
        public void Impl_Deactivate_Should_Call_Raise_Activated_Event()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();

                bool raised = false;
                var target = new TestTopLevel(impl.Object);
                target.Deactivated += (s, e) => raised = true;

                impl.Object.Deactivated();

                Assert.True(raised);
            }
        }

        [Fact]
        public void Impl_Input_Should_Pass_Input_To_InputManager()
        {
            var inputManagerMock = new Mock<IInputManager>();
            var services = TestServices.StyledWindow.With(inputManager: inputManagerMock.Object);

            using (UnitTestApplication.Start(services))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();

                var target = new TestTopLevel(impl.Object);

                var input = new RawKeyEventArgs(
                    new Mock<IKeyboardDevice>().Object,
                    0,
                    RawKeyEventType.KeyDown,
                    Key.A, InputModifiers.None);
                impl.Object.Input(input);

                inputManagerMock.Verify(x => x.ProcessInput(input));
            }
        }

        [Fact]
        public void Adding_Top_Level_As_Child_Should_Throw_Exception()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();
                var target = new TestTopLevel(impl.Object);
                var child = new TestTopLevel(impl.Object);

                target.Template = CreateTemplate();
                target.Content = child;

                Assert.Throws<InvalidOperationException>(() => target.ApplyTemplate());
            }
        }

        [Fact]
        public void Exiting_Application_Notifies_Top_Level()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();
                var target = new TestTopLevel(impl.Object);
                UnitTestApplication.Current.Exit();
                Assert.True(target.IsClosed);
            }
        }

        private FuncControlTemplate<TestTopLevel> CreateTemplate()
        {
            return new FuncControlTemplate<TestTopLevel>(x =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty],
                });
        }

        private class TestTopLevel : TopLevel
        {
            public bool IsClosed { get; private set; }

            public TestTopLevel(ITopLevelImpl impl)
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
