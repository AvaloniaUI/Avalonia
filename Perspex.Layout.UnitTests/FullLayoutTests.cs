// -----------------------------------------------------------------------
// <copyright file="LayoutManagerTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Perspex.Layout.UnitTests
{
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Input;
    using Perspex.Platform;
    using Perspex.Rendering;
    using Perspex.Styling;
    using Perspex.Themes.Default;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Splat;

    [TestClass]
    public class FullLayoutTests
    {
        [TestMethod]
        public void Grandchild_Size_Changed()
        {
            using (var context = Locator.CurrentMutable.WithResolver())
            {
                this.RegisterServices();

                Border border;
                TextBlock textBlock;

                var window = new Window()
                {
                    Content = (border = new Border
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Content = new Border
                        {
                            Content = (textBlock = new TextBlock
                            {
                                Width = 400,
                                Height = 400,
                                Text = "Hello World!",
                            }),
                        }
                    })
                };

                window.LayoutManager.ExecuteLayoutPass();

                Assert.AreEqual(new Size(400, 400), border.ActualSize);
                textBlock.Width = 200;
                window.LayoutManager.ExecuteLayoutPass();

                Assert.AreEqual(new Size(200, 400), border.ActualSize);
            }
        }

        [TestMethod]
        public void Test_ScrollViewer_With_TextBlock()
        {
            using (var context = Locator.CurrentMutable.WithResolver())
            {
                this.RegisterServices();

                ScrollViewer scrollViewer;
                TextBlock textBlock;

                var window = new Window()
                {
                    Content = (scrollViewer = new ScrollViewer
                    {
                        Width = 200,
                        Height = 200,
                        CanScrollHorizontally = true,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Content = (textBlock = new TextBlock
                        {
                            Width = 400,
                            Height = 400,
                            Text = "Hello World!",
                        }),
                    })
                };

                window.LayoutManager.ExecuteLayoutPass();

                Assert.AreEqual(new Size(800, 600), window.ActualSize);
                Assert.AreEqual(new Size(200, 200), scrollViewer.ActualSize);
                Assert.AreEqual(new Point(300, 200), Position(scrollViewer));
                Assert.AreEqual(new Size(400, 400), textBlock.ActualSize);

                var scrollBars = scrollViewer.GetTemplateControls().OfType<ScrollBar>().ToList();
                var presenters = scrollViewer.GetTemplateControls().OfType<ScrollContentPresenter>().ToList();

                Assert.AreEqual(2, scrollBars.Count);
                Assert.AreEqual(1, presenters.Count);

                var presenter = presenters[0];
                Assert.AreEqual(new Size(190, 190), presenter.ActualSize);

                var horzScroll = scrollBars.Single(x => x.Orientation == Orientation.Horizontal);
                var vertScroll = scrollBars.Single(x => x.Orientation == Orientation.Vertical);

                Assert.IsTrue(horzScroll.IsVisible);
                Assert.IsTrue(vertScroll.IsVisible);
                Assert.AreEqual(new Size(190, 10), horzScroll.ActualSize);
                Assert.AreEqual(new Size(10, 190), vertScroll.ActualSize);
                Assert.AreEqual(new Point(0, 190), Position(horzScroll));
                Assert.AreEqual(new Point(190, 0), Position(vertScroll));
            }
        }

        private static Point Position(IVisual v)
        {
            return v.Bounds.Position;
        }

        private void RegisterServices()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var l = Locator.CurrentMutable;

            var formattedText = fixture.Create<IFormattedTextImpl>();
            var globalStyles = new Mock<IGlobalStyles>();
            var renderInterface = fixture.Create<IPlatformRenderInterface>();
            var renderManager = fixture.Create<IRenderManager>();
            var theme = new DefaultTheme();
            var windowImpl = new Mock<IWindowImpl>();

            globalStyles.Setup(x => x.Styles).Returns(theme);
            windowImpl.Setup(x => x.ClientSize).Returns(new Size(800, 600));

            l.RegisterConstant(new Mock<IInputManager>().Object, typeof(IInputManager));
            l.RegisterConstant(globalStyles.Object, typeof(IGlobalStyles));
            l.RegisterConstant(new LayoutManager(), typeof(ILayoutManager));
            l.RegisterConstant(renderInterface, typeof(IPlatformRenderInterface));
            l.RegisterConstant(new Mock<IPlatformThreadingInterface>().Object, typeof(IPlatformThreadingInterface));
            l.RegisterConstant(renderManager, typeof(IRenderManager));
            l.RegisterConstant(new Styler(), typeof(IStyler));
            l.RegisterConstant(windowImpl.Object, typeof(IWindowImpl));
        }
    }
}
