import { JsExports } from "./jsExports";
import { RuntimeAPI } from "../../types/dotnet";
import { SingleThreadedDispatcherHelper } from "./singleThreadedDispatcher";
import { MultiThreadedDispatcherHelper } from "./multiThreadedDispatcher";
import {
    ControlBlock,
    DomKeyModifiers,
    KeyRecord,
    PointerRecord,
    PointerType,
    PointRecord,
    RecordHeader,
    RecordType,
    SegmentHeader,
    WheelRecord
} from "./inputLayout.g";

/*
 * Producer side of the browser input queue.
 *
 * The queue is a chain of segments in WASM linear memory. JS appends records to the last segment; when a record
 * doesn't fit, it allocates a new segment with malloc and links it in. C# reads the chain in order and frees every
 * segment it has read past.
 *
 * The layout is defined by the structs in BrowserInputLayout.cs; inputLayout.g.ts is generated from them at build
 * time. Records copy DOM event fields as they are; C# maps them to Avalonia input.
 */

export type PointerRecordType =
    RecordType.PointerMove | RecordType.PointerDown | RecordType.PointerUp | RecordType.PointerCancel;
export type KeyRecordType = RecordType.KeyDown | RecordType.KeyUp;

// Typed views over the WASM heap. Generated offsets are aligned to their field type, so p >> 3 and p >> 2
// address f64 and 32-bit fields exactly.
interface Heap {
    buffer: ArrayBufferLike;
    f64: Float64Array;
    f32: Float32Array;
    i32: Int32Array;
    u16: Uint16Array;
}

const MaxCoalescedPoints = 256;

export class InputQueue {
    private static runtime: RuntimeAPI | undefined;
    private static controlPtr = 0;
    private static segmentSize = 0;
    private static threadingEnabled = false;

    // Producer state. tail is the published end of written records; limit is the end of the current segment.
    private static segment = 0;
    private static limit = 0;
    private static tail = 0;

