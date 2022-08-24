// aliases for emscripten
declare let GL: any;
declare let GLctx: WebGLRenderingContext;
declare let Module: EmscriptenModule;

// container for gl info
type SKGLViewInfo = {
    context: WebGLRenderingContext | WebGL2RenderingContext | undefined;
    fboId: number;
    stencil: number;
    sample: number;
    depth: number;
}

// alias for a potential skia html canvas
type SKHtmlCanvasElement = {
    SKHtmlCanvas: SKHtmlCanvas | undefined
} & HTMLCanvasElement

export class SKHtmlCanvas {
    static elements: Map<string, HTMLCanvasElement>;

    htmlCanvas: HTMLCanvasElement;
    glInfo?: SKGLViewInfo;
    renderFrameCallback: DotNet.DotNetObject;
    renderLoopEnabled: boolean = false;
    renderLoopRequest: number = 0;
    newWidth?: number;
    newHeight?: number;

    public static initGL(element: HTMLCanvasElement, elementId: string, callback: DotNet.DotNetObject): SKGLViewInfo | null {
        var view = SKHtmlCanvas.init(true, element, elementId, callback);
        if (!view || !view.glInfo)
            return null;

        return view.glInfo;
    }

    public static initRaster(element: HTMLCanvasElement, elementId: string, callback: DotNet.DotNetObject): boolean {
        var view = SKHtmlCanvas.init(false, element, elementId, callback);
        if (!view)
            return false;

        return true;
    }

    static init(useGL: boolean, element: HTMLCanvasElement, elementId: string, callback: DotNet.DotNetObject): SKHtmlCanvas | null {
        var htmlCanvas = element as SKHtmlCanvasElement;
        if (!htmlCanvas) {
            console.error(`No canvas element was provided.`);
            return null;
        }

        if (!SKHtmlCanvas.elements)
            SKHtmlCanvas.elements = new Map<string, HTMLCanvasElement>();
        SKHtmlCanvas.elements.set(elementId, element);

        const view = new SKHtmlCanvas(useGL, element, callback);

        htmlCanvas.SKHtmlCanvas = view;

        return view;
    }

    public static deinit(elementId: string) {
        if (!elementId)
            return;

        const element = SKHtmlCanvas.elements.get(elementId);
        SKHtmlCanvas.elements.delete(elementId);

        const htmlCanvas = element as SKHtmlCanvasElement;
        if (!htmlCanvas || !htmlCanvas.SKHtmlCanvas)
            return;

        htmlCanvas.SKHtmlCanvas.deinit();
        htmlCanvas.SKHtmlCanvas = undefined;
    }

    public static requestAnimationFrame(element: HTMLCanvasElement, renderLoop?: boolean) {
        const htmlCanvas = element as SKHtmlCanvasElement;
        if (!htmlCanvas || !htmlCanvas.SKHtmlCanvas)
            return;

        htmlCanvas.SKHtmlCanvas.requestAnimationFrame(renderLoop);
    }

    public static setCanvasSize(element: HTMLCanvasElement, width: number, height: number) {
        const htmlCanvas = element as SKHtmlCanvasElement;
        if (!htmlCanvas || !htmlCanvas.SKHtmlCanvas)
            return;

        htmlCanvas.SKHtmlCanvas.setCanvasSize(width, height);
    }

    public static setEnableRenderLoop(element: HTMLCanvasElement, enable: boolean) {
        const htmlCanvas = element as SKHtmlCanvasElement;
        if (!htmlCanvas || !htmlCanvas.SKHtmlCanvas)
            return;

        htmlCanvas.SKHtmlCanvas.setEnableRenderLoop(enable);
    }

    public static putImageData(element: HTMLCanvasElement, pData: number, width: number, height: number) {
        const htmlCanvas = element as SKHtmlCanvasElement;
        if (!htmlCanvas || !htmlCanvas.SKHtmlCanvas)
            return;

        htmlCanvas.SKHtmlCanvas.putImageData(pData, width, height);
    }

    public constructor(useGL: boolean, element: HTMLCanvasElement, callback: DotNet.DotNetObject) {
        this.htmlCanvas = element;
        this.renderFrameCallback = callback;

        if (useGL) {
            const ctx = SKHtmlCanvas.createWebGLContext(this.htmlCanvas);
            if (!ctx) {
                console.error(`Failed to create WebGL context: err ${ctx}`);
                return;
            }

            // make current
            GL.makeContextCurrent(ctx);

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

    public deinit() {
        this.setEnableRenderLoop(false);
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
            // make current
            GL.makeContextCurrent(this.glInfo.context);
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
                // make current
                GL.makeContextCurrent(this.glInfo.context);
            }

            if (this.htmlCanvas.width != this.newWidth) {
                this.htmlCanvas.width = this.newWidth || 0;
            }

            if (this.htmlCanvas.height != this.newHeight) {
                this.htmlCanvas.height = this.newHeight || 0;
            }

            this.renderFrameCallback.invokeMethod('Invoke');
            this.renderLoopRequest = 0;

            // we may want to draw the next frame
            if (this.renderLoopEnabled)
                this.requestAnimationFrame();
        });
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

    public putImageData(pData: number, width: number, height: number): boolean {
        if (this.glInfo || !pData || width <= 0 || width <= 0)
            return false;

        var ctx = this.htmlCanvas.getContext('2d');
        if (!ctx) {
            console.error(`Failed to obtain 2D canvas context.`);
            return false;
        }

        // make sure the canvas is scaled correctly for the drawing
        this.htmlCanvas.width = width;
        this.htmlCanvas.height = height;

        // set the canvas to be the bytes
        var buffer = new Uint8ClampedArray(Module.HEAPU8.buffer, pData, width * height * 4);
        var imageData = new ImageData(buffer, width, height);
        ctx.putImageData(imageData, 0, 0);

        return true;
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
