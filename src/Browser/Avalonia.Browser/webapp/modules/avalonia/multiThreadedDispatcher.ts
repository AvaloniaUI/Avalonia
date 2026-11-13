// Wakes the managed dispatcher pthread directly from the main thread: the dispatcher blocks on an int32
// in shared linear memory (see native/avalonia_browser_dispatcher.c), so a store + notify is enough.
export class MultiThreadedDispatcherHelper {
    private static wakeupPtr = 0;

    public static setWakeupEvent(ptr: number): void {
        MultiThreadedDispatcherHelper.wakeupPtr = ptr;
    }

    public static signal(): void {
        const ptr = MultiThreadedDispatcherHelper.wakeupPtr;
        if (!ptr) {
            return;
        }
        // The view must be re-read each time, memory growth replaces the buffer.
        const heap: Int32Array = (globalThis as any).getDotnetRuntime(0).localHeapViewI32();
        Atomics.store(heap, ptr >>> 2, 1);
        Atomics.notify(heap, ptr >>> 2, 1);
    }
}
