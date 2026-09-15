using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Browser.Interop;
using Avalonia.Collections.Pooled;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.Browser;

/// <summary>
/// Consumer side of the browser input ring buffer.
/// The ring lives in WASM linear memory; JavaScript appends encoded input records to it and
/// this class decodes them into raw input events. See browser-input-v2.md for the contract.
/// </summary>
internal static unsafe class BrowserInputQueue
{
    // Control block layout (bytes). Must match inputQueue.ts.
    private const int AbiVersion = 1;
    private const int ControlBlockSize = 32;
    private const int OffsetAbiVersion = 0;
    private const int OffsetCapacity = 4;
    private const int OffsetHead = 8;
    private const int OffsetTail = 12;
    private const int OffsetFlags = 16;
    private const int FlagOverflow = 1;

    // Record header layout (bytes). Must match inputQueue.ts.
    private const int HeaderSize = 16;
    private const int OffsetType = 0;
    private const int OffsetSize = 4;
    private const int OffsetTopLevelId = 8;

    private const int DefaultCapacity = 64 * 1024;

    private enum RecordType : ushort
    {
        Padding = 0,
        PointerMove = 1,
        PointerDown = 2,
        PointerUp = 3,
        PointerCancel = 4,
        Wheel = 5,
        KeyDown = 6,
        KeyUp = 7
    }

    private static byte* s_control;
    private static byte* s_data;
    private static int s_capacity;
    private static bool s_draining;

    public static bool IsInitialized => s_control != null;

    public static void Initialize()
    {
        if (s_control != null)
            return;

        s_capacity = DefaultCapacity;
        s_control = (byte*)NativeMemory.AlignedAlloc((nuint)(ControlBlockSize + s_capacity), 8);
        NativeMemory.Clear(s_control, (nuint)(ControlBlockSize + s_capacity));
        s_data = s_control + ControlBlockSize;

        *(int*)(s_control + OffsetAbiVersion) = AbiVersion;
        *(int*)(s_control + OffsetCapacity) = s_capacity;

        InputHelper.AttachInputQueue((int)(nint)s_control, BrowserWindowingPlatform.IsThreadingEnabled);
    }

    private static int Head => *(int*)(s_control + OffsetHead);
    private static int Tail => Volatile.Read(ref *(int*)(s_control + OffsetTail));
    private static int Flags => Volatile.Read(ref *(int*)(s_control + OffsetFlags));

    /// <summary>
    /// True when the ring or the JS overflow list hold records that were not decoded yet.
    /// Plain memory reads, safe to call from the dispatcher's queue lock.
    /// Reports false while a drain is in progress: those records are being handed to the dispatcher
    /// right now, and the Signaled handler runs as soon as the drain completes.
    /// </summary>
    public static bool HasPendingInput =>
        s_control != null && !s_draining && (Head != Tail || (Flags & FlagOverflow) != 0);

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
            while (true)
            {
                DrainRing();

                if ((Flags & FlagOverflow) == 0)
                    break;

                // The ring is empty now; let JS move as many spilled records back as fit.
                if (InputHelper.SpillOverflow() == 0)
                    break;
            }
        }
        finally
        {
            s_draining = false;
        }
    }

    /// <summary>
    /// Drains the ring and dispatches everything inline, returning the handled state of the last event.
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

    private static void DrainRing()
    {
        var head = Head;
        while (true)
        {
            if (head == Tail)
                return;

            var record = s_data + head;
            var type = (RecordType)(*(ushort*)(record + OffsetType));
            var size = *(int*)(record + OffsetSize);

            if (size <= 0 || head + size > s_capacity)
            {
                // Corrupted ring; resynchronise with the producer instead of looping forever.
                head = Tail;
                Volatile.Write(ref *(int*)(s_control + OffsetHead), head);
                return;
            }

            if (type != RecordType.Padding)
                Decode(type, record);

            head += size;
            if (head >= s_capacity)
                head = 0;

            // Publish only after the record was fully read: the producer may reuse the space right away.
            Volatile.Write(ref *(int*)(s_control + OffsetHead), head);
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
