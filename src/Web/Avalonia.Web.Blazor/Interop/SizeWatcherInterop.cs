using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SkiaSharp;

namespace Avalonia.Web.Blazor.Interop
{
    internal class SizeWatcherInterop : IDisposable
    {
        private const string ObserveSymbol = "SizeWatcher.observe";
        private const string UnobserveSymbol = "SizeWatcher.unobserve";

        private readonly AvaloniaModule _module;
        private readonly ElementReference _htmlElement;
        private readonly string _htmlElementId;
        private readonly FloatFloatActionHelper _callbackHelper;

        private DotNetObjectReference<FloatFloatActionHelper>? callbackReference;

        public SizeWatcherInterop(AvaloniaModule module, ElementReference element, Action<SKSize> callback)
        {
            _module = module;
            _htmlElement = element;
            _htmlElementId = element.Id;
            _callbackHelper = new FloatFloatActionHelper((x, y) => callback(new SKSize(x, y)));
        }

        public void Dispose() => Stop();

        public void Start()
        {
            if (callbackReference != null)
                return;

            callbackReference = DotNetObjectReference.Create(_callbackHelper);

            _module.Invoke(ObserveSymbol, _htmlElement, _htmlElementId, callbackReference);
        }

        public void Stop()
        {
            if (callbackReference == null)
                return;

            _module.Invoke(UnobserveSymbol, _htmlElementId);

            callbackReference?.Dispose();
            callbackReference = null;
        }
    }
}
