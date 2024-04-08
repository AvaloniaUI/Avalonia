import { BrowserRenderingMode } from "./surfaceBase";
import { HtmlCanvasSurfaceBase } from "./htmlSurfaceBase";
import { RuntimeAPI } from "../../../types/dotnet";

const sharedArrayBufferDefined = typeof SharedArrayBuffer !== "undefined";
function isSharedArrayBuffer(buffer: any): buffer is SharedArrayBuffer {
    // BEWARE: In some cases, `instanceof SharedArrayBuffer` returns false even though buffer is an SAB.
    // Patch adapted from https://github.com/emscripten-core/emscripten/pull/16994
    // See also https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Symbol/toStringTag
    return sharedArrayBufferDefined && buffer[Symbol.toStringTag] === "SharedArrayBuffer";
}

export class SoftwareSurface extends HtmlCanvasSurfaceBase {
    private readonly runtime: RuntimeAPI | undefined;

    constructor(public canvas: HTMLCanvasElement) {
        const context = canvas.getContext("2d", {
            alpha: true
        });
        if (!context) {
            throw new Error("HTMLCanvasElement.getContext(2d) returned null.");
        }
        super(canvas, context, BrowserRenderingMode.Software2D);

        this.runtime = globalThis.getDotnetRuntime(0);
    }

    public putPixelData(span: any /* IMemoryView */, width: number, height: number): void {
        this.ensureSize();

        const heap8 = this.runtime?.localHeapViewU8();

        let clampedBuffer: Uint8ClampedArray;
        if (span._pointer > 0 && span._length > 0 && heap8 && !isSharedArrayBuffer(heap8.buffer)) {
            // Attempt to use undocumented access to the HEAP8 directly
            // Note, SharedArrayBuffer cannot be used with ImageData (when WasmEnableThreads = true).
            clampedBuffer = new Uint8ClampedArray(heap8.buffer, span._pointer, span._length);
        } else {
            // Or fallback to the normal API that does multiple array copies.
            const copy = new Uint8Array(span.byteLength);
            span.copyTo(copy);
            clampedBuffer = new Uint8ClampedArray(copy.buffer);
        }

        const imageData = new ImageData(clampedBuffer, width, height);
        (this.context as CanvasRenderingContext2D).putImageData(imageData, 0, 0);
    }
}
