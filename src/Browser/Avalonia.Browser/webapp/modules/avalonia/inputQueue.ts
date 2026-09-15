import { JsExports } from "./jsExports";
import { RuntimeAPI } from "../../types/dotnet";

/*
 * Producer side of the browser input ring buffer.
 *
 * The ring lives in WASM linear memory, created by the C# code.
 * JS appends encoded records and owns the producer index and
 * a spill list that is used only when the ring is full.
 *
 * Layouts below must match BrowserInputQueue.cs.
 *
 * Control block (all i32):
 *   0 abiVersion  4 capacity  8 head (C#)  12 tail (JS)  16 flags (bit 0: overflow list non-empty)
 * Record header:
 *   0 type u16  2 reserved u16  4 size u32  8 topLevelId i32  12 reserved i32
 * Records are 8-byte aligned and never split across the end of the data area; a Padding record
 * fills the remainder instead.
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
    Padding = 0,
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

const AbiVersion = 1;
const ControlBlockSize = 32;
const OffsetAbiVersion = 0;
const OffsetHead = 8;
const OffsetTail = 12;
const OffsetFlags = 16;
const FlagOverflow = 1;

const HeaderSize = 16;
const PointerRecordSize = HeaderSize + 64;
const CoalescedPointsOffset = PointerRecordSize + 8;
const CoalescedPointSize = 32;
const MaxCoalescedPoints = 256;
const WheelRecordSize = HeaderSize + 48;
const KeyRecordHeaderSize = HeaderSize + 24;

export class InputQueue {
    private static runtime: RuntimeAPI | undefined;
    private static basePtr = 0;
    private static dataPtr = 0;
    private static capacity = 0;
    private static tail = 0;
    private static threadingEnabled = false;

    private static scratch = new ArrayBuffer(4096);
    private static scratchView = new DataView(InputQueue.scratch);
    private static scratchBytes = new Uint8Array(InputQueue.scratch);

    private static overflow: Uint8Array[] = [];
    private static overflowStart = 0;

    private static wakeChannel: MessageChannel | undefined;

    public static get isAttached(): boolean {
        return this.basePtr !== 0;
    }

    /**
     * Called once by C# with the address of the control block.
     */
    public static attach(controlBlockPtr: number, threadingEnabled: boolean): void {
        const runtime = globalThis.getDotnetRuntime(0);
        if (!runtime) {
            throw new Error("Unable to access .NET runtime");
        }

        const i32 = runtime.localHeapViewI32();
        const abiVersion = i32[(controlBlockPtr + OffsetAbiVersion) >> 2];
        if (abiVersion !== AbiVersion) {
            throw new Error(`Input queue ABI mismatch: C# ${abiVersion}, JS ${AbiVersion}`);
        }

        this.runtime = runtime;
        this.basePtr = controlBlockPtr;
        this.dataPtr = controlBlockPtr + ControlBlockSize;
        this.capacity = i32[(controlBlockPtr + 4) >> 2];
        this.tail = Atomics.load(i32, (controlBlockPtr + OffsetTail) >> 2);
        this.threadingEnabled = threadingEnabled;
    }

    public static postPointer(type: InputRecordType, topLevelId: number, args: PointerEvent): void {
        if (!this.isAttached) {
            return;
        }

        let size = PointerRecordSize;
        let coalesced: PointerEvent[] | undefined;
        if (type === InputRecordType.PointerMove) {
            size = CoalescedPointsOffset;
            if (typeof args.getCoalescedEvents === "function") {
                coalesced = args.getCoalescedEvents();
                // The last coalesced event is the event itself; it is the primary point.
                let count = coalesced.length - 1;
                if (count > MaxCoalescedPoints) {
                    coalesced = coalesced.slice(coalesced.length - 1 - MaxCoalescedPoints);
                    count = MaxCoalescedPoints;
                }
                if (count > 0) {
                    size += count * CoalescedPointSize;
                } else {
                    coalesced = undefined;
                }
            }
        }

        const view = this.reserveScratch(size);
        this.writeHeader(view, type, size, topLevelId);

        // pointer body, offsets relative to HeaderSize
        view.setFloat64(HeaderSize + 0, args.timeStamp, true);
        view.setFloat64(HeaderSize + 8, args.pointerId, true);
        view.setUint8(HeaderSize + 16, InputQueue.getPointerType(args));
        view.setInt32(HeaderSize + 20, args.button, true);
        view.setInt32(HeaderSize + 24, InputQueue.getModifiers(args), true);
        InputQueue.writePoint(view, HeaderSize + 32, args);

        if (type === InputRecordType.PointerMove) {
            const count = coalesced ? coalesced.length - 1 : 0;
            view.setInt32(PointerRecordSize, count, true);
            if (coalesced) {
                for (let i = 0; i < count; i++) {
                    InputQueue.writePoint(view, CoalescedPointsOffset + i * CoalescedPointSize, coalesced[i]);
                }
            }
        }

        this.post(size);
    }

    public static postWheel(topLevelId: number, args: WheelEvent): void {
        if (!this.isAttached) {
            return;
        }

        const view = this.reserveScratch(WheelRecordSize);
        this.writeHeader(view, InputRecordType.Wheel, WheelRecordSize, topLevelId);
        view.setFloat64(HeaderSize + 0, args.timeStamp, true);
        view.setFloat64(HeaderSize + 8, args.offsetX, true);
        view.setFloat64(HeaderSize + 16, args.offsetY, true);
        view.setFloat64(HeaderSize + 24, args.deltaX, true);
        view.setFloat64(HeaderSize + 32, args.deltaY, true);
        view.setInt32(HeaderSize + 40, args.deltaMode, true);
        view.setInt32(HeaderSize + 44, InputQueue.getModifiers(args), true);

        this.post(WheelRecordSize);
    }


    public static postKey(type: InputRecordType, topLevelId: number, args: KeyboardEvent): boolean {
        if (!this.isAttached) {
            return false;
        }

        const key = args.key ?? "";
        const code = args.code ?? "";
        const size = InputQueue.alignUp(KeyRecordHeaderSize + (key.length + code.length) * 2);

        const view = this.reserveScratch(size);
        this.writeHeader(view, type, size, topLevelId);
        view.setFloat64(HeaderSize + 0, args.timeStamp, true);
        view.setInt32(HeaderSize + 8, InputQueue.getModifiers(args), true);
        view.setInt32(HeaderSize + 12, key.length, true);
        view.setInt32(HeaderSize + 16, code.length, true);
        let offset = KeyRecordHeaderSize;
        for (let i = 0; i < key.length; i++, offset += 2) {
            view.setUint16(offset, key.charCodeAt(i), true);
        }
        for (let i = 0; i < code.length; i++, offset += 2) {
            view.setUint16(offset, code.charCodeAt(i), true);
        }

        this.post(size);

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

    /**
     * Called by C# after it has emptied the ring. Moves spilled records back in FIFO order.
     */
    public static spillOverflow(): number {
        let moved = 0;
        while (this.overflowStart < this.overflow.length) {
            const record = this.overflow[this.overflowStart];
            if (!this.tryWrite(record, record.byteLength)) {
                break;
            }
            this.overflowStart++;
            moved++;
        }

        if (this.overflowStart >= this.overflow.length) {
            this.overflow = [];
            this.overflowStart = 0;
            const i32 = this.heapI32();
            Atomics.and(i32, (this.basePtr + OffsetFlags) >> 2, ~FlagOverflow);
        }

        return moved;
    }

    private static post(size: number): void {
        const wasEmpty = this.isEmpty();

        // While the spill list is non-empty every new record must go behind it to keep order.
        if (this.overflow.length > 0 || !this.tryWrite(this.scratchBytes, size)) {
            this.spill(size);
        }

        if (wasEmpty) {
            this.scheduleWake();
        }
    }

    private static isEmpty(): boolean {
        const i32 = this.heapI32();
        return this.overflow.length === 0 &&
            Atomics.load(i32, (this.basePtr + OffsetHead) >> 2) === this.tail;
    }

    private static tryWrite(bytes: Uint8Array, size: number): boolean {
        const i32 = this.heapI32();
        const head = Atomics.load(i32, (this.basePtr + OffsetHead) >> 2);
        const tail = this.tail;
        const capacity = this.capacity;
        let offset: number;

        // head === tail means empty, so the producer must never fill the ring completely.
        if (tail >= head) {
            if (capacity - tail > size || (capacity - tail === size && head !== 0)) {
                offset = tail;
            } else if (size < head) {
                // Does not fit at the end: pad the remainder and wrap to the start.
                this.writePadding(tail, capacity - tail);
                offset = 0;
            } else {
                return false;
            }
        } else if (head - tail > size) {
            offset = tail;
        } else {
            return false;
        }

        this.copyIn(bytes, size, offset);

        let newTail = offset + size;
        if (newTail >= capacity) {
            newTail = 0;
        }
        this.tail = newTail;
        Atomics.store(i32, (this.basePtr + OffsetTail) >> 2, newTail);
        return true;
    }

    private static copyIn(bytes: Uint8Array, size: number, offset: number): void {
        const heap = this.heapU8();
        heap.set(size === bytes.byteLength ? bytes : bytes.subarray(0, size), this.dataPtr + offset);
    }

    private static spill(size: number): void {
        // The scratch buffer is reused, so the spilled record needs its own copy.
        this.overflow.push(this.scratchBytes.slice(0, size));
        const i32 = this.heapI32();
        Atomics.or(i32, (this.basePtr + OffsetFlags) >> 2, FlagOverflow);
    }

    private static readonly paddingHeader = new Uint8Array(8);

    private static writePadding(offset: number, size: number): void {
        const view = new DataView(this.paddingHeader.buffer);
        view.setUint16(0, InputRecordType.Padding, true);
        view.setUint16(2, 0, true);
        view.setUint32(4, size, true);
        this.copyIn(this.paddingHeader, 8, offset);
    }

    private static scheduleWake(): void {
        if (!this.wakeChannel) {
            this.wakeChannel = new MessageChannel();
            this.wakeChannel.port1.onmessage = () => InputQueue.onWake();
        }
        // A macrotask, not a microtask: microtasks would run before the DOM handler's task ends
        // and re-create the per-event entry into C#.
        this.wakeChannel.port2.postMessage(null);
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

    private static reserveScratch(size: number): DataView {
        if (this.scratch.byteLength < size) {
            let newSize = this.scratch.byteLength * 2;
            while (newSize < size) {
                newSize *= 2;
            }
            this.scratch = new ArrayBuffer(newSize);
            this.scratchView = new DataView(this.scratch);
            this.scratchBytes = new Uint8Array(this.scratch);
        }
        return this.scratchView;
    }

    private static writeHeader(view: DataView, type: InputRecordType, size: number, topLevelId: number): void {
        view.setUint16(0, type, true);
        view.setUint16(2, 0, true);
        view.setUint32(4, size, true);
        view.setInt32(8, topLevelId, true);
        view.setInt32(12, 0, true);
    }

    private static writePoint(view: DataView, offset: number, args: PointerEvent): void {
        view.setFloat64(offset + 0, args.offsetX, true);
        view.setFloat64(offset + 8, args.offsetY, true);
        view.setFloat32(offset + 16, args.pressure, true);
        view.setFloat32(offset + 20, args.tiltX, true);
        view.setFloat32(offset + 24, args.tiltY, true);
        view.setFloat32(offset + 28, args.twist, true);
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

    private static heapI32(): Int32Array {
        if (!this.runtime) {
            throw new Error("Input queue is not attached");
        }
        // Views are fetched per call: memory growth invalidates cached views.
        return this.runtime.localHeapViewI32();
    }

    private static heapU8(): Uint8Array {
        if (!this.runtime) {
            throw new Error("Input queue is not attached");
        }
        return this.runtime.localHeapViewU8();
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
