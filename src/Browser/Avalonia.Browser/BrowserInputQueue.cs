using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Browser.Interop;
using Avalonia.Collections.Pooled;
using Avalonia.Input;
using Avalonia.Input.Raw;
using static Avalonia.Browser.BrowserInputLayout;
using BrowserPointerType = Avalonia.Browser.BrowserInputLayout.PointerType;

namespace Avalonia.Browser;

/// <summary>
/// Consumer side of the browser input queue.
/// The queue is a chain of segments in WASM linear memory. JavaScript appends input records to the last segment,
/// allocating a new one with malloc when a record doesn't fit, and this class decodes them into raw input events,
/// freeing every segment it has read past. JS copies DOM event fields as they are; mapping them to Avalonia's
/// input model happens here. See <see cref="BrowserInputLayout"/> for the layout.
/// </summary>
internal static unsafe class BrowserInputQueue
{
    private const int DefaultSegmentSize = 64 * 1024;

    // A wheel notch is 3 lines in line mode and 100 pixels in pixel mode, so a line is a third of that.
    private const double WheelLinePixels = 100.0 / 3;
    private const int DomDeltaLine = 1;
    private const int DomDeltaPage = 2;

    private static ControlBlock* s_control;
    private static SegmentHeader* s_segment;
    private static bool s_draining;

    public static void Initialize()
    {
        if (s_control != null)
            return;

        // Segments are allocated by JS with the same malloc, and freed here with NativeMemory.Free.
        s_segment = (SegmentHeader*)NativeMemory.Alloc((nuint)(sizeof(SegmentHeader) + DefaultSegmentSize));
        *s_segment = default;

        s_control = (ControlBlock*)NativeMemory.Alloc((nuint)sizeof(ControlBlock));
        *s_control = new ControlBlock
        {
            SegmentSize = DefaultSegmentSize,
            Head = (int)(nint)(s_segment + 1),
            Tail = (int)(nint)(s_segment + 1)
        };

        InputHelper.AttachInputQueue((int)(nint)s_control, BrowserWindowingPlatform.IsThreadingEnabled);
    }

    /// <summary>
    /// Decodes every queued record into the per-top-level <see cref="RawEventGrouper"/>.
    /// Does not dispatch anything by itself.
    /// </summary>
    public static void Drain() => DrainCore();

    // Returns the event decoded from the last record, or null when the queue was empty or that record produced none.
    private static RawInputEventArgs? DrainCore()
    {
        if (s_control == null || s_draining)
            return null;

        s_draining = true;
        try
        {
            return DrainSegments();
        }
        finally
        {
            s_draining = false;
        }
    }

    /// <summary>
    /// Drains the queue and dispatches everything inline, returning the handled state of the last record decoded.
    /// JS calls this right after posting a record, so that is the record whose DOM handler is asking.
    /// Only valid on a single-threaded runtime, where the dispatcher runs on the browser main thread.
    /// </summary>
    public static bool FlushSync()
    {
        if (s_draining)
            return false;

        var posted = DrainCore();

        if (BrowserWindowingPlatform.AutomaticEventGrouperDispatchQueue is not { } queue)
            return false;

        s_draining = true;
        try
        {
            queue.DrainAll();
            return posted?.Handled ?? false;
        }
        finally
        {
            s_draining = false;
        }
    }

    private static RawInputEventArgs? DrainSegments()
    {
        RawInputEventArgs? last = null;

        // Full fence: the clear must be visible before the tail is read below.
        Interlocked.Exchange(ref s_control->WakeRequested, 0);

        var head = (byte*)s_control->Head;
        while (true)
        {
            var tail = (byte*)Volatile.Read(ref s_control->Tail);
            if (head == tail)
                return last;

            var end = (byte*)Volatile.Read(ref s_segment->End);
            if (head == end)
            {
                // Sealed and fully read. The tail has moved on, so the next segment is linked already.
                var next = (SegmentHeader*)Volatile.Read(ref s_segment->Next);
                NativeMemory.Free(s_segment);
                s_segment = next;
                head = (byte*)(next + 1);
            }
            else
            {
                var record = (RecordHeader*)head;
                var limit = end != null ? end : tail;
                if (record->Size < sizeof(RecordHeader) || head + record->Size > limit)
                    throw new InvalidOperationException("Corrupted browser input queue.");

                last = Decode(record);
                head += record->Size;
            }

            Volatile.Write(ref s_control->Head, (int)(nint)head);
        }
    }

    private static RawInputEventArgs? Decode(RecordHeader* record) => record->Type switch
    {
        RecordType.PointerMove or RecordType.PointerDown or RecordType.PointerUp or RecordType.PointerCancel =>
            DecodePointer((PointerRecord*)record),
        RecordType.Wheel => DecodeWheel((WheelRecord*)record),
        RecordType.KeyDown or RecordType.KeyUp => DecodeKey((KeyRecord*)record),
        _ => null
    };

