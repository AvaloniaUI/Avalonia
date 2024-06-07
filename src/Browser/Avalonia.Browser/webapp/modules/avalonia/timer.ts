import { JsExports } from "./jsExports";

export class TimerHelper {
    public static runAnimationFrames(): void {
        function render(time: number) {
            JsExports.TimerHelper?.JsExportOnAnimationFrame();
            self.requestAnimationFrame(render);
        }
        self.requestAnimationFrame(render);
    }

    static onTimeout() {
        JsExports.TimerHelper?.JsExportOnTimeout();
    }

    static onInterval() {
        JsExports.TimerHelper?.JsExportOnInterval();
    }

    public static setTimeout(interval: number): number {
        return setTimeout(TimerHelper.onTimeout, interval);
    }

    public static setInterval(interval: number): number {
        return setInterval(TimerHelper.onInterval, interval);
    }
}
