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
                return null;
            }
            // make current
            GL.makeContextCurrent(ctx);
            // read values
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
        SKHtmlCanvas.elements[elementId] = element;
        const view = new SKHtmlCanvas(useGL, element, callback);
        htmlCanvas.SKHtmlCanvas = view;
        return view;
    }
    static deinit(elementId) {
        if (!elementId)
            return;
        const element = SKHtmlCanvas.elements[elementId];
        SKHtmlCanvas.elements.delete(elementId);
        const htmlCanvas = element;
        if (!htmlCanvas || !htmlCanvas.SKHtmlCanvas)
            return;
        htmlCanvas.SKHtmlCanvas.deinit();
        htmlCanvas.SKHtmlCanvas = undefined;
    }
    static requestAnimationFrame(element, renderLoop, width, height) {
        const htmlCanvas = element;
        if (!htmlCanvas || !htmlCanvas.SKHtmlCanvas)
            return;
        htmlCanvas.SKHtmlCanvas.requestAnimationFrame(renderLoop, width, height);
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
    requestAnimationFrame(renderLoop, width, height) {
        // optionally update the render loop
        if (renderLoop !== undefined && this.renderLoopEnabled !== renderLoop)
            this.setEnableRenderLoop(renderLoop);
        // make sure the canvas is scaled correctly for the drawing
        if (width && height) {
            this.htmlCanvas.width = width;
            this.htmlCanvas.height = height;
        }
        // skip because we have a render loop
        if (this.renderLoopRequest !== 0)
            return;
        // add the draw to the next frame
        this.renderLoopRequest = window.requestAnimationFrame(() => {
            if (this.glInfo) {
                // make current
                GL.makeContextCurrent(this.glInfo.context);
            }
            this.renderFrameCallback.invokeMethod('Invoke');
            this.renderLoopRequest = 0;
            // we may want to draw the next frame
            if (this.renderLoopEnabled)
                this.requestAnimationFrame();
        });
    }
    setEnableRenderLoop(enable) {
        this.renderLoopEnabled = enable;
        // either start the new frame or cancel the existing one
        if (enable) {
            //console.info(`Enabling render loop with callback ${this.renderFrameCallback._id}...`);
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
        // make sure the canvas is scaled correctly for the drawing
        this.htmlCanvas.width = width;
        this.htmlCanvas.height = height;
        // set the canvas to be the bytes
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
            antialias: 1,
            premultipliedAlpha: 1,
            preserveDrawingBuffer: 0,
            preferLowPowerToHighPerformance: 0,
            failIfMajorPerformanceCaveat: 0,
            majorVersion: 2,
            minorVersion: 0,
            enableExtensionsByDefault: 1,
            explicitSwapControl: 0,
            renderViaOffscreenBackBuffer: 0,
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