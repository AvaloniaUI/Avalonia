using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Avalonia.Web.Blazor.Interop
{
    internal class DpiWatcherInterop : JSModuleInterop
    {
        private const string JsFilename = "./_content/Avalonia.Web.Blazor/DpiWatcher.js";
        private const string StartSymbol = "DpiWatcher.start";
        private const string StopSymbol = "DpiWatcher.stop";
        private const string GetDpiSymbol = "DpiWatcher.getDpi";

        private static DpiWatcherInterop? instance;

        private event Action<double>? callbacksEvent;
        private readonly FloatFloatActionHelper callbackHelper;

        private DotNetObjectReference<FloatFloatActionHelper>? callbackReference;

        public static async Task<DpiWatcherInterop> ImportAsync(IJSRuntime js, Action<double>? callback = null)
        {
            var interop = Get(js);
            await interop.ImportAsync();
            if (callback != null)
                interop.Subscribe(callback);
            return interop;
        }

        public static DpiWatcherInterop Get(IJSRuntime js) =>
            instance ??= new DpiWatcherInterop(js);

        private DpiWatcherInterop(IJSRuntime js)
            : base(js, JsFilename)
        {
            callbackHelper = new FloatFloatActionHelper((o, n) => callbacksEvent?.Invoke(n));
        }

        protected override void OnDisposingModule() =>
            Stop();

        public void Subscribe(Action<double> callback)
        {
            var shouldStart = callbacksEvent == null;

            callbacksEvent += callback;

            var dpi = shouldStart
                ? Start()
                : GetDpi();

            callback(dpi);
        }

        public void Unsubscribe(Action<double> callback)
        {
            callbacksEvent -= callback;

            if (callbacksEvent == null)
                Stop();
        }

        private double Start()
        {
            if (callbackReference != null)
                return GetDpi();

            callbackReference = DotNetObjectReference.Create(callbackHelper);

            return Invoke<double>(StartSymbol, callbackReference);
        }

        private void Stop()
        {
            if (callbackReference == null)
                return;

            Invoke(StopSymbol);

            callbackReference?.Dispose();
            callbackReference = null;
        }

        public double GetDpi() =>
            Invoke<double>(GetDpiSymbol);
    }
}
