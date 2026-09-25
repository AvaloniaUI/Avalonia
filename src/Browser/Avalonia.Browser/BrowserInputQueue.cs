using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Browser.Interop;
using Avalonia.Collections.Pooled;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.Browser;

/// <summary>
/// Consumer side of the browser input queue.
/// The queue is a chain of segments in WASM linear memory. JavaScript appends encoded input records
/// to the last segment, allocating a new one with malloc when a record doesn't fit, and this class
/// decodes them into raw input events, freeing every segment it has read past.
/// </summary>
internal static unsafe class BrowserInputQueue
{
    // Layouts (bytes). Must match inputQueue.ts.
    // Control block, all i32:
    //   0 abiVersion  4 segmentSize  8 head (C#)  12 tail (JS)  16 wakeRequested
    // head and tail are absolute addresses; the queue is empty when they are equal.
    // JS sets wakeRequested after publishing a tail and posts a wake if it was clear; C# clears it
    // before reading the tail, so a record published after the last read always gets a wake.
    private const int AbiVersion = 2;
    private const int ControlBlockSize = 20;
    private const int OffsetAbiVersion = 0;
    private const int OffsetSegmentSize = 4;
    private const int OffsetHead = 8;
    private const int OffsetTail = 12;
    private const int OffsetWakeRequested = 16;

    // Segment header, all i32: 0 next (0 until sealed)  4 end (0 until sealed). Records follow it.
    // JS writes end, then next, then publishes a tail in the next segment.
    private const int SegmentHeaderSize = 8;
    private const int OffsetNext = 0;
    private const int OffsetEnd = 4;

    // Record header: 0 type u16  2 reserved u16  4 size u32  8 topLevelId i32  12 reserved i32
    private const int HeaderSize = 16;
    private const int OffsetType = 0;
    private const int OffsetSize = 4;
    private const int OffsetTopLevelId = 8;

    private const int DefaultSegmentSize = 64 * 1024;

    private enum RecordType : ushort
    {
        PointerMove = 1,
        PointerDown = 2,
        PointerUp = 3,
        PointerCancel = 4,
        Wheel = 5,
        KeyDown = 6,
        KeyUp = 7
    }

    private static byte* s_control;
    private static byte* s_segment;
    private static bool s_draining;

    public static void Initialize()
    {
        if (s_control != null)
            return;

        // Segments are allocated by JS with the same malloc, and freed here with NativeMemory.Free.
        s_segment = (byte*)NativeMemory.Alloc(SegmentHeaderSize + DefaultSegmentSize);
        *(int*)(s_segment + OffsetNext) = 0;
        *(int*)(s_segment + OffsetEnd) = 0;

        s_control = (byte*)NativeMemory.Alloc(ControlBlockSize);
        *(int*)(s_control + OffsetAbiVersion) = AbiVersion;
        *(int*)(s_control + OffsetSegmentSize) = DefaultSegmentSize;
        *(int*)(s_control + OffsetHead) = (int)(nint)(s_segment + SegmentHeaderSize);
        *(int*)(s_control + OffsetTail) = (int)(nint)(s_segment + SegmentHeaderSize);
        *(int*)(s_control + OffsetWakeRequested) = 0;

        InputHelper.AttachInputQueue((int)(nint)s_control, BrowserWindowingPlatform.IsThreadingEnabled);
    }

    /// <summary>
    /// Decodes every queued record into the per-top-level <see cref="RawEventGrouper"/>.
    /// Does not dispatch anything by itself.
    /// </summary>
    public static void Drain()
    {
        if (s_control == null || s_draining)
            return;

        s_draining = true;
        try
        {
            DrainSegments();
        }
        finally
        {
            s_draining = false;
        }
    }

    /// <summary>
    /// Drains the queue and dispatches everything inline, returning the handled state of the last event.
    /// Only valid on a single-threaded runtime, where the dispatcher runs on the browser main thread.
    /// </summary>
    public static bool FlushSync()
    {
        if (s_draining)
            return false;

        Drain();

        if (BrowserWindowingPlatform.AutomaticEventGrouperDispatchQueue is not { } queue)
            return false;

        s_draining = true;
        try
        {
            return queue.DrainAll();
        }
        finally
        {
            s_draining = false;
        }
    }

    private static void DrainSegments()
    {
        // Full fence: the clear must be visible before the tail is read below.
        Interlocked.Exchange(ref *(int*)(s_control + OffsetWakeRequested), 0);

        var head = (byte*)*(int*)(s_control + OffsetHead);
        while (true)
        {
            var tail = (byte*)Volatile.Read(ref *(int*)(s_control + OffsetTail));
            if (head == tail)
                return;

            var end = (byte*)Volatile.Read(ref *(int*)(s_segment + OffsetEnd));
            if (head == end)
            {
                // Sealed and fully read. The tail has moved on, so the next segment is linked already.
                var next = (byte*)Volatile.Read(ref *(int*)(s_segment + OffsetNext));
                NativeMemory.Free(s_segment);
                s_segment = next;
                head = next + SegmentHeaderSize;
            }
            else
            {
                var type = (RecordType)(*(ushort*)(head + OffsetType));
                var size = *(int*)(head + OffsetSize);
                var limit = end != null ? end : tail;
                if (size < HeaderSize || head + size > limit)
                    throw new InvalidOperationException("Corrupted browser input queue.");

                Decode(type, head);
                head += size;
            }

            Volatile.Write(ref *(int*)(s_control + OffsetHead), (int)(nint)head);
        }
    }