    private static RawInputEventArgs? DecodePointer(PointerRecord* record)
    {
        var type = record->Header.Type;
        var pointerType = record->PointerType;

        // Only touch cancellation is meaningful to Avalonia.
        if (type == RecordType.PointerCancel && pointerType != BrowserPointerType.Touch)
            return null;
        if (BrowserTopLevelImpl.TryGetTopLevel(record->TopLevelId) is not { } topLevel)
            return null;

        var eventType = type switch
        {
            RecordType.PointerMove => pointerType == BrowserPointerType.Touch
                ? RawPointerEventType.TouchUpdate
                : RawPointerEventType.Move,
            RecordType.PointerDown => pointerType == BrowserPointerType.Touch
                ? RawPointerEventType.TouchBegin
                : ButtonDownType(record->Button),
            RecordType.PointerUp => pointerType == BrowserPointerType.Touch
                ? RawPointerEventType.TouchEnd
                : ButtonUpType(record->Button),
            _ => RawPointerEventType.TouchCancel
        };

        PooledList<RawPointerPoint>? intermediatePoints = null;
        if (type == RecordType.PointerMove && record->CoalescedCount > 0)
        {
            var points = (PointRecord*)(record + 1);
            intermediatePoints = new PooledList<RawPointerPoint>(record->CoalescedCount);
            for (var i = 0; i < record->CoalescedCount; i++)
                intermediatePoints.Add(ToRawPoint(points + i));
        }

        return topLevel.InputHandler.OnQueuedPointerEvent(eventType, pointerType, (long)record->PointerId,
            (ulong)record->TimeStamp, ToRawPoint(&record->Point),
            ToModifiers(record->KeyModifiers, record->Buttons, pointerType), intermediatePoints);
    }

    private static RawInputEventArgs? DecodeWheel(WheelRecord* record)
    {
        if (BrowserTopLevelImpl.TryGetTopLevel(record->TopLevelId) is not { } topLevel)
            return null;

        var (scaleX, scaleY) = record->DeltaMode switch
        {
            DomDeltaLine => (WheelLinePixels, WheelLinePixels),
            DomDeltaPage => ((double)record->PageWidth, (double)record->PageHeight),
            _ => (1.0, 1.0)
        };

        return topLevel.InputHandler.OnQueuedWheelEvent((ulong)record->TimeStamp,
            new Point(record->OffsetX, record->OffsetY),
            new Vector(record->DeltaX * scaleX, record->DeltaY * scaleY),
            ToModifiers(record->KeyModifiers, record->Buttons, BrowserPointerType.Mouse));
    }

    private static RawInputEventArgs? DecodeKey(KeyRecord* record)
    {
        if (BrowserTopLevelImpl.TryGetTopLevel(record->TopLevelId) is not { } topLevel)
            return null;

        var chars = (char*)(record + 1);
        var key = new string(chars, 0, record->KeyLength);
        var code = new string(chars + record->KeyLength, 0, record->CodeLength);

        return topLevel.InputHandler.OnQueuedKeyEvent(
            record->Header.Type == RecordType.KeyDown ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
            (ulong)record->TimeStamp, code, key, ToModifiers(record->KeyModifiers, 0, BrowserPointerType.Mouse));
    }

    private static RawPointerPoint ToRawPoint(PointRecord* p) => new()
    {
        Position = new Point(p->OffsetX, p->OffsetY),
        Pressure = p->Pressure,
        XTilt = p->TiltX,
        YTilt = p->TiltY,
        Twist = p->Twist
    };

    /// <summary>
    /// Maps DOM modifier keys and the MouseEvent.buttons bit mask to Avalonia modifiers.
    /// </summary>
    internal static RawInputModifiers ToModifiers(DomKeyModifiers keys, int buttons, BrowserPointerType pointerType)
    {
        var modifiers = RawInputModifiers.None;

        if ((keys & DomKeyModifiers.Alt) != 0)
            modifiers |= RawInputModifiers.Alt;
        if ((keys & DomKeyModifiers.Control) != 0)
            modifiers |= RawInputModifiers.Control;
        if ((keys & DomKeyModifiers.Shift) != 0)
            modifiers |= RawInputModifiers.Shift;
        if ((keys & DomKeyModifiers.Meta) != 0)
            modifiers |= RawInputModifiers.Meta;

        if ((buttons & 1) != 0)
            modifiers |= RawInputModifiers.LeftMouseButton;
        if ((buttons & 2) != 0)
            modifiers |= pointerType == BrowserPointerType.Pen
                ? RawInputModifiers.PenBarrelButton
                : RawInputModifiers.RightMouseButton;
        if ((buttons & 4) != 0)
            modifiers |= RawInputModifiers.MiddleMouseButton;
        if ((buttons & 8) != 0)
            modifiers |= RawInputModifiers.XButton1MouseButton;
        if ((buttons & 16) != 0)
            modifiers |= RawInputModifiers.XButton2MouseButton;
        if ((buttons & 32) != 0)
            modifiers |= RawInputModifiers.PenEraser;

        return modifiers;
    }

    private static RawPointerEventType ButtonDownType(int button) => button switch
    {
        0 => RawPointerEventType.LeftButtonDown,
        1 => RawPointerEventType.MiddleButtonDown,
        2 => RawPointerEventType.RightButtonDown,
        3 => RawPointerEventType.XButton1Down,
        4 => RawPointerEventType.XButton2Down,
        5 => RawPointerEventType.XButton1Down, // should be pen eraser button
        _ => RawPointerEventType.Move
    };

    private static RawPointerEventType ButtonUpType(int button) => button switch
    {
        0 => RawPointerEventType.LeftButtonUp,
        1 => RawPointerEventType.MiddleButtonUp,
        2 => RawPointerEventType.RightButtonUp,
        3 => RawPointerEventType.XButton1Up,
        4 => RawPointerEventType.XButton2Up,
        5 => RawPointerEventType.XButton1Up, // should be pen eraser button
        _ => RawPointerEventType.Move
    };
}
