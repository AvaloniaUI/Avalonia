export class TimerHelper {
    public static runAnimationFrames(renderFrameCallback: (timestamp: number) => boolean): void {
        function render(time: number) {
            const next = renderFrameCallback(time);
            if (next) {
                window.requestAnimationFrame(render);
            }
        }

        window.requestAnimationFrame(render);
    }
}