    private static heap: Heap | undefined;

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
        this.runtime = runtime;
        this.threadingEnabled = threadingEnabled;
        this.segmentSize = i32[(controlPtr + ControlBlock.SegmentSize) >> 2];
        // C# hands over an empty queue, so the tail is at the start of the first segment.
        this.tail = Atomics.load(i32, (controlPtr + ControlBlock.Tail) >> 2);
        this.segment = this.tail - SegmentHeader.$size;
        this.limit = this.tail + this.segmentSize;
        this.controlPtr = controlPtr;
    }

    public static postPointer(type: PointerRecordType, topLevelId: number, args: PointerEvent): void {
        if (!this.isAttached) {
            return;
        }

        let coalesced: PointerEvent[] | undefined;
        let count = 0;
        if (type === RecordType.PointerMove && typeof args.getCoalescedEvents === "function") {
            coalesced = args.getCoalescedEvents();
            // The last coalesced event is the event itself; it is the primary point.
            count = Math.max(0, Math.min(coalesced.length - 1, MaxCoalescedPoints));
        }

        const size = InputQueue.alignUp(PointerRecord.$size + count * PointRecord.$size);
        const p = this.reserve(size);
        const h = this.getHeap();
        InputQueue.writeHeader(h, p + PointerRecord.Header, type, size);
        h.f64[(p + PointerRecord.TimeStamp) >> 3] = args.timeStamp;
        h.f64[(p + PointerRecord.PointerId) >> 3] = args.pointerId;
        InputQueue.writePoint(h, p + PointerRecord.Point, args);
        h.i32[(p + PointerRecord.TopLevelId) >> 2] = topLevelId;
        h.i32[(p + PointerRecord.PointerType) >> 2] = InputQueue.getPointerType(args);
        h.i32[(p + PointerRecord.Button) >> 2] = args.button;
        h.i32[(p + PointerRecord.Buttons) >> 2] = args.buttons;
        h.i32[(p + PointerRecord.KeyModifiers) >> 2] = InputQueue.getKeyModifiers(args);
        h.i32[(p + PointerRecord.CoalescedCount) >> 2] = count;

        if (coalesced) {
            // Keep the latest points when there are more than fit.
            const first = coalesced.length - 1 - count;
            for (let i = 0; i < count; i++) {
                InputQueue.writePoint(h, p + PointerRecord.$size + i * PointRecord.$size, coalesced[first + i]);
            }
        }

        this.commit(p + size);

        // Presses are dispatched inside the DOM handler: actions that need a user gesture, like focusing
        // the text input to bring up the iOS on-screen keyboard, must run before the handler returns.
        if (type === RecordType.PointerDown || type === RecordType.PointerUp) {
            this.flushSync();
        }
    }

    public static postWheel(topLevelId: number, args: WheelEvent): void {
        if (!this.isAttached) {
            return;
        }

        // Deltas are read before deltaMode: Firefox reports pixels unless a script checks deltaMode first.
        const deltaX = args.deltaX;
        const deltaY = args.deltaY;
        const deltaMode = args.deltaMode;
        // Only read for page mode, the only one that needs it: reading the client size may force a layout.
        const target = deltaMode === WheelEvent.DOM_DELTA_PAGE ? args.currentTarget as HTMLElement | null : null;

        const size = WheelRecord.$size;
        const p = this.reserve(size);
        const h = this.getHeap();
        InputQueue.writeHeader(h, p + WheelRecord.Header, RecordType.Wheel, size);
        h.f64[(p + WheelRecord.TimeStamp) >> 3] = args.timeStamp;
        h.f64[(p + WheelRecord.OffsetX) >> 3] = args.offsetX;
        h.f64[(p + WheelRecord.OffsetY) >> 3] = args.offsetY;
        h.f64[(p + WheelRecord.DeltaX) >> 3] = deltaX;
        h.f64[(p + WheelRecord.DeltaY) >> 3] = deltaY;
        h.i32[(p + WheelRecord.TopLevelId) >> 2] = topLevelId;
        h.i32[(p + WheelRecord.DeltaMode) >> 2] = deltaMode;
        h.i32[(p + WheelRecord.Buttons) >> 2] = args.buttons;
        h.i32[(p + WheelRecord.KeyModifiers) >> 2] = InputQueue.getKeyModifiers(args);
        h.i32[(p + WheelRecord.PageWidth) >> 2] = target?.clientWidth ?? 0;
        h.i32[(p + WheelRecord.PageHeight) >> 2] = target?.clientHeight ?? 0;

        this.commit(p + size);
    }

    public static postKey(type: KeyRecordType, topLevelId: number, args: KeyboardEvent): boolean {
        if (!this.isAttached) {
            return false;
        }

        const key = args.key ?? "";
        const code = args.code ?? "";
        const size = InputQueue.alignUp(KeyRecord.$size + (key.length + code.length) * 2);

        const p = this.reserve(size);
        const h = this.getHeap();
        InputQueue.writeHeader(h, p + KeyRecord.Header, type, size);
        h.f64[(p + KeyRecord.TimeStamp) >> 3] = args.timeStamp;
        h.i32[(p + KeyRecord.TopLevelId) >> 2] = topLevelId;
        h.i32[(p + KeyRecord.KeyModifiers) >> 2] = InputQueue.getKeyModifiers(args);
        h.i32[(p + KeyRecord.KeyLength) >> 2] = key.length;
        h.i32[(p + KeyRecord.CodeLength) >> 2] = code.length;
        let index = (p + KeyRecord.$size) >> 1;
        for (let i = 0; i < key.length; i++) {
            h.u16[index++] = key.charCodeAt(i);
        }
        for (let i = 0; i < code.length; i++) {
            h.u16[index++] = code.charCodeAt(i);
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

        // With threads nothing can be flushed synchronously; queued records have already signaled the dispatcher.
        if (!this.threadingEnabled) {
            this.flushSync();
        }
    }

    /**
     * Packs the modifier keys of a DOM event as BrowserInputLayout.DomKeyModifiers.
     */
    public static getKeyModifiers(args: KeyboardEvent | MouseEvent): number {
        let modifiers = 0;
        if (args.altKey) { modifiers |= DomKeyModifiers.Alt; }
        if (args.ctrlKey) { modifiers |= DomKeyModifiers.Control; }
        if (args.shiftKey) { modifiers |= DomKeyModifiers.Shift; }
        if (args.metaKey) { modifiers |= DomKeyModifiers.Meta; }
        return modifiers;
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
        const segment = this.getRuntime().Module._malloc(SegmentHeader.$size + capacity) as unknown as number;
        if (!segment) {
            throw new Error("Unable to allocate input queue segment");
        }

        const i32 = this.getHeap().i32;
        i32[(segment + SegmentHeader.Next) >> 2] = 0;
        i32[(segment + SegmentHeader.End) >> 2] = 0;

        // Seal the current segment: end first, then next. The tail published by commit() comes last,
        // so C# only moves on once both are visible.
        Atomics.store(i32, (this.segment + SegmentHeader.End) >> 2, this.tail);
        Atomics.store(i32, (this.segment + SegmentHeader.Next) >> 2, segment);

        this.segment = segment;
        this.limit = segment + SegmentHeader.$size + capacity;
        return segment + SegmentHeader.$size;
    }

    private static commit(tail: number): void {
        this.tail = tail;
        const i32 = this.getHeap().i32;
        Atomics.store(i32, (this.controlPtr + ControlBlock.Tail) >> 2, tail);

        if (Atomics.exchange(i32, (this.controlPtr + ControlBlock.WakeRequested) >> 2, 1) === 0) {
            if (this.threadingEnabled) {
                // Wakes the dispatcher thread directly; it drains the queue after waking up.
                MultiThreadedDispatcherHelper.signal();
            } else {
                // A macrotask, so a burst of DOM events enters C# once. The dispatcher drains the queue
                // before running its jobs on every signal.
                SingleThreadedDispatcherHelper.signal();
            }
        }
    }

    private static flushSync(): boolean {
        if (this.threadingEnabled) {
            return false;
        }

        return JsExports.InputHelper.FlushInputSync() as boolean;
    }

    private static writeHeader(h: Heap, p: number, type: RecordType, size: number): void {
        h.i32[(p + RecordHeader.Type) >> 2] = type;
        h.i32[(p + RecordHeader.Size) >> 2] = size;
    }

    private static writePoint(h: Heap, p: number, args: PointerEvent): void {
        h.f64[(p + PointRecord.OffsetX) >> 3] = args.offsetX;
        h.f64[(p + PointRecord.OffsetY) >> 3] = args.offsetY;
        h.f32[(p + PointRecord.Pressure) >> 2] = args.pressure;
        h.f32[(p + PointRecord.TiltX) >> 2] = args.tiltX;
        h.f32[(p + PointRecord.TiltY) >> 2] = args.tiltY;
        h.f32[(p + PointRecord.Twist) >> 2] = args.twist;
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
    private static getHeap(): Heap {
        const buffer = this.getRuntime().localHeapViewU8().buffer;
        if (this.heap?.buffer !== buffer) {
            this.heap = {
                buffer,
                f64: new Float64Array(buffer),
                f32: new Float32Array(buffer),
                i32: new Int32Array(buffer),
                u16: new Uint16Array(buffer)
            };
        }
        return this.heap;
    }

    private static getRuntime(): RuntimeAPI {
        if (!this.runtime) {
            throw new Error("Input queue is not attached");
        }
        return this.runtime;
    }
}
