export class TimerHelper {
    public static runAnimationFrames(renderFrameCallback: (timestamp: number) => boolean): void {
        function render(time: number) {
            const next = renderFrameCallback(time);
            if (next) {
                self.requestAnimationFrame(render);
            }
        }

        self.requestAnimationFrame(render);
    }
}
