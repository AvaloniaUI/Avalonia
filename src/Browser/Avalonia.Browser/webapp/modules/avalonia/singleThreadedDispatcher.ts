import { JsExports } from "./jsExports";

export type Post = () => void;

// Returns a function that queues `callback` as a macrotask:
// postTask where available, else MessageChannel (no nested-timer clamp), else setTimeout.
export function createPost(priority: "user-blocking" | "user-visible" | "background", callback: () => void): Post {
    const scheduler = (globalThis as any).scheduler;
    if (typeof scheduler?.postTask === "function") {
        return () => { scheduler.postTask(callback, { priority }); };
    }
    if (typeof MessageChannel === "function") {
        const channel = new MessageChannel();
        channel.port1.onmessage = callback;
        return () => channel.port2.postMessage(null);
    }
    return () => { setTimeout(callback, 0); };
}

// Scheduling primitives for the single-threaded dispatcher. Mechanism selection happens once here;
// the managed side only sees signal, background continuation and a one-shot timer.
export class SingleThreadedDispatcherHelper {
    private static readonly postSignal: Post = createPost(
        "user-blocking", () => JsExports.SingleThreadedDispatcherImpl?.OnSignaled());

    private static readonly postBackground: Post = createPost(
        "user-visible", () => JsExports.SingleThreadedDispatcherImpl?.OnReadyForBackgroundProcessing());

    private static timerId: number | undefined;

    public static signal(): void {
        SingleThreadedDispatcherHelper.postSignal();
    }

    public static requestBackgroundProcessing(): void {
        SingleThreadedDispatcherHelper.postBackground();
    }

    public static setTimer(delayMs: number): void {
        SingleThreadedDispatcherHelper.clearTimer();
        SingleThreadedDispatcherHelper.timerId = setTimeout(() => {
            SingleThreadedDispatcherHelper.timerId = undefined;
            JsExports.SingleThreadedDispatcherImpl?.OnTimer();
        }, delayMs);
    }

    public static clearTimer(): void {
        if (SingleThreadedDispatcherHelper.timerId !== undefined) {
            clearTimeout(SingleThreadedDispatcherHelper.timerId);
            SingleThreadedDispatcherHelper.timerId = undefined;
        }
    }
}
