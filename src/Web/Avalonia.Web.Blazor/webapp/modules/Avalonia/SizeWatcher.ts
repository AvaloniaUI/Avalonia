type SizeWatcherElement = {
    SizeWatcher: SizeWatcherInstance;
} & HTMLElement

type SizeWatcherInstance = {
    callback: DotNet.DotNetObject;
}

export class SizeWatcher {
    static observer: ResizeObserver;
    static elements: Map<string, HTMLElement>;

    public static observe(element: HTMLElement, elementId: string, callback: DotNet.DotNetObject) {
        if (!element || !callback)
            return;

        //console.info(`Adding size watcher observation with callback ${callback._id}...`);

        SizeWatcher.init();

        const watcherElement = element as SizeWatcherElement;
        watcherElement.SizeWatcher = {
            callback: callback
        };

        SizeWatcher.elements.set(elementId, element);
        SizeWatcher.observer.observe(element);

        SizeWatcher.invoke(element);
    }

    public static unobserve(elementId: string) {
        if (!elementId || !SizeWatcher.observer)
            return;

        //console.info('Removing size watcher observation...');

        const element = SizeWatcher.elements.get(elementId)!;

        SizeWatcher.elements.delete(elementId);
        SizeWatcher.observer.unobserve(element);
    }

    static init() {
        if (SizeWatcher.observer)
            return;

        //console.info('Starting size watcher...');

        SizeWatcher.elements = new Map<string, HTMLElement>();
        SizeWatcher.observer = new ResizeObserver((entries) => {
            for (let entry of entries) {
                SizeWatcher.invoke(entry.target);
            }
        });
    }

    static invoke(element: Element) {
        const watcherElement = element as SizeWatcherElement;
        const instance = watcherElement.SizeWatcher;

        if (!instance || !instance.callback)
            return;

        return instance.callback.invokeMethod('Invoke', element.clientWidth, element.clientHeight);
    }
}
