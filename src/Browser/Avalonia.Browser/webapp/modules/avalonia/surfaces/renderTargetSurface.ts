import { HtmlCanvasSurfaceBase } from "./htmlSurfaceBase";
import { WebRenderTargetRegistry } from "./webRenderTarget";
import { BrowserRenderingMode } from "./surfaceBase";

export class RenderTargetSurface extends HtmlCanvasSurfaceBase {
    public targetId: number;
    constructor(canvas: HTMLCanvasElement, modes: BrowserRenderingMode[], threadId: number) {
        const targetId = WebRenderTargetRegistry.create(threadId, canvas, modes);
        super(canvas);
        this.targetId = targetId;
    }
}
