// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Diagnostics;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using JetBrains.dotMemoryUnit;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.LeakTests
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
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new Canvas()
                    };

                    window.Show();

                    // Do a layout and make sure that Canvas gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass(window);
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
        }

        [Fact]
        public void Named_Canvas_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var scope = new NameScope();
                    var window = new Window
                    {
                        Content = new Canvas
                        {
                            Name = "foo"
                        }.RegisterInNameScope(scope)
                    };
                    NameScope.SetNameScope(window, scope);

                    window.Show();

                    // Do a layout and make sure that Canvas gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass(window);
                    Assert.IsType<Canvas>(window.Find<Canvas>("foo"));
                    Assert.IsType<Canvas>(window.Presenter.Child);

                    // Clear the content and ensure the Canvas is removed.
                    window.Content = null;
                    NameScope.SetNameScope(window, null);

                    window.LayoutManager.ExecuteLayoutPass();
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
            using (Start())
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

                    window.Show();

                    // Do a layout and make sure that ScrollViewer gets added to visual tree and its 
                    // template applied.
                    window.LayoutManager.ExecuteInitialLayoutPass(window);
                    Assert.IsType<ScrollViewer>(window.Presenter.Child);
                    Assert.IsType<Canvas>(((ScrollViewer)window.Presenter.Child).Presenter.Child);

                    // Clear the content and ensure the ScrollViewer is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
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
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new TextBox()
                    };

                    window.Show();

                    // Do a layout and make sure that TextBox gets added to visual tree and its 
                    // template applied.
                    window.LayoutManager.ExecuteInitialLayoutPass(window);
                    Assert.IsType<TextBox>(window.Presenter.Child);
                    Assert.NotEmpty(window.Presenter.Child.GetVisualChildren());

                    // Clear the content and ensure the TextBox is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
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
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        DataContext = new Node { Name = "foo" },
                        Content = new TextBox()
                    };

                    var binding = new Avalonia.Data.Binding
                    {
                        Path = "Name"
                    };

                    var textBox = (TextBox)window.Content;
                    textBox.Bind(TextBox.TextProperty, binding);

                    window.Show();

                    // Do a layout and make sure that TextBox gets added to visual tree and its 
                    // Text property set.
                    window.LayoutManager.ExecuteInitialLayoutPass(window);
                    Assert.IsType<TextBox>(window.Presenter.Child);
                    Assert.Equal("foo", ((TextBox)window.Presenter.Child).Text);

                    // Clear the content and DataContext and ensure the TextBox is removed.
                    window.Content = null;
                    window.DataContext = null;
                    window.LayoutManager.ExecuteLayoutPass();
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
        public void TextBox_Class_Listeners_Are_Freed()
        {
            using (Start())
            {
                TextBox textBox;

                var window = new Window
                {
                    Content = textBox = new TextBox()
                };

                window.Show();

                // Do a layout and make sure that TextBox gets added to visual tree and its 
                // template applied.
                window.LayoutManager.ExecuteInitialLayoutPass(window);
                Assert.Same(textBox, window.Presenter.Child);

                // Get the border from the TextBox template.
                var border = textBox.GetTemplateChildren().FirstOrDefault(x => x.Name == "border");

                // The TextBox should have subscriptions to its Classes collection from the
                // default theme.
                Assert.NotEmpty(((INotifyCollectionChangedDebug)textBox.Classes).GetCollectionChangedSubscribers());

                // Clear the content and ensure the TextBox is removed.
                window.Content = null;
                window.LayoutManager.ExecuteLayoutPass();
                Assert.Null(window.Presenter.Child);

                // Check that the TextBox has no subscriptions to its Classes collection.
                Assert.Null(((INotifyCollectionChangedDebug)textBox.Classes).GetCollectionChangedSubscribers());
            }
        }

        [Fact]
        public void TreeView_Is_Freed()
        {
            using (Start())
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
                            DataTemplates =
                            {
                                new FuncTreeDataTemplate<Node>(
                                    (x, _) => new TextBlock { Text = x.Name },
                                    x => x.Children)
                            },
                            Items = nodes
                        }
                    };

                    window.Show();

                    // Do a layout and make sure that TreeViewItems get realized.
                    window.LayoutManager.ExecuteInitialLayoutPass(window);
                    Assert.Single(target.ItemContainerGenerator.Containers);

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
        }


        [Fact]
        public void Slider_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new Slider()
                    };

                    window.Show();

                    // Do a layout and make sure that Slider gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass(window);
                    Assert.IsType<Slider>(window.Presenter.Child);

                    // Clear the content and ensure the Slider is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Slider>()).ObjectsCount));
            }
        }

        [Fact]
        public void RendererIsDisposed()
        {
            using (Start())
            {
                var renderer = new Mock<IRenderer>();
                renderer.Setup(x => x.Dispose());
                var impl = new Mock<IWindowImpl>();
                impl.SetupGet(x => x.Scaling).Returns(1);
                impl.SetupProperty(x => x.Closed);
                impl.Setup(x => x.CreateRenderer(It.IsAny<IRenderRoot>())).Returns(renderer.Object);
                impl.Setup(x => x.Dispose()).Callback(() => impl.Object.Closed());

                AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatform>()
                    .ToConstant(new MockWindowingPlatform(() => impl.Object));
                var window = new Window()
                {
                    Content = new Button()
                };
                window.Show();
                window.Close();
                renderer.Verify(r => r.Dispose());
            }
        }

        private IDisposable Start()
        {
            return UnitTestApplication.Start(TestServices.StyledWindow);
        }

        private class Node
        {
            public string Name { get; set; }
            public IEnumerable<Node> Children { get; set; }
        }

        private class NullRenderer : IRenderer
        {
            public bool DrawFps { get; set; }
            public bool DrawDirtyRects { get; set; }
            public event EventHandler<SceneInvalidatedEventArgs> SceneInvalidated;

            public void AddDirty(IVisual visual)
            {
            }

            public void Dispose()
            {
            }

            public IEnumerable<IVisual> HitTest(Point p, IVisual root, Func<IVisual, bool> filter) => null;

            public void Paint(Rect rect)
            {
            }

            public void RecalculateChildren(IVisual visual)
            {
            }

            public void Resized(Size size)
            {
            }

            public void Start()
            {
            }

            public void Stop()
            {
            }
        }
    }
}
