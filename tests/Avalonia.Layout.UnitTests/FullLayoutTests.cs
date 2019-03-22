// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using Moq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Shared.PlatformSupport;
using Avalonia.Styling;
using Avalonia.Themes.Default;
using Avalonia.VisualTree;
using Xunit;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using Avalonia.Controls.UnitTests;
using Avalonia.UnitTests;

namespace Avalonia.Layout.UnitTests
{
    public class FullLayoutTests
    {
        [Fact]
        public void Grandchild_Size_Changed()
        {
            using (var context = AvaloniaLocator.EnterScope())
            {
                RegisterServices();

                Border border;
                TextBlock textBlock;

                var window = new Window()
                {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Content = border = new Border
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new Border
                        {
                            Child = textBlock = new TextBlock
                            {
                                Width = 400,
                                Height = 400,
                                Text = "Hello World!",
                            },
                        }
                    }
                };

                window.Show();
                window.LayoutManager.ExecuteInitialLayoutPass(window);

                Assert.Equal(new Size(400, 400), border.Bounds.Size);
                textBlock.Width = 200;
                window.LayoutManager.ExecuteLayoutPass();

                Assert.Equal(new Size(200, 400), border.Bounds.Size);
            }
        }

        [Fact]
        public void Test_ScrollViewer_With_TextBlock()
        {
            using (var context = AvaloniaLocator.EnterScope())
            {
                RegisterServices();

                ScrollViewer scrollViewer;
                TextBlock textBlock;

                var window = new Window()
                {
                    Width = 800,
                    Height = 600,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Content = scrollViewer = new ScrollViewer
                    {
                        Width = 200,
                        Height = 200,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Content = textBlock = new TextBlock
                        {
                            Width = 400,
                            Height = 400,
                            Text = "Hello World!",
                        },
                    }
                };

                window.Resources["ScrollBarThickness"] = 10.0;

                window.Show();
                window.LayoutManager.ExecuteInitialLayoutPass(window);

                Assert.Equal(new Size(800, 600), window.Bounds.Size);
                Assert.Equal(new Size(200, 200), scrollViewer.Bounds.Size);
                Assert.Equal(new Point(300, 200), Position(scrollViewer));
                Assert.Equal(new Size(400, 400), textBlock.Bounds.Size);

                var scrollBars = scrollViewer.GetTemplateChildren().OfType<ScrollBar>().ToList();
                var presenters = scrollViewer.GetTemplateChildren().OfType<ScrollContentPresenter>().ToList();

                Assert.Equal(2, scrollBars.Count);
                Assert.Single(presenters);

                var presenter = presenters[0];
                Assert.Equal(new Size(190, 190), presenter.Bounds.Size);

                var horzScroll = scrollBars.Single(x => x.Orientation == Orientation.Horizontal);
                var vertScroll = scrollBars.Single(x => x.Orientation == Orientation.Vertical);

                Assert.True(horzScroll.IsVisible);
                Assert.True(vertScroll.IsVisible);
                Assert.Equal(new Size(190, 10), horzScroll.Bounds.Size);
                Assert.Equal(new Size(10, 190), vertScroll.Bounds.Size);
                Assert.Equal(new Point(0, 190), Position(horzScroll));
                Assert.Equal(new Point(190, 0), Position(vertScroll));
            }
        }

        private static Point Position(IVisual v)
        {
            return v.Bounds.Position;
        }

        class FormattedTextMock : IFormattedTextImpl
        {
            public FormattedTextMock(string text)
            {
                Text = text;
            }

            public Size Constraint { get; set; }

            public string Text { get; }

            public Rect Bounds => Rect.Empty;

            public void Dispose()
            {
            }

            public IEnumerable<FormattedTextLine> GetLines() => new FormattedTextLine[0];

            public TextHitTestResult HitTestPoint(Point point) => new TextHitTestResult();

            public Rect HitTestTextPosition(int index) => new Rect();

            public IEnumerable<Rect> HitTestTextRange(int index, int length) => new Rect[0];

            public Size Measure() => Constraint;
        }

        private void RegisterServices()
        {
            var globalStyles = new Mock<IGlobalStyles>();
            var globalStylesResources = globalStyles.As<IResourceNode>();
            var outObj = (object)10;
            globalStylesResources.Setup(x => x.TryGetResource("FontSizeNormal", out outObj)).Returns(true);

            var renderInterface = new Mock<IPlatformRenderInterface>();
            renderInterface.Setup(x =>
                x.CreateFormattedText(
                    It.IsAny<string>(),
                    It.IsAny<Typeface>(),
                    It.IsAny<TextAlignment>(),
                    It.IsAny<TextWrapping>(),
                    It.IsAny<Size>(),
                    It.IsAny<IReadOnlyList<FormattedTextStyleSpan>>()))
                .Returns(new FormattedTextMock("TEST"));

            var streamGeometry = new Mock<IStreamGeometryImpl>();
            streamGeometry.Setup(x =>
                    x.Open())
                .Returns(new Mock<IStreamGeometryContextImpl>().Object);

            renderInterface.Setup(x =>
                    x.CreateStreamGeometry())
                .Returns(streamGeometry.Object);

            var windowImpl = new Mock<IWindowImpl>();

            Size clientSize = default(Size);

            windowImpl.SetupGet(x => x.ClientSize).Returns(() => clientSize);
            windowImpl.Setup(x => x.Resize(It.IsAny<Size>())).Callback<Size>(s => clientSize = s);
            windowImpl.Setup(x => x.MaxClientSize).Returns(new Size(1024, 1024));
            windowImpl.SetupGet(x => x.Scaling).Returns(1);

            AvaloniaLocator.CurrentMutable
                .Bind<IStandardCursorFactory>().ToConstant(new CursorFactoryMock())
                .Bind<IAssetLoader>().ToConstant(new AssetLoader())
                .Bind<IInputManager>().ToConstant(new Mock<IInputManager>().Object)
                .Bind<IGlobalStyles>().ToConstant(globalStyles.Object)
                .Bind<IRuntimePlatform>().ToConstant(new AppBuilder().RuntimePlatform)
                .Bind<IPlatformRenderInterface>().ToConstant(renderInterface.Object)
                .Bind<IStyler>().ToConstant(new Styler())
                .Bind<IWindowingPlatform>().ToConstant(new Avalonia.Controls.UnitTests.WindowingPlatformMock(() => windowImpl.Object));

            var theme = new DefaultTheme();
            globalStyles.Setup(x => x.IsStylesInitialized).Returns(true);
            globalStyles.Setup(x => x.Styles).Returns(theme);
        }
    }
}
