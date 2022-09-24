declare let Module: EmscriptenModule;

type SKGLViewInfo = {
    context: WebGLRenderingContext | WebGL2RenderingContext | undefined;
    fboId: number;
    stencil: number;
    sample: number;
    depth: number;
}

type CanvasElement = {
    Canvas: Canvas | undefined
} & HTMLCanvasElement

export class Canvas {
    static elements: Map<string, HTMLCanvasElement>;

    htmlCanvas: HTMLCanvasElement;
    glInfo?: SKGLViewInfo;
    renderFrameCallback: () => void;
    renderLoopEnabled: boolean = false;
    renderLoopRequest: number = 0;
    newWidth?: number;
    newHeight?: number;

    public static createCanvas(element: HTMLDivElement): HTMLCanvasElement {
        var canvas = document.createElement("canvas");

        element.appendChild(canvas);
        canvas.classList.add('avalonia-canvas');

        return canvas;
    }

    public static initGL(element: HTMLCanvasElement, elementId: string, renderFrameCallback: () => void): SKGLViewInfo | null {
        console.log("inside initGL");
        var view = Canvas.init(true, element, elementId, renderFrameCallback);
        if (!view || !view.glInfo)
            return null;

        return view.glInfo;
    }

    static init(useGL: boolean, element: HTMLCanvasElement, elementId: string, renderFrameCallback: () => void): Canvas | null {
        var htmlCanvas = element as CanvasElement;
        if (!htmlCanvas) {
            console.error(`No canvas element was provided.`);
            return null;
        }

        if (!Canvas.elements)
            Canvas.elements = new Map<string, HTMLCanvasElement>();
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
                console.error(`Failed to create WebGL context: err ${ctx}`);
                return;
            }

            var GL = (globalThis as any).AvaloniaGL;

            // make current
            GL.makeContextCurrent(ctx);

            var GLctx = GL.currentContext.GLctx as WebGLRenderingContext;

            // read values
            const fbo = GLctx.getParameter(GLctx.FRAMEBUFFER_BINDING);

            this.glInfo = {
                context: ctx,
                fboId: fbo ? fbo.id : 0,
                stencil: GLctx.getParameter(GLctx.STENCIL_BITS),
                sample: 0, // TODO: GLctx.getParameter(GLctx.SAMPLES)
                depth: GLctx.getParameter(GLctx.DEPTH_BITS),
            };
        }
    }

    public setEnableRenderLoop(enable: boolean) {
        this.renderLoopEnabled = enable;

        // either start the new frame or cancel the existing one
        if (enable) {
            //console.info(`Enabling render loop with callback ${this.renderFrameCallback._id}...`);
            this.requestAnimationFrame();
        } else if (this.renderLoopRequest !== 0) {
            window.cancelAnimationFrame(this.renderLoopRequest);
            this.renderLoopRequest = 0;
        }
    }

    public requestAnimationFrame(renderLoop?: boolean) {
        // optionally update the render loop
        if (renderLoop !== undefined && this.renderLoopEnabled !== renderLoop)
            this.setEnableRenderLoop(renderLoop);

        // skip because we have a render loop
        if (this.renderLoopRequest !== 0)
            return;

        // add the draw to the next frame
        this.renderLoopRequest = window.requestAnimationFrame(() => {
            if (this.glInfo) {
                var GL = (globalThis as any).AvaloniaGL;
                // make current
                GL.makeContextCurrent(this.glInfo.context);
            }

            if (this.htmlCanvas.width != this.newWidth) {
                this.htmlCanvas.width = this.newWidth || 0;
            }

            if (this.htmlCanvas.height != this.newHeight) {
                this.htmlCanvas.height = this.newHeight || 0;
            }

            this.renderFrameCallback();
            this.renderLoopRequest = 0;

            // we may want to draw the next frame
            if (this.renderLoopEnabled)
                this.requestAnimationFrame();
        });
    }

    public setCanvasSize(width: number, height: number) {
        this.newWidth = width;
        this.newHeight = height;

        if (this.htmlCanvas.width != this.newWidth) {
            this.htmlCanvas.width = this.newWidth;
        }

        if (this.htmlCanvas.height != this.newHeight) {
            this.htmlCanvas.height = this.newHeight;
        }

        if (this.glInfo) {
            var GL = (globalThis as any).AvaloniaGL;
            // make current
            GL.makeContextCurrent(this.glInfo.context);
        }
    }

    public static setCanvasSize(element: HTMLCanvasElement, width: number, height: number) {
        const htmlCanvas = element as CanvasElement;
        if (!htmlCanvas || !htmlCanvas.Canvas)
            return;

        htmlCanvas.Canvas.setCanvasSize(width, height);
    }

    public static requestAnimationFrame(element: HTMLCanvasElement, renderLoop?: boolean) {
        const htmlCanvas = element as CanvasElement;
        if (!htmlCanvas || !htmlCanvas.Canvas)
            return;

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
            renderViaOffscreenBackBuffer: 1,
        };

        var GL = (globalThis as any).AvaloniaGL;

        let ctx: WebGLRenderingContext = GL.createContext(htmlCanvas, contextAttributes);

        if (!ctx && contextAttributes.majorVersion > 1) {
            console.warn('Falling back to WebGL 1.0');
            contextAttributes.majorVersion = 1;
            contextAttributes.minorVersion = 0;
            ctx = GL.createContext(htmlCanvas, contextAttributes);
        }

        return ctx;
    }
}

