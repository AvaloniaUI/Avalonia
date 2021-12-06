using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SkiaSharp;

namespace Avalonia.Web.Blazor.Interop
{
    internal class SizeWatcherInterop : JSModuleInterop
    {
        private const string JsFilename = "./_content/Avalonia.Web.Blazor/SizeWatcher.js";
        private const string ObserveSymbol = "SizeWatcher.observe";
        private const string UnobserveSymbol = "SizeWatcher.unobserve";

        private readonly ElementReference htmlElement;
        private readonly string htmlElementId;
        private readonly FloatFloatActionHelper callbackHelper;

        private DotNetObjectReference<FloatFloatActionHelper>? callbackReference;

        public static async Task<SizeWatcherInterop> ImportAsync(IJSRuntime js, ElementReference element, Action<SKSize> callback)
        {
            var interop = new SizeWatcherInterop(js, element, callback);
            await interop.ImportAsync();
            interop.Start();
            return interop;
        }

        public SizeWatcherInterop(IJSRuntime js, ElementReference element, Action<SKSize> callback)
            : base(js, JsFilename)
        {
            htmlElement = element;
            htmlElementId = element.Id;
            callbackHelper = new FloatFloatActionHelper((x, y) => callback(new SKSize(x, y)));
        }

        protected override void OnDisposingModule() =>
            Stop();

        public void Start()
        {
            if (callbackReference != null)
                return;

            callbackReference = DotNetObjectReference.Create(callbackHelper);

            Invoke(ObserveSymbol, htmlElement, htmlElementId, callbackReference);
        }

        public void Stop()
        {
            if (callbackReference == null)
                return;

            Invoke(UnobserveSymbol, htmlElementId);

            callbackReference?.Dispose();
            callbackReference = null;
        }
    }
}
