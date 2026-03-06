import { RuntimeAPI } from "../../../types/dotnet";
import { WebRenderTarget } from "./webRenderTarget";

export class SoftwareRenderTarget extends WebRenderTarget {
    private readonly runtime: RuntimeAPI | undefined;
    private readonly context: CanvasRenderingContext2D | OffscreenCanvasRenderingContext2D;
    constructor(canvas: HTMLCanvasElement | OffscreenCanvas) {
        const context = canvas.getContext("2d", {
            alpha: true
        });
        if (!context) {
            throw new Error("HTMLCanvasElement.getContext(2d) returned null.");
        }

        super(canvas, "software");
        this.context = context;

        this.runtime = globalThis.getDotnetRuntime(0);
    }

    public putPixelData(pointer: number, length: number, width: number, height: number): void {
        const heap8 = this.runtime?.localHeapViewU8();

        let clampedBuffer: Uint8ClampedArray;
        if (heap8?.buffer) {
            clampedBuffer = new Uint8ClampedArray(heap8.buffer, pointer, length);

            // Need to make a copy if using MT, ImageData can't consume shared arrays
            if (this.canvas instanceof OffscreenCanvas) {
                const dstArrayBuffer = new ArrayBuffer(clampedBuffer.byteLength);
                const copy = new Uint8ClampedArray(dstArrayBuffer);
                copy.set(clampedBuffer);
                clampedBuffer = copy;
            }
        } else throw new Error("Unable to access .NET memory");

        const imageData = new ImageData(clampedBuffer, width, height);
        (this.context).putImageData(imageData, 0, 0);
    }

    public static staticPutPixelData(target: SoftwareRenderTarget, pointer: number, length: number, width: number, height: number): void {
        target.putPixelData(pointer, length, width, height);
    }
}
