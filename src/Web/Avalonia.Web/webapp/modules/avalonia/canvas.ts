declare let Module: EmscriptenModule;

type SKGLViewInfo = {
    context: WebGLRenderingContext | WebGL2RenderingContext | undefined;
    fboId: number;
    stencil: number;
    sample: number;
    depth: number;
}

type SKHtmlCanvasElement = {
    SKHtmlCanvas: Canvas | undefined
} & HTMLCanvasElement

export class Canvas {
    static elements: Map<string, HTMLCanvasElement>;

    //htmlCanvas: HTMLCanvasElement;
    glInfo?: SKGLViewInfo;
    //renderFrameCallback: DotNet.DotNetObject;
    renderLoopEnabled: boolean = false;
    renderLoopRequest: number = 0;
    newWidth?: number;
    newHeight?: number;

    public static initGL(element: HTMLCanvasElement, elementId: string): SKGLViewInfo | null {
        console.log("inside initGL");
        var view = Canvas.init(true, element, elementId);
        if (!view || !view.glInfo)
            return null;

        return view.glInfo;
    }

    static init(useGL: boolean, element: HTMLCanvasElement, elementId: string): Canvas | null {
        var htmlCanvas = element as SKHtmlCanvasElement;
        if (!htmlCanvas) {
            console.error(`No canvas element was provided.`);
            return null;
        }

        if (!Canvas.elements)
            Canvas.elements = new Map<string, HTMLCanvasElement>();
        Canvas.elements.set(elementId, element);

        const view = new Canvas(useGL, element);

        htmlCanvas.SKHtmlCanvas = view;

        return view;
    }


    public constructor(useGL: boolean, element: HTMLCanvasElement) {
        //this.htmlCanvas = element;
        //this.renderFrameCallback = callback;

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


    static Foo(canvas: HTMLCanvasElement) {
        const ctx = canvas.getContext("2d")!;
        ctx.fillStyle = "#FF0000";
        ctx.fillRect(0, 0, 150, 75);
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
