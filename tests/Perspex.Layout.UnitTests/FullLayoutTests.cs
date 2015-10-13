// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using Moq;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Diagnostics;
using Perspex.Input;
using Perspex.Platform;
using Perspex.Rendering;
using Perspex.Styling;
using Perspex.Themes.Default;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace Perspex.Layout.UnitTests
{
    public class FullLayoutTests
    {
        [Fact]
        public void Grandchild_Size_Changed()
        {
            using (var context = PerspexLocator.EnterScope())
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

                window.LayoutManager.ExecuteLayoutPass();

                Assert.Equal(new Size(400, 400), border.Bounds.Size);
                textBlock.Width = 200;
                window.LayoutManager.ExecuteLayoutPass();

                Assert.Equal(new Size(200, 400), border.Bounds.Size);
            }
        }

        [Fact]
        public void Test_ScrollViewer_With_TextBlock()
        {
            using (var context = PerspexLocator.EnterScope())
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
                        CanScrollHorizontally = true,
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

                window.LayoutManager.ExecuteLayoutPass();

                Assert.Equal(new Size(800, 600), window.Bounds.Size);
                Assert.Equal(new Size(200, 200), scrollViewer.Bounds.Size);
                Assert.Equal(new Point(300, 200), Position(scrollViewer));
                Assert.Equal(new Size(400, 400), textBlock.Bounds.Size);

                var scrollBars = scrollViewer.GetTemplateChildren().OfType<ScrollBar>().ToList();
                var presenters = scrollViewer.GetTemplateChildren().OfType<ScrollContentPresenter>().ToList();

                Assert.Equal(2, scrollBars.Count);
                Assert.Equal(1, presenters.Count);

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

        private void RegisterServices()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());


            var formattedText = fixture.Create<IFormattedTextImpl>();
            var globalStyles = new Mock<IGlobalStyles>();
            var renderInterface = fixture.Create<IPlatformRenderInterface>();
            var renderManager = fixture.Create<IRenderQueueManager>();
            var theme = new DefaultTheme();
            var windowImpl = new Mock<IWindowImpl>();

            windowImpl.SetupProperty(x => x.ClientSize);
            windowImpl.Setup(x => x.MaxClientSize).Returns(new Size(1024, 1024));
            globalStyles.Setup(x => x.Styles).Returns(theme);

            PerspexLocator.CurrentMutable
                .Bind<IInputManager>().ToConstant(new Mock<IInputManager>().Object)
                .Bind<IGlobalStyles>().ToConstant(globalStyles.Object)
                .Bind<ILayoutManager>().ToConstant(new LayoutManager())
                .Bind<IPlatformRenderInterface>().ToConstant(renderInterface)
                .Bind<IRenderQueueManager>().ToConstant(renderManager)
                .Bind<IStyler>().ToConstant(new Styler())
                .Bind<IWindowImpl>().ToConstant(windowImpl.Object);
        }
    }
}
