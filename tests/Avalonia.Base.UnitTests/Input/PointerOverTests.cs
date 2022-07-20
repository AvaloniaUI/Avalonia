#nullable enable
using System;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;

using Moq;

using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class PointerOverTests
    {
        // https://github.com/AvaloniaUI/Avalonia/issues/2821
        [Fact]
        public void Close_Should_Remove_PointerOver()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var device = CreatePointerDeviceMock().Object;
            var impl = CreateTopLevelImplMock(renderer.Object);

            Canvas canvas;
            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas())
                }
            });

            SetHit(renderer, canvas);
            impl.Object.Input!(CreateRawPointerMovedArgs(device, root));

            Assert.True(canvas.IsPointerOver);

            impl.Object.Closed!();

            Assert.False(canvas.IsPointerOver);
        }

        [Fact]
        public void MouseMove_Should_Update_IsPointerOver()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var device = CreatePointerDeviceMock().Object;
            var impl = CreateTopLevelImplMock(renderer.Object);

            Canvas canvas;
            Border border;
            Decorator decorator;

            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas()),
                    (border = new Border
                    {
                        Child = decorator = new Decorator(),
                    })
                }
            });

            SetHit(renderer, decorator);
            impl.Object.Input!(CreateRawPointerMovedArgs(device, root));

            Assert.True(decorator.IsPointerOver);
            Assert.True(border.IsPointerOver);
            Assert.False(canvas.IsPointerOver);
            Assert.True(root.IsPointerOver);

            SetHit(renderer, canvas);
            impl.Object.Input!(CreateRawPointerMovedArgs(device, root));

            Assert.False(decorator.IsPointerOver);
            Assert.False(border.IsPointerOver);
            Assert.True(canvas.IsPointerOver);
            Assert.True(root.IsPointerOver);
        }


        [Fact]
        public void TouchMove_Should_Not_Set_IsPointerOver()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var device = CreatePointerDeviceMock(pointerType: PointerType.Touch).Object;
            var impl = CreateTopLevelImplMock(renderer.Object);

            Canvas canvas;

            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas())
                }
            });

            SetHit(renderer, canvas);
            impl.Object.Input!(CreateRawPointerMovedArgs(device, root));

            Assert.False(canvas.IsPointerOver);
            Assert.False(root.IsPointerOver);
        }

        [Fact]
        public void HitTest_Should_Be_Ignored_If_Element_Captured()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var pointer = new Mock<IPointer>();
            var device = CreatePointerDeviceMock(pointer.Object).Object;
            var impl = CreateTopLevelImplMock(renderer.Object);

            Canvas canvas;
            Border border;
            Decorator decorator;

            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas()),
                    (border = new Border
                    {
                        Child = decorator = new Decorator(),
                    })
                }
            });

            SetHit(renderer, canvas);
            pointer.SetupGet(p => p.Captured).Returns(decorator);
            impl.Object.Input!(CreateRawPointerMovedArgs(device, root));

            Assert.True(decorator.IsPointerOver);
            Assert.True(border.IsPointerOver);
            Assert.False(canvas.IsPointerOver);
            Assert.True(root.IsPointerOver);
        }

        [Fact]
        public void IsPointerOver_Should_Be_Updated_When_Child_Sets_Handled_True()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var device = CreatePointerDeviceMock().Object;
            var impl = CreateTopLevelImplMock(renderer.Object);

            Canvas canvas;
            Border border;
            Decorator decorator;

            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas()),
                    (border = new Border
                    {
                        Child = decorator = new Decorator(),
                    })
                }
            });

            SetHit(renderer, canvas);
            impl.Object.Input!(CreateRawPointerMovedArgs(device, root));

            Assert.False(decorator.IsPointerOver);
            Assert.False(border.IsPointerOver);
            Assert.True(canvas.IsPointerOver);
            Assert.True(root.IsPointerOver);

            // Ensure that e.Handled is reset between controls.
            root.PointerMoved += (s, e) => e.Handled = true;
            decorator.PointerEntered += (s, e) => e.Handled = true;

            SetHit(renderer, decorator);
            impl.Object.Input!(CreateRawPointerMovedArgs(device, root));

            Assert.True(decorator.IsPointerOver);
            Assert.True(border.IsPointerOver);
            Assert.False(canvas.IsPointerOver);
            Assert.True(root.IsPointerOver);
        }

        [Fact]
        public void Pointer_Enter_Move_Leave_Should_Be_Followed()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var deviceMock = CreatePointerDeviceMock();
            var impl = CreateTopLevelImplMock(renderer.Object);
            var result = new List<(object?, string)>();

            void HandleEvent(object? sender, PointerEventArgs e)
            {
                result.Add((sender, e.RoutedEvent!.Name));
            }

            Canvas canvas;
            Border border;
            Decorator decorator;

            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas()),
                    (border = new Border
                    {
                        Child = decorator = new Decorator(),
                    })
                }
            });

            AddEnteredExitedHandlers(HandleEvent, canvas, decorator);

            // Enter decorator
            SetHit(renderer, decorator);
            SetMove(deviceMock, root, decorator);
            impl.Object.Input!(CreateRawPointerMovedArgs(deviceMock.Object, root));

            // Leave decorator
            SetHit(renderer, canvas);
            SetMove(deviceMock, root, canvas);
            impl.Object.Input!(CreateRawPointerMovedArgs(deviceMock.Object, root));

            Assert.Equal(
                new[]
                {
                        ((object?)decorator, nameof(InputElement.PointerEntered)),
                        (decorator, nameof(InputElement.PointerMoved)),
                        (decorator, nameof(InputElement.PointerExited)),
                        (canvas, nameof(InputElement.PointerEntered)),
                        (canvas, nameof(InputElement.PointerMoved))
                },
                result);
        }

        [Fact]
        public void PointerEntered_Exited_Should_Be_Raised_In_Correct_Order()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var deviceMock = CreatePointerDeviceMock();
            var impl = CreateTopLevelImplMock(renderer.Object);
            var result = new List<(object?, string)>();

            void HandleEvent(object? sender, PointerEventArgs e)
            {
                result.Add((sender, e.RoutedEvent!.Name));
            }

            Canvas canvas;
            Border border;
            Decorator decorator;

            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas()),
                    (border = new Border
                    {
                        Child = decorator = new Decorator(),
                    })
                }
            });

            SetHit(renderer, canvas);
            impl.Object.Input!(CreateRawPointerMovedArgs(deviceMock.Object, root));

            AddEnteredExitedHandlers(HandleEvent, root, canvas, border, decorator);

            SetHit(renderer, decorator);
            impl.Object.Input!(CreateRawPointerMovedArgs(deviceMock.Object, root));

            Assert.Equal(
                new[]
                {
                    ((object?)canvas, nameof(InputElement.PointerExited)),
                    (decorator, nameof(InputElement.PointerEntered)),
                    (border, nameof(InputElement.PointerEntered)),
                },
                result);
        }

        // https://github.com/AvaloniaUI/Avalonia/issues/7896
        [Fact]
        public void PointerEntered_Exited_Should_Set_Correct_Position()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var expectedPosition = new Point(15, 15);
            var renderer = new Mock<IRenderer>();
            var deviceMock = CreatePointerDeviceMock();
            var impl = CreateTopLevelImplMock(renderer.Object);
            var result = new List<(object?, string, Point)>();

            void HandleEvent(object? sender, PointerEventArgs e)
            {
                result.Add((sender, e.RoutedEvent!.Name, e.GetPosition(null)));
            }

            Canvas canvas;

            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas())
                }
            });

            AddEnteredExitedHandlers(HandleEvent, root, canvas);

            SetHit(renderer, canvas);
            impl.Object.Input!(CreateRawPointerMovedArgs(deviceMock.Object, root, expectedPosition));

            SetHit(renderer, null);
            impl.Object.Input!(CreateRawPointerMovedArgs(deviceMock.Object, root, expectedPosition));

            Assert.Equal(
                new[]
                {
                    ((object?)canvas, nameof(InputElement.PointerEntered), expectedPosition),
                    (root, nameof(InputElement.PointerEntered), expectedPosition),
                    (canvas, nameof(InputElement.PointerExited), expectedPosition),
                    (root, nameof(InputElement.PointerExited), expectedPosition)
                },
                result);
        }

        [Fact]
        public void Render_Invalidation_Should_Affect_PointerOver()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var deviceMock = CreatePointerDeviceMock();
            var impl = CreateTopLevelImplMock(renderer.Object);

            var invalidateRect = new Rect(0, 0, 15, 15);

            Canvas canvas;

            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas())
                }
            });

            // Let input know about latest device.
            SetHit(renderer, canvas);
            impl.Object.Input!(CreateRawPointerMovedArgs(deviceMock.Object, root));
            Assert.True(canvas.IsPointerOver);

            SetHit(renderer, canvas);
            renderer.Raise(r => r.SceneInvalidated += null, new SceneInvalidatedEventArgs((IRenderRoot)root, invalidateRect));
            Assert.True(canvas.IsPointerOver);

            // Raise SceneInvalidated again, but now hide element from the hittest.
            SetHit(renderer, null);
            renderer.Raise(r => r.SceneInvalidated += null, new SceneInvalidatedEventArgs((IRenderRoot)root, invalidateRect));
            Assert.False(canvas.IsPointerOver);
        }

        // https://github.com/AvaloniaUI/Avalonia/issues/7748
        [Fact]
        public void LeaveWindow_Should_Reset_PointerOver()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var deviceMock = CreatePointerDeviceMock();
            var impl = CreateTopLevelImplMock(renderer.Object);

            var lastClientPosition = new Point(1, 5);
            var invalidateRect = new Rect(0, 0, 15, 15);
            var result = new List<(object?, string, Point)>();

            void HandleEvent(object? sender, PointerEventArgs e)
            {
                result.Add((sender, e.RoutedEvent!.Name, e.GetPosition(null)));
            }

            Canvas canvas;

            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas())
                }
            });

            AddEnteredExitedHandlers(HandleEvent, root, canvas);

            // Init pointer over.
            SetHit(renderer, canvas);
            impl.Object.Input!(CreateRawPointerMovedArgs(deviceMock.Object, root, lastClientPosition));
            Assert.True(canvas.IsPointerOver);

            // Send LeaveWindow.
            impl.Object.Input!(new RawPointerEventArgs(deviceMock.Object, 0, root, RawPointerEventType.LeaveWindow, new Point(), default));
            Assert.False(canvas.IsPointerOver);

            Assert.Equal(
                new[]
                {
                    ((object?)canvas, nameof(InputElement.PointerEntered), lastClientPosition),
                    (root, nameof(InputElement.PointerEntered), lastClientPosition),
                    (canvas, nameof(InputElement.PointerExited), lastClientPosition),
                    (root, nameof(InputElement.PointerExited), lastClientPosition),
                },
                result);
        }

        private static void AddEnteredExitedHandlers(
            EventHandler<PointerEventArgs> handler,
            params IInputElement[] controls)
        {
            foreach (var c in controls)
            {
                c.PointerEntered += handler;
                c.PointerExited += handler;
                c.PointerMoved += handler;
            }
        }

        private static void SetHit(Mock<IRenderer> renderer, IControl? hit)
        {
            renderer.Setup(x => x.HitTest(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                .Returns(hit is null ? Array.Empty<IControl>() : new[] { hit });

            renderer.Setup(x => x.HitTestFirst(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                .Returns(hit);
        }

        private static void SetMove(Mock<IPointerDevice> deviceMock, IInputRoot root, IInputElement element)
        {
            deviceMock.Setup(d => d.ProcessRawEvent(It.IsAny<RawPointerEventArgs>()))
                .Callback(() => element.RaiseEvent(CreatePointerMovedArgs(root, element)));
        }

        private static Mock<IWindowImpl> CreateTopLevelImplMock(IRenderer renderer)
        {
            var impl = new Mock<IWindowImpl>();
            impl.DefaultValue = DefaultValue.Mock;
            impl.SetupAllProperties();
            impl.SetupGet(r => r.RenderScaling).Returns(1);
            impl.Setup(r => r.CreateRenderer(It.IsAny<IRenderRoot>())).Returns(renderer);
            impl.Setup(r => r.PointToScreen(It.IsAny<Point>())).Returns<Point>(p => new PixelPoint((int)p.X, (int)p.Y));
            impl.Setup(r => r.PointToClient(It.IsAny<PixelPoint>())).Returns<PixelPoint>(p => new Point(p.X, p.Y));
            return impl;
        }

        private static IInputRoot CreateInputRoot(IWindowImpl impl, IControl child)
        {
            var root = new Window(impl)
            {
                Width = 100,
                Height = 100,
                Content = child,
                Template = new FuncControlTemplate<Window>((w, _) => new ContentPresenter
                {
                    Content = w.Content
                })
            };
            root.Show();
            return root;
        }

        private static IInputRoot CreateInputRoot(IRenderer renderer, IControl child)
        {
            return CreateInputRoot(CreateTopLevelImplMock(renderer).Object, child);
        }

        private static RawPointerEventArgs CreateRawPointerMovedArgs(
            IPointerDevice pointerDevice,
            IInputRoot root,
            Point? positition = null)
        {
            return new RawPointerEventArgs(pointerDevice, 0, root, RawPointerEventType.Move,
                positition ?? default, default);
        }

        private static PointerEventArgs CreatePointerMovedArgs(
            IInputRoot root, IInputElement? source, Point? positition = null)
        {
            return new PointerEventArgs(InputElement.PointerMovedEvent, source, new Mock<IPointer>().Object, root,
                positition ?? default, default, PointerPointProperties.None, KeyModifiers.None);
        }

        private static Mock<IPointerDevice> CreatePointerDeviceMock(
            IPointer? pointer = null,
            PointerType pointerType = PointerType.Mouse)
        {
            if (pointer is null)
            {
                var pointerMock = new Mock<IPointer>();
                pointerMock.SetupGet(p => p.Type).Returns(pointerType);
                pointer = pointerMock.Object;
            }

            var pointerDevice = new Mock<IPointerDevice>();
            pointerDevice.Setup(d => d.TryGetPointer(It.IsAny<RawPointerEventArgs>()))
                .Returns(pointer);

            return pointerDevice;
        }
    }
}
