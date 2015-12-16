// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.dotMemoryUnit;
using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Styling;
using Perspex.VisualTree;
using Xunit;
using Xunit.Abstractions;

namespace Perspex.LeakTests
{
    [DotMemoryUnit(FailIfRunWithoutSupport = false)]
    public class StyleTests
    {
        public StyleTests(ITestOutputHelper atr)
        {
            TestApp.Initialize();
            DotMemoryUnitTestOutput.SetOutputMethod(atr.WriteLine);
        }

        [Fact]
        public void StyleActivator_Should_Be_Released()
        {
            Func<Window> run = () =>
            {
                var window = new Window
                {
                    Styles = new Styles
                    {
                        new Style(x => x.OfType<Canvas>().Class("foo"))
                        {
                            Setters = new[]
                            {
                                new Setter(Canvas.WidthProperty, 100),
                            }
                        }
                    },
                    Content = new Canvas
                    {
                        Classes = new Classes("foo"),
                    }
                };

                // Do a layout and make sure that styled Canvas gets added to visual tree.
                window.LayoutManager.ExecuteLayoutPass();
                Assert.IsType<Canvas>(window.Presenter.Child);
                Assert.Equal(100, (window.Presenter.Child).Width);

                // Clear the content and ensure the Canvas is removed.
                window.Content = null;
                window.LayoutManager.ExecuteLayoutPass();
                Assert.Null(window.Presenter.Child);

                return window;
            };

            var result = run();

            dotMemory.Check(memory =>
                Assert.Equal(0, memory.GetObjects(where => where.Type.Is<StyleActivator>()).ObjectsCount));
        }

        [Fact]
        public void Changing_Carousel_SelectedIndex_Should_Not_Leak_StyleActivators()
        {
            Func<Window> run = () =>
            {
                Carousel target;

                var window = new Window
                {
                    Styles = new Styles
                    {
                        new Style(x => x.OfType<ContentControl>().Class("foo"))
                        {
                            Setters = new[]
                            {
                                new Setter(Visual.OpacityProperty, 0.5),
                            }
                        }
                    },
                    Content = target = new Carousel
                    {
                        Items = new[]
                        {
                            new ContentControl
                            {
                                Name = "item1",
                                Classes = new Classes("foo"),
                                Content = "item1",
                            },
                            new ContentControl
                            {
                                Name = "item2",
                                Classes = new Classes("foo"),
                                Content = "item2",
                            },
                        }
                    }
                };

                // Do a layout and make sure that Carousel gets added to visual tree.
                window.LayoutManager.ExecuteLayoutPass();
                Assert.IsType<Carousel>(window.Presenter.Child);

                target.SelectedIndex = 1;
                window.LayoutManager.ExecuteLayoutPass();
                target.SelectedIndex = 0;
                window.LayoutManager.ExecuteLayoutPass();
                target.SelectedIndex = 1;
                window.LayoutManager.ExecuteLayoutPass();

                return window;
            };

            var result = run();

            dotMemory.Check(memory =>
                Assert.Equal(1, memory.GetObjects(where => where.Type.Is<StyleActivator>()).ObjectsCount));
        }
    }
}
