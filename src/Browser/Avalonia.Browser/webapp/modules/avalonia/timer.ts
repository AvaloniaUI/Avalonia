import { JsExports } from "./jsExports";

export class TimerHelper {
    public static runAnimationFrames(): void {
        function render(time: number) {
            JsExports.TimerHelper?.JsExportOnAnimationFrame(time);
            self.requestAnimationFrame(render);
        }
        self.requestAnimationFrame(render);
    }
}
