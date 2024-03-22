interface SKGLViewInfo {
    context: WebGLRenderingContext | WebGL2RenderingContext | undefined;
    fboId: number;
    stencil: number;
    sample: number;
    depth: number;
}

type CanvasElement = {
    Canvas: Canvas | undefined;
} & HTMLCanvasElement;

function getGL(): any {
    const self = globalThis as any;
    const module = self.Module ?? self.getDotnetRuntime(0)?.Module;
    return module?.GL ?? self.AvaloniaGL ?? self.SkiaSharpGL;
}

export class Canvas {
    static elements: Map<string, HTMLCanvasElement>;

    htmlCanvas: HTMLCanvasElement;
    glInfo?: SKGLViewInfo;
    renderFrameCallback: () => void;
    renderLoopEnabled: boolean = false;
    renderLoopRequest: number = 0;
    newWidth?: number;
    newHeight?: number;

    public static initGL(element: HTMLCanvasElement, elementId: string, renderFrameCallback: () => void): SKGLViewInfo | null {
        const view = Canvas.init(true, element, elementId, renderFrameCallback);
        if (!view || !view.glInfo) {
            return null;
        }

        return view.glInfo;
    }

    static init(useGL: boolean, element: HTMLCanvasElement, elementId: string, renderFrameCallback: () => void): Canvas | null {
        const htmlCanvas = element as CanvasElement;
        if (!htmlCanvas) {
            console.error("No canvas element was provided.");
            return null;
        }

        if (!Canvas.elements) {
            Canvas.elements = new Map<string, HTMLCanvasElement>();
        }
        Canvas.elements.set(elementId, element);

        const view = new Canvas(useGL, element, renderFrameCallback);

        htmlCanvas.Canvas = view;

        return view;
    }

    public constructor(useGL: boolean, element: HTMLCanvasElement, renderFrameCallback: () => void) {
        this.htmlCanvas = element;
        this.renderFrameCallback = renderFrameCallback;

        if (useGL) {
            const ctx = Canvas.createWebGLContext(element);
            if (!ctx) {
                console.error("Failed to create WebGL context");
                return;
            }

            const GL = getGL();

            // make current
            GL.makeContextCurrent(ctx);

            const GLctx = GL.currentContext.GLctx as WebGLRenderingContext;

            // read values
            const fbo = GLctx.getParameter(GLctx.FRAMEBUFFER_BINDING);

            this.glInfo = {
                context: ctx,
                fboId: fbo ? fbo.id : 0,
                stencil: GLctx.getParameter(GLctx.STENCIL_BITS),
                sample: 0, // TODO: GLctx.getParameter(GLctx.SAMPLES)
                depth: GLctx.getParameter(GLctx.DEPTH_BITS)
            };
        }
    }

    public setEnableRenderLoop(enable: boolean): void {
        this.renderLoopEnabled = enable;

        // either start the new frame or cancel the existing one
        if (enable) {
            // console.info(`Enabling render loop with callback ${this.renderFrameCallback._id}...`);
            this.requestAnimationFrame();
        } else if (this.renderLoopRequest !== 0) {
            window.cancelAnimationFrame(this.renderLoopRequest);
            this.renderLoopRequest = 0;
        }
    }

    public requestAnimationFrame(renderLoop?: boolean): void {
        // optionally update the render loop
        if (renderLoop !== undefined && this.renderLoopEnabled !== renderLoop) {
            this.setEnableRenderLoop(renderLoop);
        }

        // skip because we have a render loop
        if (this.renderLoopRequest !== 0) {
            return;
        }

        // add the draw to the next frame
        this.renderLoopRequest = window.requestAnimationFrame(() => {
            if (this.htmlCanvas.width !== this.newWidth) {
                this.htmlCanvas.width = this.newWidth ?? 0;
            }

            if (this.htmlCanvas.height !== this.newHeight) {
                this.htmlCanvas.height = this.newHeight ?? 0;
            }

            this.renderFrameCallback();
            this.renderLoopRequest = 0;

            // we may want to draw the next frame
            if (this.renderLoopEnabled) {
                this.requestAnimationFrame();
            }
        });
    }

