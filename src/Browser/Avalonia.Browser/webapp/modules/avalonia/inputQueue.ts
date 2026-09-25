import { JsExports } from "./jsExports";
import { RuntimeAPI } from "../../types/dotnet";
import { createPost, Post } from "./singleThreadedDispatcher";

/*
 * Producer side of the browser input queue.
 *
 * The queue is a chain of segments in WASM linear memory. JS appends encoded records to the last
 * segment; when a record doesn't fit, it allocates a new segment with malloc and links it in.
 * C# reads the chain in order and frees every segment it has read past.
 *
 * Layouts below must match BrowserInputQueue.cs.
 *
 * Control block (all i32):
 *   0 abiVersion  4 segmentSize  8 head (C#)  12 tail (JS)  16 wakeRequested
 *   head and tail are absolute addresses; the queue is empty when they are equal.
 *   JS sets wakeRequested after publishing a tail and posts a wake if it was clear; C# clears it
 *   before reading the tail, so a record published after the consumer's last read always gets a wake.
 * Segment header (all i32):
 *   0 next  4 end   both 0 until the segment is sealed; records follow the header.
 * Record header:
 *   0 type u16  2 reserved u16  4 size u32  8 topLevelId i32  12 reserved i32
 * Records are 8-byte aligned.
 */

export enum RawInputModifiers {
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Meta = 8,

    LeftMouseButton = 16,
    RightMouseButton = 32,
    MiddleMouseButton = 64,
    XButton1MouseButton = 128,
    XButton2MouseButton = 256,
    KeyboardMask = Alt | Control | Shift | Meta,

    PenInverted = 512,
    PenEraser = 1024,
    PenBarrelButton = 2048
}

export enum InputRecordType {
    PointerMove = 1,
    PointerDown = 2,
    PointerUp = 3,
    PointerCancel = 4,
    Wheel = 5,
    KeyDown = 6,
    KeyUp = 7
}

const enum PointerType {
    Mouse = 0,
    Touch = 1,
    Pen = 2
}

const AbiVersion = 2;
const OffsetAbiVersion = 0;
const OffsetSegmentSize = 4;
const OffsetHead = 8;
const OffsetTail = 12;
const OffsetWakeRequested = 16;

const SegmentHeaderSize = 8;
const OffsetNext = 0;
const OffsetEnd = 4;

const HeaderSize = 16;
const PointerRecordSize = HeaderSize + 64;
const CoalescedPointsOffset = PointerRecordSize + 8;
const CoalescedPointSize = 32;
const MaxCoalescedPoints = 256;
const WheelRecordSize = HeaderSize + 48;
const KeyRecordHeaderSize = HeaderSize + 24;

export class InputQueue {
    private static runtime: RuntimeAPI | undefined;
    private static controlPtr = 0;
    private static segmentSize = 0;
    private static threadingEnabled = false;

    // Producer state. tail is the published end of written records; limit is the end of the current segment.
    private static segment = 0;
    private static limit = 0;
    private static tail = 0;

    private static view: DataView | undefined;

    // A macrotask, not a microtask: microtasks would run before the DOM handler's task ends
    // and re-create the per-event entry into C#. Same priority as the dispatcher signal, input is user-blocking.
    private static readonly postWake: Post = createPost("user-blocking", () => InputQueue.onWake());

    public static get isAttached(): boolean {
        return this.controlPtr !== 0;
    }

    /**
     * Called once by C# with the address of the control block.
     */
    public static attach(controlPtr: number, threadingEnabled: boolean): void {
        const runtime = globalThis.getDotnetRuntime(0);
        if (!runtime) {
            throw new Error("Unable to access .NET runtime");
        }

        const i32 = runtime.localHeapViewI32();
        const abiVersion = i32[(controlPtr + OffsetAbiVersion) >> 2];
        if (abiVersion !== AbiVersion) {
            throw new Error(`Input queue ABI mismatch: C# ${abiVersion}, JS ${AbiVersion}`);
        }

        this.runtime = runtime;
        this.controlPtr = controlPtr;
        this.threadingEnabled = threadingEnabled;
        this.segmentSize = i32[(controlPtr + OffsetSegmentSize) >> 2];
        // C# hands over an empty queue, so the tail is at the start of the first segment.
        this.tail = Atomics.load(i32, (controlPtr + OffsetTail) >> 2);
        this.segment = this.tail - SegmentHeaderSize;
        this.limit = this.tail + this.segmentSize;
    }

