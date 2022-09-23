var __async = (__this, __arguments, generator) => {
  return new Promise((resolve, reject) => {
    var fulfilled = (value) => {
      try {
        step(generator.next(value));
      } catch (e) {
        reject(e);
      }
    };
    var rejected = (value) => {
      try {
        step(generator.throw(value));
      } catch (e) {
        reject(e);
      }
    };
    var step = (x) => x.done ? resolve(x.value) : Promise.resolve(x.value).then(fulfilled, rejected);
    step((generator = generator.apply(__this, __arguments)).next());
  });
};

// modules/avalonia/canvas.ts
var Canvas = class {
  constructor(useGL, element) {
    this.renderLoopEnabled = false;
    this.renderLoopRequest = 0;
    if (useGL) {
      const ctx = Canvas.createWebGLContext(element);
      if (!ctx) {
        console.error(`Failed to create WebGL context: err ${ctx}`);
        return;
      }
      var GL = globalThis.AvaloniaGL;
      GL.makeContextCurrent(ctx);
      var GLctx = GL.currentContext.GLctx;
      const fbo = GLctx.getParameter(GLctx.FRAMEBUFFER_BINDING);
      this.glInfo = {
        context: ctx,
        fboId: fbo ? fbo.id : 0,
        stencil: GLctx.getParameter(GLctx.STENCIL_BITS),
        sample: 0,
        depth: GLctx.getParameter(GLctx.DEPTH_BITS)
      };
    }
  }
  static createCanvas(element) {
    var canvas = document.createElement("canvas");
    element.appendChild(canvas);
    return canvas;
  }
  static initGL(element, elementId) {
    console.log("inside initGL");
    var view = Canvas.init(true, element, elementId);
    if (!view || !view.glInfo)
      return null;
    return view.glInfo;
  }
  static init(useGL, element, elementId) {
    var htmlCanvas = element;
    if (!htmlCanvas) {
      console.error(`No canvas element was provided.`);
      return null;
    }
    if (!Canvas.elements)
      Canvas.elements = /* @__PURE__ */ new Map();
    Canvas.elements.set(elementId, element);
    const view = new Canvas(useGL, element);
    htmlCanvas.SKHtmlCanvas = view;
    return view;
  }
  static Foo(canvas) {
    const ctx = canvas.getContext("2d");
    ctx.fillStyle = "#FF0000";
    ctx.fillRect(0, 0, 150, 75);
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
      renderViaOffscreenBackBuffer: 1
    };
    var GL = globalThis.AvaloniaGL;
    let ctx = GL.createContext(htmlCanvas, contextAttributes);
    if (!ctx && contextAttributes.majorVersion > 1) {
      console.warn("Falling back to WebGL 1.0");
      contextAttributes.majorVersion = 1;
      contextAttributes.minorVersion = 0;
      ctx = GL.createContext(htmlCanvas, contextAttributes);
    }
    return ctx;
  }
};

// modules/avalonia/runtime.ts
var AvaloniaRuntime = class {
  constructor(dotnetAssembly, api) {
    this.dotnetAssembly = dotnetAssembly;
    api.setModuleImports("avalonia.ts", {
      Canvas
    });
  }
};

// modules/avalonia.ts
function createAvaloniaRuntime(api) {
  return __async(this, null, function* () {
    const dotnetAssembly = yield api.getAssemblyExports("Avalonia.Web.dll");
    return new AvaloniaRuntime(dotnetAssembly, api);
  });
}
export {
  createAvaloniaRuntime
};
//# sourceMappingURL=avalonia.js.map
