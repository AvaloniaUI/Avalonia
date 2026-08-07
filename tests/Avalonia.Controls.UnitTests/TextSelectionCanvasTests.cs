using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextSelectionCanvasTests : ScopedTestBase
    {
        [Fact]
        public void Text_Selection_Handle_Moves_With_Scrolling()
        {
            using (UnitTestApplication.Start(Services))
            {
                var touchHelper = new TouchTestHelper();
                Border rootBorder = new Border()
                {
                    Width = 200,
                    Height = 600
                };
                var visualLayerManager = new VisualLayerManager()
                {
                    Child = rootBorder,
                    EnableTextSelectorLayer = true
                };
                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = visualLayerManager
                };

                var focusedTextBox = CreateTextBox();

                var panel = new StackPanel()
                {
                    Orientation = Layout.Orientation.Vertical,
                    Spacing = 20,
                    Children =
                    {
                        focusedTextBox,
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                        CreateTextBox(),
                    }
                };

                var scrollViewer = new ScrollViewer
                {
                    Template = new FuncControlTemplate<ScrollViewer>(ScrollViewerTests.CreateTemplate),
                    Content = panel
                };

                rootBorder.Child = scrollViewer;

                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                touchHelper.Tap(focusedTextBox);

                topLevel.LayoutManager.ExecuteLayoutPass();

                var presenter = focusedTextBox.FindDescendantOfType<TextPresenter>()!;
                var canvas = presenter.TextSelectionHandleCanvas;

                Assert.NotNull(canvas);

                var handle = canvas.Children.FirstOrDefault() as TextSelectionHandle;

                Assert.NotNull(handle);

                Assert.Equal(new Point(25.5, 14.5), handle.GetTopLeft());

                scrollViewer.Offset = new Vector(0, 50);

                topLevel.LayoutManager.ExecuteLayoutPass();

                Assert.Equal(new Point(25.5, -35.5), handle.GetTopLeft());
            }

            TextBox CreateTextBox()
            {
                return new TextBox
                {
                    Template = TextBoxTests.CreateTemplate(),
                    Text = "Test",
                    Width = 150
                };
            }
        }

        private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
        {

        }

        static Mock<ITopLevelImpl> CreateMockTopLevelImpl()
        {
            var topLevel = new Mock<ITopLevelImpl>();
            topLevel.Setup(x => x.RenderScaling).Returns(1);
            topLevel.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
            return topLevel;
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

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            textShaperImpl: new HarfBuzzTextShaper(),
            fontManagerImpl: new TestFontManager(),
            keyboardDevice: () => new KeyboardDevice(),
            keyboardNavigation: () => new KeyboardNavigationHandler(),
            inputManager: new InputManager(),
            assetLoader: new StandardAssetLoader());
    }
}
