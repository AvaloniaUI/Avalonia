// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TopLevelTests
    {
        [Fact]
        public void IsAttachedToLogicalTree_Is_True()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                var target = new TestTopLevel(impl.Object);

                Assert.True(((ILogical)target).IsAttachedToLogicalTree);
            }
        }

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
            var services = TestServices.StyledWindow;

            using (UnitTestApplication.Start(services))
            {
                var impl = new Mock<ITopLevelImpl>();
                
                var target = new TestTopLevel(impl.Object, Mock.Of<ILayoutManager>());

                // The layout pass should be scheduled by the derived class.
                var layoutManagerMock = Mock.Get(target.LayoutManager);
                layoutManagerMock.Verify(x => x.ExecuteLayoutPass(), Times.Never);
            }
        }

        [Fact]
        public void Bounds_Should_Be_Set_After_Layout_Pass()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupProperty(x => x.Resized);
                impl.SetupGet(x => x.Scaling).Returns(1);

                var target = new TestTopLevel(impl.Object)
                {
                    IsVisible = true,
                    Template = CreateTemplate(),
                    Content = new TextBlock
                    {
                        Width = 321,
                        Height = 432,
                    }
                };

                target.LayoutManager.ExecuteInitialLayoutPass(target);

                Assert.Equal(new Rect(0, 0, 321, 432), target.Bounds);
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
                target.LayoutManager.ExecuteLayoutPass();

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

                inputManagerMock.Verify(x => x.ProcessInput(input, null));
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
                UnitTestApplication.Current.Shutdown();
                Assert.True(target.IsClosed);
            }
        }

        [Fact]
        public void Adding_Resource_To_Application_Should_Raise_ResourcesChanged()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var impl = new Mock<ITopLevelImpl>();
                impl.SetupAllProperties();
                var target = new TestTopLevel(impl.Object);
                var raised = false;

                target.ResourcesChanged += (_, __) => raised = true;
                Application.Current.Resources.Add("foo", "bar");

                Assert.True(raised);
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
            private readonly ILayoutManager _layoutManager;
            public bool IsClosed { get; private set; }

            public TestTopLevel(ITopLevelImpl impl, ILayoutManager layoutManager = null)
                : base(impl)
            {
                _layoutManager = layoutManager ?? new LayoutManager();
            }

            protected override ILayoutManager CreateLayoutManager() => _layoutManager;

            protected override void HandleApplicationExiting()
            {
                base.HandleApplicationExiting();
                IsClosed = true;
            }
        }
    }
}
