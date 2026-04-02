#nullable enable

using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxTests_Input : ScopedTestBase
    {
        [Fact]
        public void Touch_Tap_Moves_Caret()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "12 12345678"
                };

                var root = new TestRoot()
                {
                    Child = target
                };

                target.ApplyTemplate();

                root.LayoutManager.ExecuteInitialLayoutPass();

                var touch = new TouchTestHelper();

                Assert.Equal(target.CaretIndex, 0);

                // Move to index 8
                touch.Down(target, new Point(50, 0));
                touch.Up(target, new Point(50, 0));

                Assert.Equal(target.CaretIndex, 8);
            }
        }

        [Fact]
        public void Touch_Double_Tap_Selects_Word()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "12 12345678"
                };

                var root = new TestRoot()
                {
                    Child = target
                };

                target.ApplyTemplate();


                root.LayoutManager.ExecuteInitialLayoutPass();

                var touch = new TouchTestHelper();

                Assert.Equal(target.CaretIndex, 0);

                // Move to index 8
                touch.Down(target, new Point(50, 0));
                touch.Up(target, new Point(50, 0));

                // Double tap
                touch.Down(target, new Point(50, 0));
                touch.Up(target, new Point(50, 0));

                Assert.Equal(target.SelectionStart, 3);
                Assert.Equal(target.SelectionEnd, 11);
                Assert.Equal(target.CaretIndex, 8);
            }
        }

        [Fact]
        public void Touch_Hold_Selects_Word()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "12 12345678"
                };

                var root = new TestRoot()
                {
                    Child = target
                };

                target.ApplyTemplate();

                root.LayoutManager.ExecuteInitialLayoutPass();

                var touch = new TouchTestHelper();

                Assert.Equal(target.CaretIndex, 0);

                // Move to index 8
                touch.Down(target, new Point(50, 0));

                var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
                timer.ForceFire();
                touch.Up(target, new Point(50, 0));

                Assert.Equal(target.SelectionStart, 3);
                Assert.Equal(target.SelectionEnd, 11);
                Assert.Equal(target.CaretIndex, 8);
            }
        }

        [Fact]
        public void Touch_Hold_On_Selection_Requests_Context()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "12 12345678"
                };

                var root = new TestRoot()
                {
                    Child = target
                };

                target.ApplyTemplate();
                bool requested = false;

                target.SelectionStart = 3;
                target.SelectionEnd = 11;

                target.ContextRequested += Target_ContextRequested;

                root.LayoutManager.ExecuteInitialLayoutPass();

                var touch = new TouchTestHelper();

                Assert.Equal(target.CaretIndex, 0);

                // Move to index 8
                touch.Down(target, new Point(50, 0));

                var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
                timer.ForceFire();
                touch.Up(target, new Point(50, 0));

                Assert.True(requested);

                void Target_ContextRequested(object? sender, ContextRequestedEventArgs e)
                {
                    requested = true;
                }
            }
        }

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            textShaperImpl: new HarfBuzzTextShaper(),
            fontManagerImpl: new TestFontManager(),
            assetLoader: new StandardAssetLoader());

        internal static IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate<TextBox>((control, scope) =>
            new ScrollViewer
            {
                Name = "PART_ScrollViewer",
                Template = new FuncControlTemplate<ScrollViewer>(ScrollViewerTests.CreateTemplate),
                Content = new TextPresenter
                {
                    Name = "PART_TextPresenter",
                    [!!TextPresenter.TextProperty] = new Binding
                    {
                        Path = nameof(TextPresenter.Text),
                        Mode = BindingMode.TwoWay,
                        Priority = BindingPriority.Template,
                        RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                    },
                    [!!TextPresenter.CaretIndexProperty] = new Binding
                    {
                        Path = nameof(TextPresenter.CaretIndex),
                        Mode = BindingMode.TwoWay,
                        Priority = BindingPriority.Template,
                        RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                    }
                }.RegisterInNameScope(scope)
            }.RegisterInNameScope(scope));
        }

        private static void RaiseKeyEvent(TextBox textBox, Key key, KeyModifiers inputModifiers)
        {
            textBox.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                KeyModifiers = inputModifiers,
                Key = key
            });
        }

        private static void RaiseTextEvent(TextBox textBox, string text)
        {
            textBox.RaiseEvent(new TextInputEventArgs
            {
                RoutedEvent = InputElement.TextInputEvent,
                Text = text
            });
        }

        private class Class1 : NotifyingBase
        {
            private int _foo;
            private string? _bar;

            public int Foo
            {
                get { return _foo; }
                set { _foo = value; RaisePropertyChanged(); }
            }

            public string? Bar
            {
                get { return _bar; }
                set { _bar = value; RaisePropertyChanged(); }
            }
        }

        private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
        {
        }

        private static Mock<ITopLevelImpl> CreateMockTopLevelImpl()
        {
            var clipboard = new Mock<ITopLevelImpl>();
            clipboard.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
            clipboard.Setup(r => r.TryGetFeature(typeof(IClipboard)))
                .Returns(new Clipboard(new HeadlessClipboardImplStub()));
            clipboard.SetupGet(x => x.RenderScaling).Returns(1);
            return clipboard;
        }

        private static FuncControlTemplate<TestTopLevel> CreateTopLevelTemplate()
        {
            return new FuncControlTemplate<TestTopLevel>((x, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty],
                }.RegisterInNameScope(scope));
        }

        private class TestContextMenu : ContextMenu
        {
            public TestContextMenu()
            {
                IsOpen = true;
            }
        }
    }
}
