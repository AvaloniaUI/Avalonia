// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.dotMemoryUnit;
using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Layout;
using Perspex.Styling;
using Perspex.UnitTests;
using Perspex.VisualTree;
using Xunit;
using Xunit.Abstractions;

namespace Perspex.LeakTests
{
    [DotMemoryUnit(FailIfRunWithoutSupport = false)]
    public class ControlTests
    {
        public ControlTests(ITestOutputHelper atr)
        {
            DotMemoryUnitTestOutput.SetOutputMethod(atr.WriteLine);
        }

        [Fact]
        public void Canvas_Is_Freed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new Canvas()
                    };

                    // Do a layout and make sure that Canvas gets added to visual tree.
                    LayoutManager.Instance.ExecuteInitialLayoutPass(window);
                    Assert.IsType<Canvas>(window.Presenter.Child);

                    // Clear the content and ensure the Canvas is removed.
                    window.Content = null;
                    LayoutManager.Instance.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
            }
        }

        [Fact]
        public void Named_Canvas_Is_Freed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
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
                    LayoutManager.Instance.ExecuteInitialLayoutPass(window);
                    Assert.IsType<Canvas>(window.Find<Canvas>("foo"));
                    Assert.IsType<Canvas>(window.Presenter.Child);

                    // Clear the content and ensure the Canvas is removed.
                    window.Content = null;
                    LayoutManager.Instance.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
            }
        }

        [Fact]
        public void ScrollViewer_With_Content_Is_Freed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new ScrollViewer
                        {
                            Content = new Canvas()
                        }
                    };

                    // Do a layout and make sure that ScrollViewer gets added to visual tree and its 
                    // template applied.
                    LayoutManager.Instance.ExecuteInitialLayoutPass(window);
                    Assert.IsType<ScrollViewer>(window.Presenter.Child);
                    Assert.IsType<Canvas>(((ScrollViewer)window.Presenter.Child).Presenter.Child);

                    // Clear the content and ensure the ScrollViewer is removed.
                    window.Content = null;
                    LayoutManager.Instance.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBox>()).ObjectsCount));
                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
            }
        }

        [Fact]
        public void TextBox_Is_Freed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new TextBox()
                    };

                    // Do a layout and make sure that TextBox gets added to visual tree and its 
                    // template applied.
                    LayoutManager.Instance.ExecuteInitialLayoutPass(window);
                    Assert.IsType<TextBox>(window.Presenter.Child);
                    Assert.NotEqual(0, window.Presenter.Child.GetVisualChildren().Count());

                    // Clear the content and ensure the TextBox is removed.
                    window.Content = null;
                    LayoutManager.Instance.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBox>()).ObjectsCount));
            }
        }

        [Fact]
        public void TextBox_With_Xaml_Binding_Is_Freed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        DataContext = new Node { Name = "foo" },
                        Content = new TextBox()
                    };

                    var binding = new Perspex.Markup.Xaml.Data.Binding
                    {
                        Path = "Name"
                    };

                    var textBox = (TextBox)window.Content;
                    textBox.Bind(TextBox.TextProperty, binding);

                    // Do a layout and make sure that TextBox gets added to visual tree and its 
                    // Text property set.
                    LayoutManager.Instance.ExecuteInitialLayoutPass(window);
                    Assert.IsType<TextBox>(window.Presenter.Child);
                    Assert.Equal("foo", ((TextBox)window.Presenter.Child).Text);

                    // Clear the content and DataContext and ensure the TextBox is removed.
                    window.Content = null;
                    window.DataContext = null;
                    LayoutManager.Instance.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBox>()).ObjectsCount));
                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Node>()).ObjectsCount));
            }
        }

        [Fact]
        public void TreeView_Is_Freed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
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
                                    x => x.Children)
                            },
                            Items = nodes
                        }
                    };

                    // Do a layout and make sure that TreeViewItems get realized.
                    LayoutManager.Instance.ExecuteInitialLayoutPass(window);
                    Assert.Equal(1, target.ItemContainerGenerator.Containers.Count());

                    // Clear the content and ensure the TreeView is removed.
                    window.Content = null;
                    LayoutManager.Instance.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TreeView>()).ObjectsCount));
            }
        }

        private class TestTemplatedControl : TemplatedControl
        {
            public static readonly StyledProperty<int> IsCanvasVisibleProperty =
                PerspexProperty.Register<TestTemplatedControl, int>("IsCanvasVisible");

            public TestTemplatedControl()
            {
                Template = new FuncControlTemplate<TestTemplatedControl>(parent =>
                    new Canvas
                    {
                        [~IsVisibleProperty] = parent[~IsCanvasVisibleProperty]
                    });
            }
        }

        private class Node
        {
            public string Name { get; set; }
            public IEnumerable<Node> Children { get; set; }
        }
    }
}
