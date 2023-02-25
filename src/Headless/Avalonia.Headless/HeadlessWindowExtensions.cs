using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Headless;

public static class HeadlessWindowExtensions
{
    /// <summary>
    /// Triggers a renderer timer tick and captures last rendered frame.
    /// </summary>
    /// <returns>Bitmap with last rendered frame. Null, if nothing was rendered.</returns>
    public static Bitmap? CaptureRenderedFrame(this TopLevel topLevel)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        return topLevel.GetLastRenderedFrame();
    }
    
    /// <summary>
    /// Reads last rendered frame.
    /// Note, in order to trigger rendering timer, call <see cref="AvaloniaHeadlessPlatform.ForceRenderTimerTick"/> method.  
    /// </summary>
    /// <returns>Bitmap with last rendered frame. Null, if nothing was rendered.</returns>
    public static Bitmap? GetLastRenderedFrame(this TopLevel topLevel)
    {
        if (AvaloniaLocator.Current.GetService<IPlatformRenderInterface>() is HeadlessPlatformRenderInterface)
        {
            throw new NotSupportedException("To capture a rendered frame, make sure that headless application was initialized with '.UseSkia()' and disabled 'UseHeadlessDrawing' in the 'AvaloniaHeadlessPlatformOptions'.");
        }

        return GetImpl(topLevel).GetLastRenderedFrame();
    }

    /// <summary>
    /// Simulates keyboard press on the headless window/toplevel.
    /// </summary>
    public static void KeyPress(this TopLevel topLevel, Key key, RawInputModifiers modifiers) =>
        GetImpl(topLevel).KeyPress(key, modifiers);

    /// <summary>
    /// Simulates keyboard release on the headless window/toplevel.
    /// </summary>
    public static void KeyRelease(this TopLevel topLevel, Key key, RawInputModifiers modifiers) =>
        GetImpl(topLevel).KeyRelease(key, modifiers);

    /// <summary>
    /// Simulates mouse down on the headless window/toplevel.
    /// </summary>
    public static void MouseDown(this TopLevel topLevel, Point point, MouseButton button,
        RawInputModifiers modifiers = RawInputModifiers.None) => GetImpl(topLevel).MouseDown(point, button, modifiers);

    /// <summary>
    /// Simulates mouse move on the headless window/toplevel.
    /// </summary>
    public static void MouseMove(this TopLevel topLevel, Point point,
        RawInputModifiers modifiers = RawInputModifiers.None) => GetImpl(topLevel).MouseMove(point, modifiers);

    /// <summary>
    /// Simulates mouse up on the headless window/toplevel.
    /// </summary>
    public static void MouseUp(this TopLevel topLevel, Point point, MouseButton button,
        RawInputModifiers modifiers = RawInputModifiers.None) => GetImpl(topLevel).MouseUp(point, button, modifiers);

    /// <summary>
    /// Simulates mouse wheel on the headless window/toplevel.
    /// </summary>
    public static void MouseWheel(this TopLevel topLevel, Point point, Vector delta,
        RawInputModifiers modifiers = RawInputModifiers.None) => GetImpl(topLevel).MouseWheel(point, delta, modifiers);

    /// <summary>
    /// Simulates drag'n'drop target on the headless window/toplevel.
    /// </summary>
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
