import { ResizeHandler } from "./resizeHandler";
import { CanvasSurface, AvaloniaRenderingContext, BrowserRenderingMode } from "./surfaceBase";

export abstract class HtmlCanvasSurfaceBase extends CanvasSurface {
    private sizeParams?: [number, number, number];
    private sizeChangedCallback?: (width: number, height: number, dpr: number) => void;

    constructor(
        public canvas: HTMLCanvasElement,
        public context: AvaloniaRenderingContext,
        public mode: BrowserRenderingMode) {
        super(context, mode);

        // No need to ubsubsribe, canvas never leaves JS world, it should be GC'ed with all callbacks.
        ResizeHandler.observeSize(canvas, (width, height, dpr) => {
            this.sizeParams = [width, height, dpr];

            if (this.sizeChangedCallback) {
                this.sizeChangedCallback(width, height, dpr);
            }
        });
    }

    public destroy(): void {
        delete this.sizeChangedCallback;
    }

    public onSizeChanged(sizeChangedCallback: (width: number, height: number, dpr: number) => void) {
        if (this.sizeChangedCallback) { throw new Error("For simplicity, we don't support multiple size changed callbacks per surface, not needed yet."); }
        this.sizeChangedCallback = sizeChangedCallback;
    }

    public ensureSize() {
        if (this.sizeParams) {
            this.canvas.width = this.sizeParams[0];
            this.canvas.height = this.sizeParams[1];
            delete this.sizeParams;
        }
    }
}
