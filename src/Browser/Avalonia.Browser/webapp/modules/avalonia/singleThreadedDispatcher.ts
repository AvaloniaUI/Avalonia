import { JsExports } from "./jsExports";

type Post = () => void;

// Scheduling primitives for the single-threaded dispatcher. Mechanism selection happens once here;
// the managed side only sees signal, background continuation and a one-shot timer.
export class SingleThreadedDispatcherHelper {
    private static readonly postSignal: Post = SingleThreadedDispatcherHelper.createPost(
        "user-blocking", () => JsExports.SingleThreadedDispatcherImpl?.OnSignaled());

    private static readonly postBackground: Post = SingleThreadedDispatcherHelper.createPost(
        "user-visible", () => JsExports.SingleThreadedDispatcherImpl?.OnReadyForBackgroundProcessing());

    private static timerId: number | undefined;

    // postTask where available, else MessageChannel (no nested-timer clamp), else setTimeout.
    private static createPost(priority: string, callback: () => void): Post {
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
