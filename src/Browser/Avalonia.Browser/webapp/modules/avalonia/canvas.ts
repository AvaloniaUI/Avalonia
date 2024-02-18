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

            const GL = (globalThis as any).AvaloniaGL;

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

        const GL = (globalThis as any).AvaloniaGL;

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

type SizeAndDpiWatcherCallback = (width: number, height: number, devicePixelRatio: number) => void;
interface SizeAndDpiWatcherItem { element: HTMLElement; callback: SizeAndDpiWatcherCallback; width: number; height: number }

export class SizeAndDpiWatcher {
    private static lastDpi: number;
    private static items: SizeAndDpiWatcherItem[] = [];
    private static updateTimeoutId?: number;
    private static isRunning = false;
    private static lastMove = 0;

    public static observe(element: HTMLElement, callback: SizeAndDpiWatcherCallback): void {
        if (!element || !callback) {
            return;
        }

        SizeAndDpiWatcher.start();

        callback(element.clientWidth, element.clientHeight, window.devicePixelRatio);

        SizeAndDpiWatcher.items.push({ element, callback, width: element.clientWidth, height: element.clientHeight });
    }

    public static unobserve(element: HTMLElement): void {
        if (!element) {
            return;
        }

        SizeAndDpiWatcher.items = SizeAndDpiWatcher.items.filter(x => x.element !== element);
    }

    public static start(): void {
        if (SizeAndDpiWatcher.isRunning) {
            return;
        }
        SizeAndDpiWatcher.isRunning = true;

        SizeAndDpiWatcher.lastDpi = window.devicePixelRatio;
        window.setInterval(SizeAndDpiWatcher.update, 500);
        window.addEventListener("resize", SizeAndDpiWatcher.update);
    }

    private static update() {
        const currentDpi = window.devicePixelRatio;

        if (Math.abs(SizeAndDpiWatcher.lastDpi - currentDpi) > 0.001) {
            if (SizeAndDpiWatcher.updateTimeoutId !== undefined) {
                clearTimeout(SizeAndDpiWatcher.updateTimeoutId);
                SizeAndDpiWatcher.updateTimeoutId = undefined;
            }

            SizeAndDpiWatcher.updateDpi(currentDpi);
        } else {
            if (SizeAndDpiWatcher.updateTimeoutId !== undefined) {
                clearTimeout(SizeAndDpiWatcher.updateTimeoutId);
            }

            if (Date.now() - SizeAndDpiWatcher.lastMove > 33) {
                SizeAndDpiWatcher.updateSize();
            }

            SizeAndDpiWatcher.updateTimeoutId = setTimeout(SizeAndDpiWatcher.updateSize, 100);
        }
    }

    private static updateSize() {
        SizeAndDpiWatcher.lastMove = Date.now();
        SizeAndDpiWatcher.updateTimeoutId = undefined;
        const currentDpi = window.devicePixelRatio;

        if (Math.abs(SizeAndDpiWatcher.lastDpi - currentDpi) > 0.001) {
            SizeAndDpiWatcher.updateDpi(currentDpi);
        } else {
            SizeAndDpiWatcher.items.forEach(item => {
                const newWidth = item.element.clientWidth;
                const newHeight = item.element.clientHeight;
                if (item.width !== newWidth || item.height !== newHeight) {
                    item.width = newWidth;
                    item.height = newHeight;
                    item.callback(item.width, item.height, SizeAndDpiWatcher.lastDpi);
                }
            });
        }
    }

    private static updateDpi(dpi: number): void {
        SizeAndDpiWatcher.items.forEach(item => {
            item.width = item.element.clientWidth;
            item.height = item.element.clientHeight;
            item.callback(item.width, item.height, dpi);
        });
        SizeAndDpiWatcher.lastDpi = dpi;
    }
}
