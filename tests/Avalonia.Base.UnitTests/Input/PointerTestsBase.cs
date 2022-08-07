#nullable enable
using System;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using Moq;

namespace Avalonia.Base.UnitTests.Input;

public abstract class PointerTestsBase
{
    protected static void SetHit(Mock<IRenderer> renderer, IControl? hit)
    {
        renderer.Setup(x => x.HitTest(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
            .Returns(hit is null ? Array.Empty<IControl>() : new[] { hit });

        renderer.Setup(x => x.HitTestFirst(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
            .Returns(hit);
    }

    protected static void SetMove(Mock<IPointerDevice> deviceMock, IInputRoot root, IInputElement element)
    {
        deviceMock.Setup(d => d.ProcessRawEvent(It.IsAny<RawPointerEventArgs>()))
            .Callback(() => element.RaiseEvent(CreatePointerMovedArgs(root, element)));
    }

    protected static Mock<IWindowImpl> CreateTopLevelImplMock(IRenderer renderer)
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

    protected static IInputRoot CreateInputRoot(IWindowImpl impl, IControl child)
    {
        var root = new Window(impl)
        {
            Width = 100,
            Height = 100,
            Content = child,
            Template = new FuncControlTemplate<Window>((w, _) => new ContentPresenter { Content = w.Content })
        };
        root.Show();
        return root;
    }

    protected static RawPointerEventArgs CreateRawPointerMovedArgs(
        IPointerDevice pointerDevice,
        IInputRoot root,
        Point? positition = null)
    {
        return new RawPointerEventArgs(pointerDevice, 0, root, RawPointerEventType.Move,
            positition ?? default, default);
    }

    protected static PointerEventArgs CreatePointerMovedArgs(
        IInputRoot root, IInputElement? source, Point? positition = null)
    {
        return new PointerEventArgs(InputElement.PointerMovedEvent, source, new Mock<IPointer>().Object, root,
            positition ?? default, default, PointerPointProperties.None, KeyModifiers.None);
    }

    protected static Mock<IPointerDevice> CreatePointerDeviceMock(
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
