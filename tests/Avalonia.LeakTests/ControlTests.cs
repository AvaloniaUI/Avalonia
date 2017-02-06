// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.dotMemoryUnit;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Diagnostics;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
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
                PurgeMoqReferences();

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
                PurgeMoqReferences();

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
                PurgeMoqReferences();

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
                PurgeMoqReferences();

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

                    var binding = new Avalonia.Markup.Xaml.Data.Binding
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
                PurgeMoqReferences();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBox>()).ObjectsCount));
                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Node>()).ObjectsCount));
            }
        }

        [Fact]
        public void TextBox_Class_Listeners_Are_Freed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                TextBox textBox;

                var window = new Window
                {
                    Content = textBox = new TextBox()
                };

                // Do a layout and make sure that TextBox gets added to visual tree and its 
                // template applied.
                LayoutManager.Instance.ExecuteInitialLayoutPass(window);
                Assert.Same(textBox, window.Presenter.Child);

                // Get the border from the TextBox template.
                var border = textBox.GetTemplateChildren().FirstOrDefault(x => x.Name == "border");

                // The TextBox should have subscriptions to its Classes collection from the
                // default theme.
                Assert.NotEmpty(((INotifyCollectionChangedDebug)textBox.Classes).GetCollectionChangedSubscribers());

                // Clear the content and ensure the TextBox is removed.
                window.Content = null;
                LayoutManager.Instance.ExecuteLayoutPass();
                Assert.Null(window.Presenter.Child);

                // Check that the TextBox has no subscriptions to its Classes collection.
                Assert.Null(((INotifyCollectionChangedDebug)textBox.Classes).GetCollectionChangedSubscribers());
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
                PurgeMoqReferences();

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TreeView>()).ObjectsCount));
            }
        }


        [Fact]
        public void RendererIsDisposed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var renderer = new Mock<IRenderer>();
                renderer.Setup(x => x.Dispose());
                var impl = new Mock<IWindowImpl>();
                impl.SetupGet(x => x.Scaling).Returns(1);
                impl.SetupProperty(x => x.Closed);
                impl.Setup(x => x.Dispose()).Callback(() => impl.Object.Closed());

                AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatform>()
                    .ToConstant(new MockWindowingPlatform(() => impl.Object));
                AvaloniaLocator.CurrentMutable.Bind<IRendererFactory>()
                    .ToConstant(new MockRendererFactory(renderer.Object));
                var window = new Window()
                {
                    Content = new Button()
                };
                window.Show();
                window.Close();
                renderer.Verify(r => r.Dispose());
            }
        }

        private static void PurgeMoqReferences()
        {
            // Moq holds onto references in its mock of IRenderer in case we want to check if a method has been called;
            // clear these.
            var renderer = Mock.Get(AvaloniaLocator.Current.GetService<IRenderer>());
            renderer.ResetCalls();
        }

        private class Node
        {
            public string Name { get; set; }
            public IEnumerable<Node> Children { get; set; }
        }
    }
}