    public static postPointer(type: InputRecordType, topLevelId: number, args: PointerEvent): void {
        if (!this.isAttached) {
            return;
        }

        let size = PointerRecordSize;
        let coalesced: PointerEvent[] | undefined;
        let count = 0;
        if (type === InputRecordType.PointerMove) {
            size = CoalescedPointsOffset;
            if (typeof args.getCoalescedEvents === "function") {
                coalesced = args.getCoalescedEvents();
                // The last coalesced event is the event itself; it is the primary point.
                count = Math.max(0, Math.min(coalesced.length - 1, MaxCoalescedPoints));
                size += count * CoalescedPointSize;
            }
        }

        const p = this.reserve(size);
        const view = this.getView();
        this.writeHeader(view, p, type, size, topLevelId);

        // pointer body, offsets relative to the end of the header
        const b = p + HeaderSize;
        view.setFloat64(b + 0, args.timeStamp, true);
        view.setFloat64(b + 8, args.pointerId, true);
        view.setUint8(b + 16, InputQueue.getPointerType(args));
        view.setInt32(b + 20, args.button, true);
        view.setInt32(b + 24, InputQueue.getModifiers(args), true);
        InputQueue.writePoint(view, b + 32, args);

        if (type === InputRecordType.PointerMove) {
            view.setInt32(p + PointerRecordSize, count, true);
            if (coalesced) {
                // Keep the latest points when there are more than fit.
                const first = coalesced.length - 1 - count;
                for (let i = 0; i < count; i++) {
                    InputQueue.writePoint(view, p + CoalescedPointsOffset + i * CoalescedPointSize, coalesced[first + i]);
                }
            }
        }

        this.commit(p + size);

        // Presses are dispatched inside the DOM handler: actions that need a user gesture, like focusing
        // the text input to bring up the iOS on-screen keyboard, must run before the handler returns.
        if (type === InputRecordType.PointerDown || type === InputRecordType.PointerUp) {
            this.flushSync();
        }
    }

    public static postWheel(topLevelId: number, args: WheelEvent): void {
        if (!this.isAttached) {
            return;
        }

        const p = this.reserve(WheelRecordSize);
        const view = this.getView();
        this.writeHeader(view, p, InputRecordType.Wheel, WheelRecordSize, topLevelId);
        const b = p + HeaderSize;
        view.setFloat64(b + 0, args.timeStamp, true);
        view.setFloat64(b + 8, args.offsetX, true);
        view.setFloat64(b + 16, args.offsetY, true);
        view.setFloat64(b + 24, args.deltaX, true);
        view.setFloat64(b + 32, args.deltaY, true);
        view.setInt32(b + 40, args.deltaMode, true);
        view.setInt32(b + 44, InputQueue.getModifiers(args), true);

        this.commit(p + WheelRecordSize);
    }

    public static postKey(type: InputRecordType, topLevelId: number, args: KeyboardEvent): boolean {
        if (!this.isAttached) {
            return false;
        }

        const key = args.key ?? "";
        const code = args.code ?? "";
        const size = InputQueue.alignUp(KeyRecordHeaderSize + (key.length + code.length) * 2);

        const p = this.reserve(size);
        const view = this.getView();
        this.writeHeader(view, p, type, size, topLevelId);
        const b = p + HeaderSize;
        view.setFloat64(b + 0, args.timeStamp, true);
        view.setInt32(b + 8, InputQueue.getModifiers(args), true);
        view.setInt32(b + 12, key.length, true);
        view.setInt32(b + 16, code.length, true);
        let offset = p + KeyRecordHeaderSize;
        for (let i = 0; i < key.length; i++, offset += 2) {
            view.setUint16(offset, key.charCodeAt(i), true);
        }
        for (let i = 0; i < code.length; i++, offset += 2) {
            view.setUint16(offset, code.charCodeAt(i), true);
        }

        this.commit(p + size);

        return this.flushSync();
    }

    /**
     * Makes sure everything queued so far is observed by Avalonia before the caller's own event.
     */
    public static flush(): void {
        if (!this.isAttached) {
            return;
        }

        if (this.threadingEnabled) {
            // Cannot call synchronously, an asynchronous wake is queued instead.
            if (!this.isEmpty()) {
                JsExports.InputHelper.OnInputWakeAsync();
            }
        } else {
            this.flushSync();
        }
    }

    private static isEmpty(): boolean {
        const i32 = this.heapI32();
        return Atomics.load(i32, (this.controlPtr + OffsetHead) >> 2) === this.tail;
    }

