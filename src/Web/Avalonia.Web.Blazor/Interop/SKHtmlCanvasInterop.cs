using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SkiaSharp;

namespace Avalonia.Web.Blazor.Interop
{
    internal class SKHtmlCanvasInterop : IDisposable
    {
        private const string JsFilename = "./_content/Avalonia.Web.Blazor/SKHtmlCanvas.js";
        private const string InitGLSymbol = "SKHtmlCanvas.initGL";
        private const string InitRasterSymbol = "SKHtmlCanvas.initRaster";
        private const string DeinitSymbol = "SKHtmlCanvas.deinit";
        private const string RequestAnimationFrameSymbol = "SKHtmlCanvas.requestAnimationFrame";
        private const string SetCanvasSizeSymbol = "SKHtmlCanvas.setCanvasSize";
        private const string PutImageDataSymbol = "SKHtmlCanvas.putImageData";

        private readonly AvaloniaModule _module;
        private readonly ElementReference _htmlCanvas;
        private readonly string _htmlElementId;
        private readonly ActionHelper _callbackHelper;

        private DotNetObjectReference<ActionHelper>? callbackReference;

        public SKHtmlCanvasInterop(AvaloniaModule module, ElementReference element, Action renderFrameCallback)
        {
            _module = module;
            _htmlCanvas = element;
            _htmlElementId = element.Id;

            _callbackHelper = new ActionHelper(renderFrameCallback);
        }

        public void Dispose() => Deinit();

        public GLInfo InitGL()
        {
            if (callbackReference != null)
                throw new InvalidOperationException("Unable to initialize the same canvas more than once.");

            callbackReference = DotNetObjectReference.Create(_callbackHelper);

            return _module.Invoke<GLInfo>(InitGLSymbol, _htmlCanvas, _htmlElementId, callbackReference);
        }

        public bool InitRaster()
        {
            if (callbackReference != null)
                throw new InvalidOperationException("Unable to initialize the same canvas more than once.");

            callbackReference = DotNetObjectReference.Create(_callbackHelper);

            return _module.Invoke<bool>(InitRasterSymbol, _htmlCanvas, _htmlElementId, callbackReference);
        }

        public void Deinit()
        {
            if (callbackReference == null)
                return;

            _module.Invoke(DeinitSymbol, _htmlElementId);

            callbackReference?.Dispose();
        }

        public void RequestAnimationFrame(bool enableRenderLoop) =>
            _module.Invoke(RequestAnimationFrameSymbol, _htmlCanvas, enableRenderLoop);

        public void SetCanvasSize(int rawWidth, int rawHeight) =>
            _module.Invoke(SetCanvasSizeSymbol, _htmlCanvas, rawWidth, rawHeight);

        public void PutImageData(IntPtr intPtr, SKSizeI rawSize) =>
            _module.Invoke(PutImageDataSymbol, _htmlCanvas, intPtr.ToInt64(), rawSize.Width, rawSize.Height);

        public record GLInfo(int ContextId, uint FboId, int Stencils, int Samples, int Depth);
    }
}
