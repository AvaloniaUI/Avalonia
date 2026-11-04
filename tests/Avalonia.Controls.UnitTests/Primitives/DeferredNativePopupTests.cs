using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives;

public class DeferredNativePopupTests : ScopedTestBase
{
    [Fact]
    public void Preparation_Should_Expose_Handle_Before_Show_Focus_And_Opened()
    {
        using var app = CreateServices();
        var window = PreparedWindow();
        var target = new Popup { PlacementTarget = window, IsLightDismissEnabled = true };
        var impl = MockWindowingPlatform.CreatePopupMock(window.PlatformImpl!);
        var handle = new PlatformHandle(new IntPtr(42), "TestWindow");
        impl.Setup(x => x.Handle).Returns(handle);
        Mock.Get(window.PlatformImpl!).Setup(x => x.CreatePopup()).Returns(impl.Object);
        var events = new List<string>();
        Action? resume = null;
        window.NativePopupShowRequested = (root, show) =>
        {
            Assert.Same(handle, root.TryGetPlatformHandle());
            Assert.Same(root, target.Host);
            Assert.True(target.IsOpen);
            events.Add("prepare");
            resume = show;
        };
        impl.Setup(x => x.Show(true, false)).Callback(() => events.Add("show"));
        impl.Setup(x => x.TakeFocus()).Callback(() => events.Add("focus"));
        target.TakesFocusFromNativeControl = true;
        target.Opened += (_, _) => events.Add("opened");
        ((IPopupHostProvider)target).PopupHostChanged += host =>
        {
            if (host != null)
                events.Add("host");
        };

        target.Open();

        var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window)!;
        Assert.Equal(["prepare"], events);
        Assert.False(overlay.IsVisible);
        resume!();
        resume();
        Assert.Equal(["prepare", "show", "focus", "opened", "host"], events);
        Assert.True(overlay.IsVisible);
        var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        overlay.RaiseEvent(new PointerPressedEventArgs(window, pointer, window, new Point(10, 15), 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None));
        Assert.False(target.IsOpen);
        Assert.False(overlay.IsVisible);
        resume();
        impl.Verify(x => x.Show(true, false), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Closing_Before_Resume_Should_Cancel_And_Not_Affect_Reopened_Popup(bool useProperty)
    {
        using var app = CreateServices();
        var window = PreparedWindow();
        var target = new Popup { PlacementTarget = window, IsLightDismissEnabled = true };
        var continuations = new List<Action>();
        var roots = new List<PopupRoot>();
        window.NativePopupShowRequested = (root, show) =>
        {
            roots.Add(root);
            continuations.Add(show);
        };
        var opened = 0;
        var closed = 0;
        target.Opened += (_, _) => ++opened;
        target.Closed += (_, _) => ++closed;

        target.Open();
        var firstImpl = Mock.Get(roots[0].PlatformImpl!);
        roots[0].Closed += (_, _) => continuations[0]();
        if (useProperty)
            target.IsOpen = false;
        else
            target.Close();
        Assert.False(target.IsOpen);
        Assert.Null(target.Host);
        Assert.Empty(window.OpenedPopups);
        Assert.Equal(1, closed);
        firstImpl.Verify(x => x.Dispose(), Times.Once);

        target.Open();
        continuations[0]();
        Assert.Equal(0, opened);
        firstImpl.Verify(x => x.Show(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        continuations[1]();
        Assert.Equal(1, opened);
        Assert.Same(roots[1], target.Host);
        target.Close();
    }

    [Fact]
    public void Closing_Owner_Before_Resume_Should_Dispose_Pending_Popup()
    {
        using var app = CreateServices();
        var window = PreparedWindow();
        var target = new Popup { PlacementTarget = window };
        Action? resume = null;
        window.NativePopupShowRequested = (_, show) => resume = show;
        target.Open();
        var impl = Mock.Get(((PopupRoot)target.Host!).PlatformImpl!);

        window.Close();
        resume!();

        Assert.False(target.IsOpen);
        Assert.Null(target.Host);
        Assert.Empty(window.OpenedPopups);
        impl.Verify(x => x.Dispose(), Times.Once);
        impl.Verify(x => x.Show(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void Resume_Should_Be_Single_Use_Even_When_Show_Reenters()
    {
        using var app = CreateServices();
        var window = PreparedWindow();
        var target = new Popup { PlacementTarget = window };
        Action? resume = null;
        window.NativePopupShowRequested = (root, show) =>
        {
            resume = show;
            Mock.Get(root.PlatformImpl!).Setup(x => x.Show(true, false)).Callback(show);
            show();
        };
        var opened = 0;
        target.Opened += (_, _) => ++opened;

        target.Open();
        resume!();

        Assert.Equal(1, opened);
        Mock.Get(((PopupRoot)target.Host!).PlatformImpl!).Verify(x => x.Show(true, false), Times.Once);
        target.Close();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Preparation_Exception_Should_Clean_Up_Even_When_Closing_Is_Cancelled(bool afterResume)
    {
        using var app = CreateServices();
        var window = PreparedWindow();
        var target = new Popup { PlacementTarget = window, IsLightDismissEnabled = true };
        Mock<IPopupImpl>? impl = null;
        Action? resume = null;
        var error = new InvalidOperationException("Preparation failed");
        target.Closing += (_, e) => e.Cancel = true;
        window.NativePopupShowRequested = (root, show) =>
        {
            impl = Mock.Get(root.PlatformImpl!);
            resume = show;
            if (afterResume)
                show();
            throw error;
        };

        Assert.Same(error, Assert.Throws<InvalidOperationException>(target.Open));
        resume!();

        Assert.False(target.IsOpen);
        Assert.Null(target.Host);
        Assert.Empty(window.OpenedPopups);
        Assert.False(LightDismissOverlayLayer.GetLightDismissOverlayLayer(window)!.IsVisible);
        impl!.Verify(x => x.Dispose(), Times.Once);
        impl.Verify(x => x.Show(true, false), afterResume ? Times.Once : Times.Never);
    }

    [Fact]
    public void Deferred_Show_Exception_Should_Clean_Up_And_Propagate()
    {
        using var app = CreateServices();
        var window = PreparedWindow();
        var target = new Popup { PlacementTarget = window, IsLightDismissEnabled = true };
        Action? resume = null;
        var error = new InvalidOperationException("Show failed");
        window.NativePopupShowRequested = (root, show) =>
        {
            Mock.Get(root.PlatformImpl!).Setup(x => x.Show(true, false)).Throws(error);
            resume = show;
        };
        var opened = 0;
        target.Opened += (_, _) => ++opened;
        target.Open();
        var impl = Mock.Get(((PopupRoot)target.Host!).PlatformImpl!);

        Assert.Same(error, Assert.Throws<InvalidOperationException>(() => resume!()));
        resume!();

        Assert.Equal(0, opened);
        Assert.False(target.IsOpen);
        Assert.Null(target.Host);
        Assert.Empty(window.OpenedPopups);
        Assert.False(LightDismissOverlayLayer.GetLightDismissOverlayLayer(window)!.IsVisible);
        impl.Verify(x => x.Dispose(), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Closing_During_Show_Or_Focus_Should_Not_Raise_Popup_Opened(bool duringFocus)
    {
        using var app = CreateServices();
        var window = PreparedWindow();
        var target = new Popup { PlacementTarget = window, TakesFocusFromNativeControl = true };
        Mock<IPopupImpl>? impl = null;
        window.NativePopupShowRequested = (root, show) =>
        {
            impl = Mock.Get(root.PlatformImpl!);
            if (duringFocus)
                impl.Setup(x => x.TakeFocus()).Callback(target.Close);
            else
                root.Opened += (_, _) => target.Close();
            show();
        };
        var opened = 0;
        target.Opened += (_, _) => ++opened;

        target.Open();

        Assert.Equal(0, opened);
        Assert.False(target.IsOpen);
        Assert.Null(target.Host);
        impl!.Verify(x => x.TakeFocus(), duringFocus ? Times.Once : Times.Never);
    }

    [Fact]
    public void Closing_In_Opened_Should_Not_Notify_A_Stale_Host()
    {
        using var app = CreateServices();
        var window = PreparedWindow();
        var target = new Popup { PlacementTarget = window };
        window.NativePopupShowRequested = (_, show) => show();
        var hosts = new List<IPopupHost?>();
        ((IPopupHostProvider)target).PopupHostChanged += hosts.Add;
        target.Opened += (_, _) => target.Close();

        target.Open();

        Assert.Equal(new IPopupHost?[] { null }, hosts);
        Assert.False(target.IsOpen);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Nested_Popup_Should_Use_Owner_Gate_And_Cancel_When_Parent_Closes(bool lightDismiss)
    {
        using var app = CreateServices();
        var window = PreparedWindow();
        var nestedTarget = new Border { Width = 20, Height = 20 };
        var deeperTarget = new Border { Width = 10, Height = 10 };
        var parent = new Popup { PlacementTarget = window, Child = nestedTarget };
        var middle = new Popup { PlacementTarget = nestedTarget, Child = deeperTarget };
        var nested = new Popup { PlacementTarget = deeperTarget, IsLightDismissEnabled = lightDismiss };
        var roots = new List<PopupRoot>();
        Action? resumeNested = null;
        window.NativePopupShowRequested = (root, show) =>
        {
            roots.Add(root);
            if (roots.Count < 3)
            {
                Mock.Get(root.PlatformImpl!).Setup(x => x.CreatePopup())
                    .Returns(() => MockWindowingPlatform.CreatePopupMock(root.PlatformImpl!).Object);
                show();
            }
            else
                resumeNested = show;
        };

        parent.Open();
        middle.Open();
        nested.Open();
        Assert.Equal(3, roots.Count);
        Assert.Same(roots[0], roots[1].ParentTopLevel);
        Assert.Same(roots[1], roots[2].ParentTopLevel);
        Assert.Equal([middle], parent.OpenedPopups);
        Assert.Equal([nested], middle.OpenedPopups);
        var impl = Mock.Get(roots[2].PlatformImpl!);

        parent.Close();
        resumeNested!();

        Assert.False(nested.IsOpen);
        Assert.False(middle.IsOpen);
        Assert.Empty(middle.OpenedPopups);
        Assert.Empty(parent.OpenedPopups);
        Assert.Empty(window.OpenedPopups);
        impl.Verify(x => x.Show(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void Resume_From_Worker_Should_Throw_Without_Consuming_Continuation()
    {
        using var app = CreateServices();
        var window = PreparedWindow();
        var target = new Popup { PlacementTarget = window };
        Action? resume = null;
        window.NativePopupShowRequested = (_, show) => resume = show;
        target.Open();
        var impl = Mock.Get(((PopupRoot)target.Host!).PlatformImpl!);

        Exception? error = null;
        var worker = new Thread(() => error = Record.Exception(() => resume!()));
        worker.Start();
        worker.Join();
        Assert.IsType<InvalidOperationException>(error);
        impl.Verify(x => x.Show(It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        resume!();
        impl.Verify(x => x.Show(true, false), Times.Once);
        target.Close();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ungated_Native_And_Overlay_Popups_Should_Open_Synchronously(bool useOverlay)
    {
        using var app = CreateServices();
        var window = PreparedWindow();
        var target = new Popup { PlacementTarget = window, ShouldUseOverlayLayer = useOverlay };
        var prepared = 0;
        if (useOverlay)
            window.NativePopupShowRequested = (_, _) => ++prepared;
        var opened = 0;
        target.Opened += (_, _) => ++opened;

        target.Open();

        Assert.True(target.IsOpen);
        Assert.Equal(useOverlay, target.IsUsingOverlayLayer);
        Assert.Equal(1, opened);
        Assert.Equal(0, prepared);
        target.Close();
    }

    [Fact]
    public void Cancelling_In_IsOpen_Notification_Should_Not_Request_Preparation()
    {
        using var app = CreateServices();
        var window = PreparedWindow();
        var target = new Popup { PlacementTarget = window };
        var prepared = 0;
        window.NativePopupShowRequested = (_, _) => ++prepared;
        target.PropertyChanged += (_, e) =>
        {
            if (e.Property == Popup.IsOpenProperty && target.IsOpen)
                target.IsOpen = false;
        };

        target.Open();

        Assert.Equal(0, prepared);
        Assert.False(target.IsOpen);
        Assert.Null(target.Host);
        Assert.Empty(window.OpenedPopups);
    }

    [Fact]
    public void Deferred_Popup_Should_Preserve_Focus_Until_Resume_And_Restore_On_Close()
    {
        using var app = UnitTestApplication.Start(TestServices.FocusableWindow);
        var window = PreparedWindow();
        var button = new Button();
        var popupButton = new Button();
        window.Content = button;
        var target = new Popup { PlacementTarget = button, Child = popupButton };
        Action? resume = null;
        window.NativePopupShowRequested = (_, show) => resume = show;
        Assert.True(button.Focus());
        target.Opened += (_, _) => Assert.True(popupButton.Focus());

        target.Open();
        Assert.Same(button, window.FocusManager!.GetFocusedElement());
        resume!();
        Assert.Same(popupButton, window.FocusManager.GetFocusedElement());
        target.Close();
        Assert.Same(button, window.FocusManager.GetFocusedElement());
    }

    private static IDisposable CreateServices() =>
        UnitTestApplication.Start(TestServices.StyledWindow.With(windowingPlatform: new MockWindowingPlatform()));

    private static Window PreparedWindow()
    {
        var window = new Window();
        window.Show();
        window.ApplyStyling();
        window.ApplyTemplate();
        return window;
    }
}
