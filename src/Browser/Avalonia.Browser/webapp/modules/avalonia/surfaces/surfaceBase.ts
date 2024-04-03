export type AvaloniaRenderingContext = RenderingContext;

export enum BrowserRenderingMode {
    Software2D = 1,
    WebGL1,
    WebGL2
}

export abstract class CanvasSurface {
    constructor(
        public context: AvaloniaRenderingContext,
        public mode: BrowserRenderingMode) {
    }

    abstract destroy(): void;
    abstract requestAnimationFrame(renderFrameCallback: () => void): () => void;
    abstract onSizeChanged(sizeChangedCallback: (width: number, height: number, dpr: number) => void): void;
}
