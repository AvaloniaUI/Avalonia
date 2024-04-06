type ResizeObserverWithCallbacks = {
    callbacks: Map<Element, ((width: number, height: number, dpr: number) => void)>;
} & ResizeObserver;

export class ResizeHandler {
    private static resizeObserver?: ResizeObserverWithCallbacks;

    public static observeSize(element: HTMLElement, callback: (width: number, height: number, dpr: number) => void) : (() => void) {
        if (!this.resizeObserver) {
            this.resizeObserver = new ResizeObserver(this.onResize) as ResizeObserverWithCallbacks;
            this.resizeObserver.callbacks = new Map<Element, ((width: number, height: number, dpr: number) => void)>();
        }

        this.resizeObserver.callbacks.set(element, callback);
        this.resizeObserver.observe(element, { box: "content-box" });

        return () => {
            this.resizeObserver?.callbacks.delete(element);
            this.resizeObserver?.unobserve(element);
        };
    }

    private static onResize(entries: ResizeObserverEntry[], observer: ResizeObserver) {
        for (const entry of entries) {
            const callback = (observer as ResizeObserverWithCallbacks).callbacks.get(entry.target);
            if (!callback) {
                continue;
            }

            const trueDpr = window.devicePixelRatio;
            let width;
            let height;
            let dpr = trueDpr;
            if (entry.devicePixelContentBoxSize) {
                // NOTE: Only this path gives the correct answer
                // The other paths are imperfect fallbacks
                // for browsers that don't provide anyway to do this
                width = entry.devicePixelContentBoxSize[0].inlineSize;
                height = entry.devicePixelContentBoxSize[0].blockSize;
                dpr = 1; // it's already in width and height
            } else if (entry.contentBoxSize) {
                if (entry.contentBoxSize[0]) {
                    width = entry.contentBoxSize[0].inlineSize;
                    height = entry.contentBoxSize[0].blockSize;
                } else {
                    width = (entry.contentBoxSize as any).inlineSize;
                    height = (entry.contentBoxSize as any).blockSize;
                }
            } else {
                width = entry.contentRect.width;
                height = entry.contentRect.height;
            }
            const displayWidth = Math.round(width * dpr);
            const displayHeight = Math.round(height * dpr);
            callback(displayWidth, displayHeight, trueDpr);
        }
    }
}
