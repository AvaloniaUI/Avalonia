#include "noarc.h"

CppAutoreleasePool::CppAutoreleasePool()
{
    _pool = [[NSAutoreleasePool alloc] init];
}

CppAutoreleasePool::~CppAutoreleasePool() {
    auto ptr = (NSAutoreleasePool*)_pool;
    [ptr release];
}
