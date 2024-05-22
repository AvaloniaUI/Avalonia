import { ResizeHandler } from "./resizeHandler";
import { WebRenderTargetRegistry } from "./webRenderTargetRegistry";
import { AvaloniaDOM } from "../dom";
import { BrowserRenderingMode } from "./renderingMode";

export class CanvasSurface {
    public targetId: number;
    private sizeParams?: [number, number, number];
    private sizeChangedCallback?: (width: number, height: number, dpr: number) => void;

    constructor(public canvas: HTMLCanvasElement, modes: BrowserRenderingMode[], threadId: number) {
        this.targetId = WebRenderTargetRegistry.create(threadId, canvas, modes);
        // No need to ubsubscribe, canvas never leaves JS world, it should be GC'ed with all callbacks.
        ResizeHandler.observeSize(canvas, (width, height, dpr) => {
            this.sizeParams = [width, height, dpr];

            if (this.sizeChangedCallback) {
                this.sizeChangedCallback(width, height, dpr);
            }
        });
    }

    public get width() {
        if (this.sizeParams) { return this.sizeParams[0]; }
        return 1;
    }

    public get height() {
        if (this.sizeParams) { return this.sizeParams[1]; }
        return 1;
    }

    public get scaling() {
        if (this.sizeParams) { return this.sizeParams[2]; }
        return 1;
    }

    public destroy(): void {
        delete this.sizeChangedCallback;
    }

    public onSizeChanged(sizeChangedCallback: (width: number, height: number, dpr: number) => void) {
        if (this.sizeChangedCallback) { throw new Error("For simplicity, we don't support multiple size changed callbacks per surface, not needed yet."); }
        this.sizeChangedCallback = sizeChangedCallback;
        // if (this.sizeParams) { this.sizeChangedCallback(this.sizeParams[0], this.sizeParams[1], this.sizeParams[2]); }
    }

    public static create(container: HTMLElement, modes: BrowserRenderingMode[], threadId: number): CanvasSurface {
        const canvas = AvaloniaDOM.createAvaloniaCanvas(container);
        AvaloniaDOM.attachCanvas(container, canvas);
        try {
            return new CanvasSurface(canvas, modes, threadId);
        } catch (ex) {
            AvaloniaDOM.detachCanvas(container, canvas);
            throw ex;
        }
    }

    public static destroy(surface: CanvasSurface) {
        surface.destroy();
    }

    public static onSizeChanged(surface: CanvasSurface, sizeChangedCallback: (width: number, height: number, dpr: number) => void) {
        surface.onSizeChanged(sizeChangedCallback);
    }
}
