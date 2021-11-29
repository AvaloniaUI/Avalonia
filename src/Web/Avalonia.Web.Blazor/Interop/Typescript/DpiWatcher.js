export class DpiWatcher {
    static getDpi() {
        return window.devicePixelRatio;
    }
    static start(callback) {
        //console.info(`Starting DPI watcher with callback ${callback._id}...`);
        DpiWatcher.lastDpi = window.devicePixelRatio;
        DpiWatcher.timerId = window.setInterval(DpiWatcher.update, 1000);
        DpiWatcher.callback = callback;
        return DpiWatcher.lastDpi;
    }
    static stop() {
        //console.info(`Stopping DPI watcher with callback ${DpiWatcher.callback._id}...`);
        window.clearInterval(DpiWatcher.timerId);
        DpiWatcher.callback = undefined;
    }
    static update() {
        if (!DpiWatcher.callback)
            return;
        const currentDpi = window.devicePixelRatio;
        const lastDpi = DpiWatcher.lastDpi;
        DpiWatcher.lastDpi = currentDpi;
        if (Math.abs(lastDpi - currentDpi) > 0.001) {
            DpiWatcher.callback.invokeMethod('Invoke', lastDpi, currentDpi);
        }
    }
}
//# sourceMappingURL=DpiWatcher.js.map