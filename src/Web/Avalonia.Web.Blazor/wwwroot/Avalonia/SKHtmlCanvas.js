export class SKHtmlCanvas {
    constructor(useGL, element, callback) {
        this.renderLoopEnabled = false;
        this.renderLoopRequest = 0;
        this.htmlCanvas = element;
        this.renderFrameCallback = callback;
        if (useGL) {
            const ctx = SKHtmlCanvas.createWebGLContext(this.htmlCanvas);
            if (!ctx) {
                console.error(`Failed to create WebGL context: err ${ctx}`);
                return;
            }
            GL.makeContextCurrent(ctx);
            const fbo = GLctx.getParameter(GLctx.FRAMEBUFFER_BINDING);
            this.glInfo = {
                context: ctx,
                fboId: fbo ? fbo.id : 0,
                stencil: GLctx.getParameter(GLctx.STENCIL_BITS),
                sample: 0,
                depth: GLctx.getParameter(GLctx.DEPTH_BITS),
            };
        }
    }
    static initGL(element, elementId, callback) {
        var view = SKHtmlCanvas.init(true, element, elementId, callback);
        if (!view || !view.glInfo)
            return null;
        return view.glInfo;
    }
    static initRaster(element, elementId, callback) {
        var view = SKHtmlCanvas.init(false, element, elementId, callback);
        if (!view)
            return false;
        return true;
    }
    static init(useGL, element, elementId, callback) {
        var htmlCanvas = element;
        if (!htmlCanvas) {
            console.error(`No canvas element was provided.`);
            return null;
        }
        if (!SKHtmlCanvas.elements)
            SKHtmlCanvas.elements = new Map();
        SKHtmlCanvas.elements.set(elementId, element);
        const view = new SKHtmlCanvas(useGL, element, callback);
        htmlCanvas.SKHtmlCanvas = view;
        return view;
    }
    static deinit(elementId) {
        if (!elementId)
            return;
        const element = SKHtmlCanvas.elements.get(elementId);
        SKHtmlCanvas.elements.delete(elementId);
        const htmlCanvas = element;
        if (!htmlCanvas || !htmlCanvas.SKHtmlCanvas)
            return;
        htmlCanvas.SKHtmlCanvas.deinit();
        htmlCanvas.SKHtmlCanvas = undefined;
    }
    static requestAnimationFrame(element, renderLoop) {
        const htmlCanvas = element;
        if (!htmlCanvas || !htmlCanvas.SKHtmlCanvas)
            return;
        htmlCanvas.SKHtmlCanvas.requestAnimationFrame(renderLoop);
    }
    static setCanvasSize(element, width, height) {
        const htmlCanvas = element;
        if (!htmlCanvas || !htmlCanvas.SKHtmlCanvas)
            return;
        htmlCanvas.SKHtmlCanvas.setCanvasSize(width, height);
    }
    static setEnableRenderLoop(element, enable) {
        const htmlCanvas = element;
        if (!htmlCanvas || !htmlCanvas.SKHtmlCanvas)
            return;
        htmlCanvas.SKHtmlCanvas.setEnableRenderLoop(enable);
    }
    static putImageData(element, pData, width, height) {
        const htmlCanvas = element;
        if (!htmlCanvas || !htmlCanvas.SKHtmlCanvas)
            return;
        htmlCanvas.SKHtmlCanvas.putImageData(pData, width, height);
    }
    deinit() {
        this.setEnableRenderLoop(false);
    }
    setCanvasSize(width, height) {
        this.newWidth = width;
        this.newHeight = height;
        if (this.htmlCanvas.width != this.newWidth) {
            this.htmlCanvas.width = this.newWidth;
        }
        if (this.htmlCanvas.height != this.newHeight) {
            this.htmlCanvas.height = this.newHeight;
        }
        if (this.glInfo) {
            GL.makeContextCurrent(this.glInfo.context);
        }
    }
    requestAnimationFrame(renderLoop) {
        if (renderLoop !== undefined && this.renderLoopEnabled !== renderLoop)
            this.setEnableRenderLoop(renderLoop);
        if (this.renderLoopRequest !== 0)
            return;
        this.renderLoopRequest = window.requestAnimationFrame(() => {
            if (this.glInfo) {
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
            if (this.renderLoopEnabled)
                this.requestAnimationFrame();
        });
    }
    setEnableRenderLoop(enable) {
        this.renderLoopEnabled = enable;
        if (enable) {
            this.requestAnimationFrame();
        }
        else if (this.renderLoopRequest !== 0) {
            window.cancelAnimationFrame(this.renderLoopRequest);
            this.renderLoopRequest = 0;
        }
    }
    putImageData(pData, width, height) {
        if (this.glInfo || !pData || width <= 0 || width <= 0)
            return false;
        var ctx = this.htmlCanvas.getContext('2d');
        if (!ctx) {
            console.error(`Failed to obtain 2D canvas context.`);
            return false;
        }
        this.htmlCanvas.width = width;
        this.htmlCanvas.height = height;
        var buffer = new Uint8ClampedArray(Module.HEAPU8.buffer, pData, width * height * 4);
        var imageData = new ImageData(buffer, width, height);
        ctx.putImageData(imageData, 0, 0);
        return true;
    }
    static createWebGLContext(htmlCanvas) {
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
        let ctx = GL.createContext(htmlCanvas, contextAttributes);
        if (!ctx && contextAttributes.majorVersion > 1) {
            console.warn('Falling back to WebGL 1.0');
            contextAttributes.majorVersion = 1;
            contextAttributes.minorVersion = 0;
            ctx = GL.createContext(htmlCanvas, contextAttributes);
        }
        return ctx;
    }
}
//# sourceMappingURL=SKHtmlCanvas.js.map