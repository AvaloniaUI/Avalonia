// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.dotMemoryUnit;
using Perspex.Controls;
using Perspex.Controls.Templates;
using Xunit;
using Xunit.Abstractions;

namespace Perspex.LeakTests
{
    [DotMemoryUnit(FailIfRunWithoutSupport = false)]
    public class ControlTests
    {
        public ControlTests(ITestOutputHelper atr)
        {
            TestApp.Initialize();
            DotMemoryUnitTestOutput.SetOutputMethod(atr.WriteLine);
        }

        [Fact]
        public void Canvas_Is_Freed()
        {
            Func<Window> run = () =>
            {
                var window = new Window
                {
                    Content = new Canvas()
                };

                // Do a layout and make sure that Canvas gets added to visual tree.
                window.LayoutManager.ExecuteLayoutPass();
                Assert.IsType<Canvas>(window.Presenter.Child);

                // Clear the content and ensure the Canvas is removed.
                window.Content = null;
                window.LayoutManager.ExecuteLayoutPass();
                Assert.Null(window.Presenter.Child);

                return window;
            };

            var result = run();

            dotMemory.Check(memory =>
                Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
        }

        [Fact]
        public void Named_Canvas_Is_Freed()
        {
            Func<Window> run = () =>
            {
                var window = new Window
                {
                    Content = new Canvas
                    {
                        Name = "foo"
                    }
                };

                // Do a layout and make sure that Canvas gets added to visual tree.
                window.LayoutManager.ExecuteLayoutPass();
                Assert.IsType<Canvas>(window.Find<Canvas>("foo"));
                Assert.IsType<Canvas>(window.Presenter.Child);

                // Clear the content and ensure the Canvas is removed.
                window.Content = null;
                window.LayoutManager.ExecuteLayoutPass();
                Assert.Null(window.Presenter.Child);

                return window;
            };

            var result = run();

            dotMemory.Check(memory =>
                Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
        }

        [Fact]
        public void TreeView_Is_Freed()
        {
            Func<Window> run = () =>
            {
                var nodes = new[]
                {
                    new Node
                    {
                        Children = new[] { new Node() },
                    }
                };

                TreeView target;

                var window = new Window
                {
                    Content = target = new TreeView
                    {
                        DataTemplates = new DataTemplates
                    {
                        new FuncTreeDataTemplate<Node>(
                            x => new TextBlock { Text = x.Name },
                            x => x.Children,
                            x => true)
                    },
                        Items = nodes
                    }
                };

                // Do a layout and make sure that TreeViewItems get realized.
                window.LayoutManager.ExecuteLayoutPass();
                Assert.Equal(1, target.ItemContainerGenerator.Containers.Count());

                // Clear the content and ensure the TreeView is removed.
                window.Content = null;
                window.LayoutManager.ExecuteLayoutPass();
                Assert.Null(window.Presenter.Child);

                return window;
            };

            var result = run();

            dotMemory.Check(memory =>
                Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TreeView>()).ObjectsCount));
        }

        private class Node
        {
            public string Name { get; set; }
            public IEnumerable<Node> Children { get; set; }
        }
    }
}
