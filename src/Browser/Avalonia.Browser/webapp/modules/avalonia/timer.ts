import { JsExports } from "./jsExports";

export class TimerHelper {
    public static runAnimationFrames(): void {
        function render(time: number) {
            if (JsExports.resolvedExports != null) {
                JsExports.resolvedExports.Avalonia.Browser.Interop.TimerHelper.JsExportOnAnimationFrame(time);
            }
            self.requestAnimationFrame(render);
        }
        self.requestAnimationFrame(render);
    }

    static onTimeout() {
        if (JsExports.resolvedExports != null) {
            JsExports.resolvedExports.Avalonia.Browser.Interop.TimerHelper.JsExportOnTimeout();
        } else { console.error("TimerHelper.onTimeout call while uninitialized"); }
    }

    static onInterval() {
        if (JsExports.resolvedExports != null) {
            JsExports.resolvedExports.Avalonia.Browser.Interop.TimerHelper.JsExportOnInterval();
        } else { console.error("TimerHelper.onInterval call while uninitialized"); }
    }

    public static setTimeout(interval: number): number {
        return setTimeout(TimerHelper.onTimeout, interval);
    }

    public static setInterval(interval: number): number {
        return setInterval(TimerHelper.onInterval, interval);
    }
}
