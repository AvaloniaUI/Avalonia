// Wake-up primitive for the multithreaded dispatcher: an int32 in linear memory that JS on the main thread
// can signal with Atomics.store + Atomics.notify, and the dispatcher pthread blocks on with atomic.wait32.
#include <stdint.h>
#include <stdlib.h>

int32_t* avn_wakeup_create(void)
{
    return (int32_t*)calloc(1, sizeof(int32_t));
}

void avn_wakeup_destroy(int32_t* p)
{
    free(p);
}

void avn_wakeup_set(int32_t* p)
{
    __atomic_store_n(p, 1, __ATOMIC_SEQ_CST);
    __builtin_wasm_memory_atomic_notify(p, 1);
}

// Returns 1 when signaled, 0 on timeout. timeout_ns < 0 waits forever.
int32_t avn_wakeup_wait(int32_t* p, int64_t timeout_ns)
{
    for (;;)
    {
        if (__atomic_exchange_n(p, 0, __ATOMIC_SEQ_CST) == 1)
            return 1;
        int r = __builtin_wasm_memory_atomic_wait32(p, 0, timeout_ns);
        // 0: notified, 1: value already changed, 2: timed out
        if (r == 2)
            return __atomic_exchange_n(p, 0, __ATOMIC_SEQ_CST) == 1;
    }
}