    public setCanvasSize(width: number, height: number): void {
        if (this.renderLoopRequest !== 0) {
            window.cancelAnimationFrame(this.renderLoopRequest);
            this.renderLoopRequest = 0;
        }

        this.newWidth = width;
        this.newHeight = height;

        if (this.htmlCanvas.width !== this.newWidth) {
            this.htmlCanvas.width = this.newWidth;
        }

        if (this.htmlCanvas.height !== this.newHeight) {
            this.htmlCanvas.height = this.newHeight;
        }

        this.requestAnimationFrame();
    }

    public static setCanvasSize(element: HTMLCanvasElement, width: number, height: number): void {
        const htmlCanvas = element as CanvasElement;
        if (!htmlCanvas || !htmlCanvas.Canvas) {
            return;
        }

        htmlCanvas.Canvas.setCanvasSize(width, height);
    }

    public static requestAnimationFrame(element: HTMLCanvasElement, renderLoop?: boolean): void {
        const htmlCanvas = element as CanvasElement;
        if (!htmlCanvas || !htmlCanvas.Canvas) {
            return;
        }

        htmlCanvas.Canvas.requestAnimationFrame(renderLoop);
    }

    static createWebGLContext(htmlCanvas: HTMLCanvasElement): WebGLRenderingContext | WebGL2RenderingContext {
        const contextAttributes = {
            alpha: 1,
            depth: 1,
            stencil: 8,
            antialias: 0,
            premultipliedAlpha: 1,
            preserveDrawingBuffer: 0,
            preferLowPowerToHighPerformance: 0,
            failIfMajorPerformanceCaveat: 0,
            majorVersion: 2,
            minorVersion: 0,
            enableExtensionsByDefault: 1,
            explicitSwapControl: 0,
            renderViaOffscreenBackBuffer: 1
        };

        const GL = getGL();

        let ctx: WebGLRenderingContext = GL.createContext(htmlCanvas, contextAttributes);

        if (!ctx && contextAttributes.majorVersion > 1) {
            console.warn("Falling back to WebGL 1.0");
            contextAttributes.majorVersion = 1;
            contextAttributes.minorVersion = 0;
            ctx = GL.createContext(htmlCanvas, contextAttributes);
        }

        return ctx;
    }
}

type ResizeHandlerCallback = (displayWidth: number, displayHeight: number, dpi: number) => void;

type ResizeObserverWithCallbacks = {
    callbacks: Map<Element, ResizeHandlerCallback>;
} & ResizeObserver;

export class ResizeHandler {
    private static resizeObserver?: ResizeObserverWithCallbacks;

    public static observeSize(element: HTMLElement, callback: ResizeHandlerCallback): (() => void) {
        if (!this.resizeObserver) {
            this.resizeObserver = new ResizeObserver(this.onResize) as ResizeObserverWithCallbacks;
            this.resizeObserver.callbacks = new Map<Element, ResizeHandlerCallback>();
        }

        this.resizeObserver.callbacks.set(element, callback);
        this.resizeObserver.observe(element, { box: "content-box" });

        return () => {
            this.resizeObserver?.callbacks.delete(element);
            this.resizeObserver?.unobserve(element);
        };
    }

    private static onResize(entries: ResizeObserverEntry[], observer: ResizeObserver) {
        for (const entry of entries) {
            const callback = (observer as ResizeObserverWithCallbacks).callbacks.get(entry.target);
            if (!callback) {
                continue;
            }

            const trueDpr = window.devicePixelRatio;
            let width;
            let height;
            let dpr = trueDpr;
            if (entry.devicePixelContentBoxSize) {
                // NOTE: Only this path gives the correct answer
                // The other paths are imperfect fallbacks
                // for browsers that don't provide anyway to do this
                width = entry.devicePixelContentBoxSize[0].inlineSize;
                height = entry.devicePixelContentBoxSize[0].blockSize;
                dpr = 1; // it's already in width and height
            } else if (entry.contentBoxSize) {
                if (entry.contentBoxSize[0]) {
                    width = entry.contentBoxSize[0].inlineSize;
                    height = entry.contentBoxSize[0].blockSize;
                } else {
                    width = (entry.contentBoxSize as any).inlineSize;
                    height = (entry.contentBoxSize as any).blockSize;
                }
            } else {
                width = entry.contentRect.width;
                height = entry.contentRect.height;
            }
            const displayWidth = Math.round(width * dpr);
            const displayHeight = Math.round(height * dpr);

            callback(displayWidth, displayHeight, trueDpr);
        }
    }
}