    private static void Decode(RecordType type, byte* record)
    {
        var topLevelId = *(int*)(record + OffsetTopLevelId);
        if (BrowserTopLevelImpl.TryGetTopLevel(topLevelId) is not { } topLevel)
            return;

        var handler = topLevel.InputHandler;
        var body = record + HeaderSize;

        switch (type)
        {
            case RecordType.PointerMove:
            case RecordType.PointerDown:
            case RecordType.PointerUp:
            case RecordType.PointerCancel:
                DecodePointer(type, body, handler);
                break;
            case RecordType.Wheel:
                DecodeWheel(body, handler);
                break;
            case RecordType.KeyDown:
            case RecordType.KeyUp:
                DecodeKey(type, body, handler);
                break;
        }
    }

    private static void DecodePointer(RecordType type, byte* body, BrowserInputHandler handler)
    {
        // pointer body layout, offsets relative to body start (see inputQueue.ts):
        //  0 timeStamp f64, 8 pointerId f64, 16 pointerType u8, 20 button i32, 24 modifiers i32,
        // 32 offsetX f64, 40 offsetY f64, 48 pressure f32, 52 tiltX f32, 56 tiltY f32, 60 twist f32
        // PointerMove appends: 64 pointCount i32, 72 points[pointCount] x 32 bytes
        var timeStamp = (ulong)*(double*)(body + 0);
        var pointerId = (long)*(double*)(body + 8);
        var pointerType = (BrowserPointerType)body[16];
        var button = *(int*)(body + 20);
        var modifiers = (RawInputModifiers)(*(int*)(body + 24));
        var point = ReadPoint(body + 32);

        PooledList<RawPointerPoint>? intermediatePoints = null;
        if (type == RecordType.PointerMove)
        {
            var pointCount = *(int*)(body + 64);
            if (pointCount > 0)
            {
                intermediatePoints = new PooledList<RawPointerPoint>(pointCount);
                var points = body + 72;
                for (var i = 0; i < pointCount; i++)
                    intermediatePoints.Add(ReadPoint(points + i * 32));
            }
        }

        var eventType = type switch
        {
            RecordType.PointerMove => pointerType == BrowserPointerType.Touch
                ? RawPointerEventType.TouchUpdate
                : RawPointerEventType.Move,
            RecordType.PointerDown => pointerType == BrowserPointerType.Touch
                ? RawPointerEventType.TouchBegin
                : ButtonDownType(button),
            RecordType.PointerUp => pointerType == BrowserPointerType.Touch
                ? RawPointerEventType.TouchEnd
                : ButtonUpType(button),
            RecordType.PointerCancel => RawPointerEventType.TouchCancel,
            _ => RawPointerEventType.Move
        };

        // Only touch cancellation is meaningful to Avalonia.
        if (type == RecordType.PointerCancel && pointerType != BrowserPointerType.Touch)
        {
            intermediatePoints?.Dispose();
            return;
        }

        handler.OnQueuedPointerEvent(eventType, pointerType, pointerId, timeStamp, point, modifiers, intermediatePoints);
    }

    private static RawPointerPoint ReadPoint(byte* p) => new()
    {
        Position = new Point(*(double*)(p + 0), *(double*)(p + 8)),
        Pressure = *(float*)(p + 16),
        XTilt = *(float*)(p + 20),
        YTilt = *(float*)(p + 24),
        Twist = *(float*)(p + 28)
    };

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

    private static void DecodeWheel(byte* body, BrowserInputHandler handler)
    {
        // wheel body: 0 timeStamp f64, 8 offsetX f64, 16 offsetY f64, 24 deltaX f64, 32 deltaY f64,
        // 40 deltaMode i32, 44 modifiers i32
        var timeStamp = (ulong)*(double*)(body + 0);
        var position = new Point(*(double*)(body + 8), *(double*)(body + 16));
        var deltaX = *(double*)(body + 24);
        var deltaY = *(double*)(body + 32);
        var modifiers = (RawInputModifiers)(*(int*)(body + 44));

        handler.OnQueuedWheelEvent(timeStamp, position, deltaX, deltaY, modifiers);
    }

    private static void DecodeKey(RecordType type, byte* body, BrowserInputHandler handler)
    {
        // key body: 0 timeStamp f64, 8 modifiers i32, 12 keyLength i32, 16 codeLength i32,
        // 24 key u16[keyLength], then code u16[codeLength]
        var timeStamp = (ulong)*(double*)(body + 0);
        var modifiers = (RawInputModifiers)(*(int*)(body + 8));
        var keyLength = *(int*)(body + 12);
        var codeLength = *(int*)(body + 16);
        var chars = (char*)(body + 24);
        var key = keyLength > 0 ? new string(chars, 0, keyLength) : string.Empty;
        var code = codeLength > 0 ? new string(chars + keyLength, 0, codeLength) : string.Empty;

        handler.OnQueuedKeyEvent(
            type == RecordType.KeyDown ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
            timeStamp, code, key, modifiers);
    }
}

internal enum BrowserPointerType : byte
{
    Mouse = 0,
    Touch = 1,
    Pen = 2
}
