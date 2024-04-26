import { BrowserRenderingMode } from "./surfaceBase";
import { WebGlRenderTarget } from "./webGlRenderTarget";

export interface WebRenderTarget {
    renderTargetType: string;
}

export class WebRenderTargetRegistry {
    private static targets: { [id: number]: (WebRenderTarget) } = {};
    private static registry: { [id: number]: ({
        canvas: HTMLCanvasElement;
        worker?: Worker;
    }); } = {};

    private static nextId = 1;

    static create(pthreadId: number, canvas: HTMLCanvasElement, preferredModes: BrowserRenderingMode[]): number {
        const id = WebRenderTargetRegistry.nextId++;
        if (pthreadId === 0) {
            WebRenderTargetRegistry.registry[id] = {
                canvas
            };
            WebRenderTargetRegistry.targets[id] = WebRenderTargetRegistry.createRenderTarget(canvas, preferredModes);
        } else {
            const self = globalThis as any;
            const module = self.Module ?? self.getDotnetRuntime(0)?.Module;
            const pthread = module?.PThread;
            if (pthread == null) { throw new Error("Unable to access emscripten PThread api"); }
            const worker = pthread.pthreads[pthreadId]?.worker as Worker;
            if (worker == null) { throw new Error(`Unable get Worker for pthread ${pthreadId}`); }
            const offscreen = canvas.transferControlToOffscreen();
            worker.postMessage({
                avaloniaCmd: "registerCanvas",
                canvas: offscreen,
                modes: preferredModes,
                id
            });
            WebRenderTargetRegistry.registry[id] = {
                canvas,
                worker
            };
        }
        return id;
    }

    static initializeWorker() {
        self.addEventListener("onmessage", ev => {
            const msg = ev as MessageEvent;
            if (msg.data.avaloniaCmd === "registerCanvas") {
                WebRenderTargetRegistry.targets[msg.data.id] = WebRenderTargetRegistry.createRenderTarget(msg.data.canvas, msg.data.modes);
            }
            if (msg.data.avaloniaCmd === "unregisterCanvas") {
                /* eslint-disable */
                // Our keys are _always_ numbers and are safe to delete
                delete WebRenderTargetRegistry.targets[msg.data.id];
                /* eslint-enable */
            }
        });
    }

    static getRenderTarget(id: number): WebRenderTarget | undefined {
        return WebRenderTargetRegistry.targets[id];
    }

    private static createRenderTarget(canvas: HTMLCanvasElement | OffscreenCanvas, modes: BrowserRenderingMode[]): WebRenderTarget {
        return new WebGlRenderTarget(canvas, BrowserRenderingMode.WebGL1);
    }
}
