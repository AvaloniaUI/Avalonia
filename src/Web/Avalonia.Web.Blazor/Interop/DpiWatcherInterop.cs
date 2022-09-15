using Microsoft.JSInterop;

namespace Avalonia.Web.Blazor.Interop
{
    internal class DpiWatcherInterop : IDisposable
    {
        private const string StartSymbol = "DpiWatcher.start";
        private const string StopSymbol = "DpiWatcher.stop";
        private const string GetDpiSymbol = "DpiWatcher.getDpi";

        private event Action<double>? callbacksEvent;
        private readonly FloatFloatActionHelper _callbackHelper;
        private readonly AvaloniaModule _module;

        private DotNetObjectReference<FloatFloatActionHelper>? callbackReference;

        public DpiWatcherInterop(AvaloniaModule module, Action<double>? callback = null)
        {
            _module = module;
            _callbackHelper = new FloatFloatActionHelper((o, n) => callbacksEvent?.Invoke(n));

            if (callback != null)
                Subscribe(callback);
        }

        public void Dispose() => Stop();

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

            callbackReference = DotNetObjectReference.Create(_callbackHelper);

            return _module.Invoke<double>(StartSymbol, callbackReference);
        }

        private void Stop()
        {
            if (callbackReference == null)
                return;

            _module.Invoke(StopSymbol);

            callbackReference?.Dispose();
            callbackReference = null;
        }

        public double GetDpi() => _module.Invoke<double>(GetDpiSymbol);
    }
}
