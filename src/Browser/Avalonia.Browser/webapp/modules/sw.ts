const WRITE = 0;
const PULL = 0;
const ERROR = 1;
const ABORT = 1;
const CLOSE = 2;

class MessagePortSource implements UnderlyingSource {
    private controller?: ReadableStreamController<any>;

    constructor (private readonly port: MessagePort) {
        this.port.onmessage = evt => this.onMessage(evt.data);
    }

    start (controller: ReadableStreamController<any>) {
        this.controller = controller;
    }

    cancel (reason: Error) {
    // Firefox can notify a cancel event, chrome can't
    // https://bugs.chromium.org/p/chromium/issues/detail?id=638494
        this.port.postMessage({ type: ERROR, reason: reason.message });
        this.port.close();
    }

    onMessage (message: { type: number; chunk: Uint8Array; reason: any }) {
    // enqueue() will call pull() if needed when there's no backpressure
        if (!this.controller) {
            return;
        }
        if (message.type === WRITE) {
            this.controller.enqueue(message.chunk);
            this.port.postMessage({ type: PULL });
        }
        if (message.type === ABORT) {
            this.controller.error(message.reason);
            this.port.close();
        }
        if (message.type === CLOSE) {
            this.controller.close();
            this.port.close();
        }
    }
}

self.addEventListener("install", () => {
    (self as any).skipWaiting();
});

self.addEventListener("activate", event /* ExtendableEvent */ => {
    (event as any).waitUntil((self as any).clients.claim());
});

(self as any).map = new Map();

// This should be called once per download
// Each event has a dataChannel that the data will be piped through
globalThis.addEventListener("message", evt => {
    const data = evt.data;
    if (data.url && data.readablePort) {
        data.rs = new ReadableStream(
            new MessagePortSource(evt.data.readablePort),
            new CountQueuingStrategy({ highWaterMark: 4 })
        );
        const map = (self as any).map;
        map.set(data.url, data);
    }
});

globalThis.addEventListener("fetch", evt => {
    const url = (evt as any).request.url;
    const map = (self as any).map;
    const data = map.get(url);
    if (!data) return null;
    map.delete(url);
    (evt as any).respondWith(new Response(data.rs, {
        headers: data.headers
    }));
});

export {};
