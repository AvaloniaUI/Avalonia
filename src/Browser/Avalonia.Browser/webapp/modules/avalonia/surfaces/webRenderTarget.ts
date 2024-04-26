export class WebRenderTarget {
    renderTargetType: string;
    constructor(protected canvas: HTMLCanvasElement | OffscreenCanvas, type: string) {
        this.renderTargetType = type;
    }

    static setSize(target: WebRenderTarget, w: number, h: number) {
        target.canvas.width = w;
        target.canvas.height = h;
    }
}
