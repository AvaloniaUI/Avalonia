using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Avalonia.Headless;

public static class HeadlessWindowExtensions
{
    public static Bitmap? CaptureRenderedFrame(this TopLevel topLevel)
    {
        var impl = GetImpl(topLevel);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        return impl.GetLastRenderedFrame();
    }
    
    public static Bitmap? GetLastRenderedFrame(this TopLevel topLevel) =>
        GetImpl(topLevel).GetLastRenderedFrame();

    public static void KeyPress(this TopLevel topLevel, Key key, RawInputModifiers modifiers) =>
        GetImpl(topLevel).KeyPress(key, modifiers);

    public static void KeyRelease(this TopLevel topLevel, Key key, RawInputModifiers modifiers) =>
        GetImpl(topLevel).KeyRelease(key, modifiers);

    public static void MouseDown(this TopLevel topLevel, Point point, MouseButton button,
        RawInputModifiers modifiers = RawInputModifiers.None) => GetImpl(topLevel).MouseDown(point, button, modifiers);

    public static void MouseMove(this TopLevel topLevel, Point point,
        RawInputModifiers modifiers = RawInputModifiers.None) => GetImpl(topLevel).MouseMove(point, modifiers);

    public static void MouseUp(this TopLevel topLevel, Point point, MouseButton button,
        RawInputModifiers modifiers = RawInputModifiers.None) => GetImpl(topLevel).MouseUp(point, button, modifiers);

    public static void MouseWheel(this TopLevel topLevel, Point point, Vector delta,
        RawInputModifiers modifiers = RawInputModifiers.None) => GetImpl(topLevel).MouseWheel(point, delta, modifiers);

    public static void DragDrop(this TopLevel topLevel, Point point, RawDragEventType type, IDataObject data,
        DragDropEffects effects, RawInputModifiers modifiers = RawInputModifiers.None) =>
        GetImpl(topLevel).DragDrop(point, type, data, effects, modifiers);

    private static IHeadlessWindow GetImpl(this TopLevel topLevel)
    {
        Dispatcher.UIThread.RunJobs();
        return topLevel.PlatformImpl as IHeadlessWindow ??
            throw new InvalidOperationException("TopLevel must be a headless window.");
    }
}
