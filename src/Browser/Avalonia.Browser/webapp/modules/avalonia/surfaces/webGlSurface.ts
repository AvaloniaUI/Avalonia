import { BrowserRenderingMode } from "./surfaceBase";
import { HtmlCanvasSurfaceBase } from "./htmlSurfaceBase";

function getGL(): any {
    const self = globalThis as any;
    const module = self.Module ?? self.getDotnetRuntime(0)?.Module;
    return module?.GL ?? self.AvaloniaGL ?? self.SkiaSharpGL;
}

export class WebGlSurface extends HtmlCanvasSurfaceBase {
    public contextHandle?: number;
    public fboId?: number;
    public stencil?: number;
    public sample?: number;
    public depth?: number;

    constructor(public canvas: HTMLCanvasElement, mode: BrowserRenderingMode.WebGL1 | BrowserRenderingMode.WebGL2) {
        // Skia only understands WebGL context wrapped in Emscripten.
        const gl = getGL();
        if (!gl) {
            throw new Error("Module.GL object wasn't initialized, WebGL can't be used.");
        }

        const modeStr = mode === BrowserRenderingMode.WebGL1 ? "webgl" : "webgl2";
        const attrs: WebGLContextAttributes | any =
        {
            alpha: true,
            depth: true,
            stencil: true,
            antialias: false,
            premultipliedAlpha: true,
            preserveDrawingBuffer: false,
            // only supported on older browsers, which is perfect as we want to fallback to 2d there.
            failIfMajorPerformanceCaveat: true,
            // attrs used by Emscripten:
            majorVersion: mode === BrowserRenderingMode.WebGL1 ? 1 : 2,
            minorVersion: 0,
            enableExtensionsByDefault: 1,
            explicitSwapControl: 0
        };
        const context = canvas.getContext(modeStr, attrs) as WebGLRenderingContext;
        if (!context) {
            throw new Error(`HTMLCanvasElement.getContext(${modeStr}) returned null.`);
        }

        const handle = gl.registerContext(context, attrs);
        gl.makeContextCurrent(handle);
        (context as any).gl_handle = handle;

        super(canvas, context, BrowserRenderingMode.Software2D);

        this.contextHandle = handle;
        this.fboId = context.getParameter(context.FRAMEBUFFER_BINDING)?.id ?? 0;
        this.stencil = context.getParameter(context.STENCIL_BITS);
        this.sample = context.getParameter(context.SAMPLES);
        this.depth = context.getParameter(context.DEPTH_BITS);
    }
}