type SizeWatcherElement = {
    SizeWatcher: SizeWatcherInstance;
} & HTMLElement

type SizeWatcherInstance = {
    callback: (width: number, height: number) => void;
}

export class SizeWatcher {
    static observer: ResizeObserver;
    static elements: Map<string, HTMLElement>;

    public static observe(element: HTMLElement, elementId: string, callback: (width: number, height: number) => void) {
        if (!element || !callback)
            return;

        //console.info(`Adding size watcher observation with callback ${callback._id}...`);

        SizeWatcher.init();

        const watcherElement = element as SizeWatcherElement;
        watcherElement.SizeWatcher = {
            callback: callback
        };

        SizeWatcher.elements.set(elementId, element);
        SizeWatcher.observer.observe(element);

        SizeWatcher.invoke(element);
    }

    public static unobserve(elementId: string) {
        if (!elementId || !SizeWatcher.observer)
            return;

        //console.info('Removing size watcher observation...');

        const element = SizeWatcher.elements.get(elementId)!;

        SizeWatcher.elements.delete(elementId);
        SizeWatcher.observer.unobserve(element);
    }

    static init() {
        if (SizeWatcher.observer)
            return;

        //console.info('Starting size watcher...');

        SizeWatcher.elements = new Map<string, HTMLElement>();
        SizeWatcher.observer = new ResizeObserver((entries) => {
            for (let entry of entries) {
                SizeWatcher.invoke(entry.target);
            }
        });
    }

    static invoke(element: Element) {
        const watcherElement = element as SizeWatcherElement;
        const instance = watcherElement.SizeWatcher;

        if (!instance || !instance.callback)
            return;

        return instance.callback(element.clientWidth, element.clientHeight);
    }
}

export class DpiWatcher {
    static lastDpi: number;
    static timerId: number;
    static callback: (old: number, newdpi: number) => void;

    public static getDpi() {
        return window.devicePixelRatio;
    }

    public static start(callback: (old: number, newdpi: number) => void) : number {
        //console.info(`Starting DPI watcher with callback ${callback._id}...`);

        DpiWatcher.lastDpi = window.devicePixelRatio;
        DpiWatcher.timerId = window.setInterval(DpiWatcher.update, 1000);
        DpiWatcher.callback = callback;

        return DpiWatcher.lastDpi;
    }

    public static stop() {
        //console.info(`Stopping DPI watcher with callback ${DpiWatcher.callback._id}...`);

        window.clearInterval(DpiWatcher.timerId);

        //DpiWatcher.callback = undefined;
    }

    static update() {
        if (!DpiWatcher.callback)
            return;

        const currentDpi = window.devicePixelRatio;
        const lastDpi = DpiWatcher.lastDpi;
        DpiWatcher.lastDpi = currentDpi;

        if (Math.abs(lastDpi - currentDpi) > 0.001) {
            DpiWatcher.callback(lastDpi, currentDpi);
        }
    }
}
