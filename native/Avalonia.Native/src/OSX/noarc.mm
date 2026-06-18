#include "noarc.h"
#include "avalonia-native.h"
CppAutoreleasePool::CppAutoreleasePool()
{
    _pool = [[NSAutoreleasePool alloc] init];
}

CppAutoreleasePool::~CppAutoreleasePool() {
    auto ptr = (NSAutoreleasePool*)_pool;
    [ptr release];
}

extern void ReleaseNSObject(void* obj)
{
    [(NSObject*)obj release];
}
extern void RetainNSObject(void* obj)
{
    [(NSObject*)obj retain];
}

extern uint64_t GetRetainCountForNSObject(void* obj)
{
    return [(NSObject*)obj retainCount];
}
