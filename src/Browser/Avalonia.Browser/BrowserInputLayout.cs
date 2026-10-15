using System;
using System.Runtime.InteropServices;

namespace Avalonia.Browser;

/// <summary>
/// Memory layout of the browser input queue, shared with inputQueue.ts.
/// These types are the only definition of the layout: Avalonia.Browser.BuildTasks generates inputLayout.g.ts from
/// them at build time, with each struct's field offsets and size and each enum's values.
/// Structs must not contain implicit padding, so the layout is the same on every runtime; the generator fails the
/// build otherwise. Records are 8-byte aligned. Variable-length data follows a record at sizeof(record).
/// </summary>
internal static class BrowserInputLayout
{
    /// <summary>
    /// Addresses are absolute WASM addresses; the queue is empty when <see cref="Head"/> equals <see cref="Tail"/>.
    /// JS sets <see cref="WakeRequested"/> after publishing a tail and signals the dispatcher if it was clear;
    /// C# clears it before reading the tail, so a record published after the last read always gets a wake.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ControlBlock
    {
        public int SegmentSize;
        public int Head;
        public int Tail;
        public int WakeRequested;
    }

    /// <summary>
    /// Precedes the records of every segment. Both fields stay 0 until JS seals the segment:
    /// it writes <see cref="End"/>, then <see cref="Next"/>, then publishes a tail in the next segment.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SegmentHeader
    {
        public int Next;
        public int End;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RecordHeader
    {
        public RecordType Type;
        public int Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PointRecord
    {
        public double OffsetX;
        public double OffsetY;
        public float Pressure;
        public float TiltX;
        public float TiltY;
        public float Twist;
    }

    /// <summary>
    /// <see cref="PointRecord"/>[<see cref="CoalescedCount"/>] follows, oldest first, excluding the primary point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PointerRecord
    {
        public RecordHeader Header;
        public double TimeStamp;
        public double PointerId;
        public PointRecord Point;
        public int TopLevelId;
        public PointerType PointerType;
        public int Button;
        public int Buttons;
        public DomKeyModifiers KeyModifiers;
        public int CoalescedCount;
    }

    /// <summary>
    /// Deltas are in DOM <see cref="DeltaMode"/> units; the page size is the event target's client size.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WheelRecord
    {
        public RecordHeader Header;
        public double TimeStamp;
        public double OffsetX;
        public double OffsetY;
        public double DeltaX;
        public double DeltaY;
        public int TopLevelId;
        public int DeltaMode;
        public int Buttons;
        public DomKeyModifiers KeyModifiers;
        public int PageWidth;
        public int PageHeight;
    }

    /// <summary>
    /// UTF-16 key[<see cref="KeyLength"/>], then code[<see cref="CodeLength"/>] follow.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KeyRecord
    {
        public RecordHeader Header;
        public double TimeStamp;
        public int TopLevelId;
        public DomKeyModifiers KeyModifiers;
        public int KeyLength;
        public int CodeLength;
    }

    public enum RecordType
    {
        PointerMove = 1,
        PointerDown = 2,
        PointerUp = 3,
        PointerCancel = 4,
        Wheel = 5,
        KeyDown = 6,
        KeyUp = 7
    }

    public enum PointerType
    {
        Mouse = 0,
        Touch = 1,
        Pen = 2
    }

    [Flags]
    public enum DomKeyModifiers
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Meta = 8
    }
}