    /**
     * Returns the address to write a record of the given size at, starting a new segment if needed.
     * May grow WASM memory, so heap views must be fetched after it.
     */
    private static reserve(size: number): number {
        if (this.tail + size <= this.limit) {
            return this.tail;
        }

        const capacity = Math.max(this.segmentSize, size);
        const segment = this.getRuntime().Module._malloc(SegmentHeaderSize + capacity) as unknown as number;
        if (!segment) {
            throw new Error("Unable to allocate input queue segment");
        }

        const i32 = this.heapI32();
        i32[(segment + OffsetNext) >> 2] = 0;
        i32[(segment + OffsetEnd) >> 2] = 0;

        // Seal the current segment: end first, then next. The tail published by commit() comes last,
        // so C# only moves on once both are visible.
        Atomics.store(i32, (this.segment + OffsetEnd) >> 2, this.tail);
        Atomics.store(i32, (this.segment + OffsetNext) >> 2, segment);

        this.segment = segment;
        this.limit = segment + SegmentHeaderSize + capacity;
        return segment + SegmentHeaderSize;
    }

    private static commit(tail: number): void {
        this.tail = tail;
        const i32 = this.heapI32();
        Atomics.store(i32, (this.controlPtr + OffsetTail) >> 2, tail);

        if (Atomics.exchange(i32, (this.controlPtr + OffsetWakeRequested) >> 2, 1) === 0) {
            this.postWake();
        }
    }

    private static onWake(): void {
        if (this.threadingEnabled) {
            JsExports.InputHelper.OnInputWakeAsync();
        } else {
            JsExports.InputHelper.OnInputWake();
        }
    }

    private static flushSync(): boolean {
        if (this.threadingEnabled) {
            return false;
        }

        return JsExports.InputHelper.FlushInputSync() as boolean;
    }

    private static writeHeader(view: DataView, p: number, type: InputRecordType, size: number, topLevelId: number): void {
        view.setUint16(p + 0, type, true);
        view.setUint16(p + 2, 0, true);
        view.setUint32(p + 4, size, true);
        view.setInt32(p + 8, topLevelId, true);
        view.setInt32(p + 12, 0, true);
    }

    private static writePoint(view: DataView, p: number, args: PointerEvent): void {
        view.setFloat64(p + 0, args.offsetX, true);
        view.setFloat64(p + 8, args.offsetY, true);
        view.setFloat32(p + 16, args.pressure, true);
        view.setFloat32(p + 20, args.tiltX, true);
        view.setFloat32(p + 24, args.tiltY, true);
        view.setFloat32(p + 28, args.twist, true);
    }

    private static getPointerType(args: PointerEvent): PointerType {
        if (args.pointerType === "touch") {
            return PointerType.Touch;
        }
        if (args.pointerType === "pen") {
            return PointerType.Pen;
        }
        return PointerType.Mouse;
    }

    private static alignUp(size: number): number {
        return (size + 7) & ~7;
    }

    // Views are re-validated on every use: memory growth replaces the underlying buffer.
    private static getView(): DataView {
        const buffer = this.getRuntime().localHeapViewU8().buffer;
        if (this.view?.buffer !== buffer) {
            this.view = new DataView(buffer);
        }
        return this.view;
    }

    private static getRuntime(): RuntimeAPI {
        if (!this.runtime) {
            throw new Error("Input queue is not attached");
        }
        return this.runtime;
    }

    private static heapI32(): Int32Array {
        return this.getRuntime().localHeapViewI32();
    }

    public static getModifiers(args: KeyboardEvent | PointerEvent | WheelEvent | DragEvent): number {
        let modifiers = RawInputModifiers.None;

        if (args.ctrlKey) { modifiers |= RawInputModifiers.Control; }
        if (args.altKey) { modifiers |= RawInputModifiers.Alt; }
        if (args.shiftKey) { modifiers |= RawInputModifiers.Shift; }
        if (args.metaKey) { modifiers |= RawInputModifiers.Meta; }

        const pointerArgs = args as PointerEvent;
        const buttons = pointerArgs.buttons;
        if (buttons) {
            if (buttons & 1) { modifiers |= RawInputModifiers.LeftMouseButton; }
            if (buttons & 2) { modifiers |= (pointerArgs.pointerType === "pen" ? RawInputModifiers.PenBarrelButton : RawInputModifiers.RightMouseButton); }
            if (buttons & 4) { modifiers |= RawInputModifiers.MiddleMouseButton; }
            if (buttons & 8) { modifiers |= RawInputModifiers.XButton1MouseButton; }
            if (buttons & 16) { modifiers |= RawInputModifiers.XButton2MouseButton; }
            if (buttons & 32) { modifiers |= RawInputModifiers.PenEraser; }
        }

        return modifiers;
    }
}
