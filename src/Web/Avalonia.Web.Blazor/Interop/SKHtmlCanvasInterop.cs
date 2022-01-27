using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SkiaSharp;

namespace Avalonia.Web.Blazor.Interop
{
    internal class SKHtmlCanvasInterop : JSModuleInterop
    {
        private const string JsFilename = "./_content/Avalonia.Web.Blazor/SKHtmlCanvas.js";
        private const string InitGLSymbol = "SKHtmlCanvas.initGL";
        private const string InitRasterSymbol = "SKHtmlCanvas.initRaster";
        private const string DeinitSymbol = "SKHtmlCanvas.deinit";
        private const string RequestAnimationFrameSymbol = "SKHtmlCanvas.requestAnimationFrame";
        private const string SetCanvasSizeSymbol = "SKHtmlCanvas.setCanvasSize";
        private const string PutImageDataSymbol = "SKHtmlCanvas.putImageData";

        private readonly ElementReference htmlCanvas;
        private readonly string htmlElementId;
        private readonly ActionHelper callbackHelper;

        private DotNetObjectReference<ActionHelper>? callbackReference;

        public static async Task<SKHtmlCanvasInterop> ImportAsync(IJSRuntime js, ElementReference element, Action callback)
        {
            var interop = new SKHtmlCanvasInterop(js, element, callback);
            await interop.ImportAsync();
            return interop;
        }

        public SKHtmlCanvasInterop(IJSRuntime js, ElementReference element, Action renderFrameCallback)
            : base(js, JsFilename)
        {
            htmlCanvas = element;
            htmlElementId = element.Id;

            callbackHelper = new ActionHelper(renderFrameCallback);
        }

        protected override void OnDisposingModule() =>
            Deinit();

        public GLInfo InitGL()
        {
            if (callbackReference != null)
                throw new InvalidOperationException("Unable to initialize the same canvas more than once.");

            callbackReference = DotNetObjectReference.Create(callbackHelper);

            return Invoke<GLInfo>(InitGLSymbol, htmlCanvas, htmlElementId, callbackReference);
        }

        public bool InitRaster()
        {
            if (callbackReference != null)
                throw new InvalidOperationException("Unable to initialize the same canvas more than once.");

            callbackReference = DotNetObjectReference.Create(callbackHelper);

            return Invoke<bool>(InitRasterSymbol, htmlCanvas, htmlElementId, callbackReference);
        }

        public void Deinit()
        {
            if (callbackReference == null)
                return;

            Invoke(DeinitSymbol, htmlElementId);

            callbackReference?.Dispose();
        }

        public void RequestAnimationFrame(bool enableRenderLoop) =>
            Invoke(RequestAnimationFrameSymbol, htmlCanvas, enableRenderLoop);

        public void SetCanvasSize(int rawWidth, int rawHeight) =>
            Invoke(SetCanvasSizeSymbol, htmlCanvas, rawWidth, rawHeight);

        public void PutImageData(IntPtr intPtr, SKSizeI rawSize) =>
            Invoke(PutImageDataSymbol, htmlCanvas, intPtr.ToInt64(), rawSize.Width, rawSize.Height);

        public record GLInfo(int ContextId, uint FboId, int Stencils, int Samples, int Depth);
    }
}
