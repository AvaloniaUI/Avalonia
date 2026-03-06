import { ResizeHandler } from "./resizeHandler";
import { WebRenderTargetRegistry } from "./webRenderTargetRegistry";
import { AvaloniaDOM } from "../dom";
import { BrowserRenderingMode } from "./renderingMode";
import { JsExports } from "../jsExports";

export class CanvasSurface {
    public targetId: number;
    private sizeParams?: [number, number, number];

    constructor(public canvas: HTMLCanvasElement, modes: BrowserRenderingMode[], topLevelId: number, threadId: number) {
        this.targetId = WebRenderTargetRegistry.create(threadId, canvas, modes);
        ResizeHandler.observeSize(canvas, (width, height, dpr) => {
            this.sizeParams = [width, height, dpr];

            JsExports.CanvasHelper?.OnSizeChanged(topLevelId, width, height, dpr);
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
    }

    public static create(container: HTMLElement, modes: BrowserRenderingMode[], topLevelId: number, threadId: number): CanvasSurface {
        const canvas = AvaloniaDOM.createAvaloniaCanvas(container);
        AvaloniaDOM.attachCanvas(container, canvas);
        try {
            return new CanvasSurface(canvas, modes, topLevelId, threadId);
        } catch (ex) {
            AvaloniaDOM.detachCanvas(container, canvas);
            throw ex;
        }
    }

    public static destroy(surface: CanvasSurface) {
        surface.destroy();
    }
}
