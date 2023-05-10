using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Headless;

/// <summary>
/// Set of extension methods to simplify usage of Avalonia.Headless platform.
/// </summary>
public static class HeadlessWindowExtensions
{
    /// <summary>
    /// Triggers a renderer timer tick and captures last rendered frame.
    /// </summary>
    /// <returns>Bitmap with last rendered frame. Null, if nothing was rendered.</returns>
    public static WriteableBitmap? CaptureRenderedFrame(this TopLevel topLevel)
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
    public static WriteableBitmap? GetLastRenderedFrame(this TopLevel topLevel)
    {
        if (AvaloniaLocator.Current.GetService<IPlatformRenderInterface>() is HeadlessPlatformRenderInterface)
        {
            throw new NotSupportedException(
                "To capture a rendered frame, make sure that headless application was initialized with '.UseSkia()' and disabled 'UseHeadlessDrawing' in the 'AvaloniaHeadlessPlatformOptions'.");
        }

        return GetImpl(topLevel).GetLastRenderedFrame();
    }

    /// <summary>
    /// Simulates keyboard press on the headless window/toplevel.
    /// </summary>
    public static void KeyPress(this TopLevel topLevel, Key key, RawInputModifiers modifiers) =>
        RunJobsOnImpl(topLevel, w => w.KeyPress(key, modifiers));

    /// <summary>
    /// Simulates keyboard release on the headless window/toplevel.
    /// </summary>
    public static void KeyRelease(this TopLevel topLevel, Key key, RawInputModifiers modifiers) =>
        RunJobsOnImpl(topLevel, w => w.KeyRelease(key, modifiers));

    /// <summary>
    /// Simulates mouse down on the headless window/toplevel.
    /// </summary>
    public static void MouseDown(this TopLevel topLevel, Point point, MouseButton button,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.MouseDown(point, button, modifiers));

    /// <summary>
    /// Simulates mouse move on the headless window/toplevel.
    /// </summary>
    public static void MouseMove(this TopLevel topLevel, Point point,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.MouseMove(point, modifiers));

    /// <summary>
    /// Simulates mouse up on the headless window/toplevel.
    /// </summary>
    public static void MouseUp(this TopLevel topLevel, Point point, MouseButton button,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.MouseUp(point, button, modifiers));

    /// <summary>
    /// Simulates mouse wheel on the headless window/toplevel.
    /// </summary>
    public static void MouseWheel(this TopLevel topLevel, Point point, Vector delta,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.MouseWheel(point, delta, modifiers));

    /// <summary>
    /// Simulates drag'n'drop target on the headless window/toplevel.
    /// </summary>
    public static void DragDrop(this TopLevel topLevel, Point point, RawDragEventType type, IDataObject data,
        DragDropEffects effects, RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.DragDrop(point, type, data, effects, modifiers));

    private static void RunJobsOnImpl(this TopLevel topLevel, Action<IHeadlessWindow> action)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        action(GetImpl(topLevel));
        Dispatcher.UIThread.RunJobs();
    }

    private static IHeadlessWindow GetImpl(this TopLevel topLevel)
    {
        return topLevel.PlatformImpl as IHeadlessWindow ??
               throw new InvalidOperationException("TopLevel must be a headless window.");
    }
}
