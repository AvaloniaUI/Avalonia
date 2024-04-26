export type AvaloniaRenderingContext = RenderingContext;

export enum BrowserRenderingMode {
    Software2D = 1,
    WebGL1,
    WebGL2
}

export abstract class CanvasSurface {
    abstract destroy(): void;
    abstract ensureSize(): void;
    abstract onSizeChanged(sizeChangedCallback: (width: number, height: number, dpr: number) => void): void;
}
