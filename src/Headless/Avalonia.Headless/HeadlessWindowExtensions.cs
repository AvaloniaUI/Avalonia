using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
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
        WriteableBitmap? bitmap = null;
        topLevel.RunJobsOnImpl(w => bitmap = w.GetLastRenderedFrame());
        return bitmap;
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
    public static void KeyPress(this TopLevel topLevel, Key key, RawInputModifiers modifiers, PhysicalKey physicalKey,
        string? keySymbol) =>
        RunJobsOnImpl(topLevel, w => w.KeyPress(key, modifiers, physicalKey, keySymbol));

    /// <summary>
    /// Simulates keyboard press on the headless window/toplevel, as if typed on a QWERTY keyboard.
    /// </summary>
    public static void KeyPressQwerty(this TopLevel topLevel, PhysicalKey physicalKey, RawInputModifiers modifiers) =>
        RunJobsOnImpl(topLevel, w => w.KeyPress(physicalKey.ToQwertyKey(), modifiers, physicalKey, physicalKey.ToQwertyKeySymbol()));

    /// <summary>
    /// Simulates keyboard release on the headless window/toplevel.
    /// </summary>
    public static void KeyRelease(this TopLevel topLevel, Key key, RawInputModifiers modifiers, PhysicalKey physicalKey,
        string? keySymbol) =>
        RunJobsOnImpl(topLevel, w => w.KeyRelease(key, modifiers, physicalKey, keySymbol));

    /// <summary>
    /// Simulates keyboard release on the headless window/toplevel, as if typed on a QWERTY keyboard.
    /// </summary>
    public static void KeyReleaseQwerty(this TopLevel topLevel, PhysicalKey physicalKey, RawInputModifiers modifiers) =>
        RunJobsOnImpl(topLevel, w => w.KeyRelease(physicalKey.ToQwertyKey(), modifiers, physicalKey, physicalKey.ToQwertyKeySymbol()));

    /// <summary>
    /// Simulates a text input event on the headless window/toplevel.
    /// </summary>
    /// <remarks>
    /// This event is independent of KeyPress and KeyRelease. If you need to simulate text input to a TextBox or a similar control, please use KeyTextInput.
    /// </remarks>
    public static void KeyTextInput(this TopLevel topLevel, string text) =>
        RunJobsOnImpl(topLevel, w => w.TextInput(text));

    /// <summary>
    /// Simulates a mouse down on the headless window/toplevel.
    /// </summary>
    /// <remarks>
    /// In the headless platform, there is a single mouse pointer. For touch input use the TouchBegin method, for pen input use the PenDown/PenMove/PenUp methods.
    /// </remarks>
    public static void MouseDown(this TopLevel topLevel, Point point, MouseButton button,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.MouseDown(point, button, modifiers));

    /// <summary>
    /// Simulates a mouse move on the headless window/toplevel.
    /// </summary>
    public static void MouseMove(this TopLevel topLevel, Point point,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.MouseMove(point, modifiers));

    /// <summary>
    /// Simulates a mouse up on the headless window/toplevel.
    /// </summary>
    public static void MouseUp(this TopLevel topLevel, Point point, MouseButton button,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.MouseUp(point, button, modifiers));

    /// <summary>
    /// Simulates a mouse wheel on the headless window/toplevel.
    /// </summary>
    public static void MouseWheel(this TopLevel topLevel, Point point, Vector delta,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.MouseWheel(point, delta, modifiers));

    /// <summary>
    /// Simulates a touch contact being pressed on the headless window/toplevel.
    /// </summary>
    /// <returns>
    /// A touch point handle to pass to <see cref="TouchMove"/> and <see cref="TouchEnd"/>.
    /// Disposing the handle cancels the contact if it is still pressed.
    /// </returns>
    /// <remarks>
    /// To simulate multi-touch, keep several returned touch points pressed at the same time.
    /// </remarks>
    public static IHeadlessTouchPoint TouchBegin(this TopLevel topLevel, Point point,
        RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var touchPointId = Interlocked.Increment(ref s_nextTouchPointId);
        RunJobsOnImpl(topLevel, w => w.Touch(point, touchPointId, RawPointerEventType.TouchBegin, modifiers));
        return new HeadlessTouchPoint(topLevel, touchPointId, point);
    }

    /// <summary>
    /// Simulates a touch contact being moved on the headless window/toplevel.
    /// </summary>
    /// <param name="topLevel">The target headless top level. Must be the one the touch point was created on.</param>
    /// <param name="touchPoint">The touch point returned from <see cref="TouchBegin"/>.</param>
    /// <param name="point">The new contact position.</param>
    /// <param name="modifiers">The optional key modifiers.</param>
    public static void TouchMove(this TopLevel topLevel, IHeadlessTouchPoint touchPoint, Point point,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        GetTouchPoint(topLevel, touchPoint).Move(point, modifiers);

    /// <summary>
    /// Simulates a touch contact being released on the headless window/toplevel.
    /// </summary>
    /// <param name="topLevel">The target headless top level. Must be the one the touch point was created on.</param>
    /// <param name="touchPoint">The touch point returned from <see cref="TouchBegin"/>.</param>
    /// <param name="point">The position at which the contact is released.</param>
    /// <param name="modifiers">The optional key modifiers.</param>
    public static void TouchEnd(this TopLevel topLevel, IHeadlessTouchPoint touchPoint, Point point,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        GetTouchPoint(topLevel, touchPoint).End(point, modifiers);

    /// <summary>
    /// Simulates a pen tip being pressed on the headless window/toplevel.
    /// </summary>
    /// <param name="topLevel">The target headless top level.</param>
    /// <param name="point">The pen position.</param>
    /// <param name="pressure">The pen pressure, in the 0..1 range.</param>
    /// <param name="xTilt">The pen tilt along the X axis, in the -90..90 degrees range.</param>
    /// <param name="yTilt">The pen tilt along the Y axis, in the -90..90 degrees range.</param>
    /// <param name="twist">The pen rotation around its own axis, in the 0..359 degrees range.</param>
    /// <param name="modifiers">The optional key modifiers.</param>
    public static void PenDown(this TopLevel topLevel, Point point, float pressure = 0.5f,
        float xTilt = 0f, float yTilt = 0f, float twist = 0f,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.Pen(CreatePenPoint(point, pressure, xTilt, yTilt, twist),
            RawPointerEventType.LeftButtonDown, modifiers));

    /// <summary>
    /// Simulates a pen being moved over the headless window/toplevel.
    /// </summary>
    /// <inheritdoc cref="PenDown" path="/param"/>
    public static void PenMove(this TopLevel topLevel, Point point, float pressure = 0.5f,
        float xTilt = 0f, float yTilt = 0f, float twist = 0f,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.Pen(CreatePenPoint(point, pressure, xTilt, yTilt, twist),
            RawPointerEventType.Move, modifiers));

    /// <summary>
    /// Simulates a pen tip being released on the headless window/toplevel.
    /// </summary>
    /// <inheritdoc cref="PenDown" path="/param"/>
    public static void PenUp(this TopLevel topLevel, Point point, float pressure = 0f,
        float xTilt = 0f, float yTilt = 0f, float twist = 0f,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.Pen(CreatePenPoint(point, pressure, xTilt, yTilt, twist),
            RawPointerEventType.LeftButtonUp, modifiers));

    private static RawPointerPoint CreatePenPoint(Point point, float pressure, float xTilt, float yTilt, float twist) =>
        new()
        {
            Position = point,
            Pressure = pressure,
            XTilt = xTilt,
            YTilt = yTilt,
            Twist = twist
        };

    /// <summary>
    /// Simulates a drag and drop target event on the headless window/toplevel. This event simulates a user moving files from another app to the current app.
    /// </summary>
    public static void DragDrop(this TopLevel topLevel, Point point, RawDragEventType type, IDataTransfer data,
        DragDropEffects effects, RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.DragDrop(point, type, data, effects, modifiers));

    /// <summary>
    /// Changes the render scaling (DPI) of the headless window/toplevel.
    /// This simulates a DPI change, triggering scaling changed notifications and a layout pass.
    /// </summary>
    /// <param name="topLevel">The target headless top level.</param>
    /// <param name="scaling">The new render scaling factor. Must be greater than zero.</param>
    public static void SetRenderScaling(this TopLevel topLevel, double scaling) =>
        RunJobsOnImpl(topLevel, w => w.SetRenderScaling(scaling));

    private static void RunJobsOnImpl(this TopLevel topLevel, Action<IHeadlessWindow> action)
    {
        RunJobsAndRender();
        action(GetImpl(topLevel));
        RunJobsAndRender();

        static void RunJobsAndRender()
        {
            var dispatcher = Dispatcher.UIThread;

            // Run jobs and render frames until everything is stable.
            // We use a simple approach: run jobs, render, and repeat until
            // there are no more pending jobs. The render timer tick can schedule
            // new jobs, so we loop until stable.
            for (var i = 0; i < 10; i++)
            {
                dispatcher.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();

                if (!dispatcher.HasJobsWithPriority(DispatcherPriority.MinimumActiveValue))
                    return;
            }

            // Final attempt: run remaining jobs without rendering
            dispatcher.RunJobs();
        }
    }

    private static IHeadlessWindow GetImpl(this TopLevel topLevel)
    {
        return topLevel.PlatformImpl switch
        {
            null => throw new ObjectDisposedException(topLevel.GetType().Name),
            IHeadlessWindow headless => headless,
            _ => throw new InvalidOperationException("TopLevel must be a headless window.")
        };
    }

    private static long s_nextTouchPointId;

    private static HeadlessTouchPoint GetTouchPoint(TopLevel topLevel, IHeadlessTouchPoint touchPoint)
    {
        if (touchPoint is not HeadlessTouchPoint headlessTouchPoint)
            throw new ArgumentException("The touch point was not created by TouchBegin.", nameof(touchPoint));
        if (headlessTouchPoint.TopLevel != topLevel)
            throw new ArgumentException("The touch point belongs to a different toplevel.", nameof(touchPoint));
        return headlessTouchPoint;
    }

    private sealed class HeadlessTouchPoint : IHeadlessTouchPoint
    {
        private readonly long _touchPointId;
        private Point _position;
        private bool _pressed = true;

        public HeadlessTouchPoint(TopLevel topLevel, long touchPointId, Point position)
        {
            TopLevel = topLevel;
            _touchPointId = touchPointId;
            _position = position;
        }

        public TopLevel TopLevel { get; }

        public void Move(Point point, RawInputModifiers modifiers)
        {
            ThrowIfReleased();
            RunJobsOnImpl(TopLevel, w => w.Touch(point, _touchPointId, RawPointerEventType.TouchUpdate, modifiers));
            _position = point;
        }

        public void End(Point point, RawInputModifiers modifiers)
        {
            ThrowIfReleased();
            _pressed = false;
            RunJobsOnImpl(TopLevel, w => w.Touch(point, _touchPointId, RawPointerEventType.TouchEnd, modifiers));
        }

        public void Dispose()
        {
            if (!_pressed)
                return;
            _pressed = false;

            // The toplevel might have been closed already, cancelling all of its touch pointers.
            if (TopLevel.PlatformImpl is IHeadlessWindow)
                RunJobsOnImpl(TopLevel, w => w.Touch(_position, _touchPointId, RawPointerEventType.TouchCancel, RawInputModifiers.None));
        }

        private void ThrowIfReleased()
        {
            if (!_pressed)
                throw new InvalidOperationException("The touch point has already been released.");
        }
    }
}
