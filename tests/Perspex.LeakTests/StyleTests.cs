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
    }
}
