#import <AppKit/AppKit.h>

class CppAutoreleasePool
{
    void* _pool;
public:
    CppAutoreleasePool();
    ~CppAutoreleasePool();
};

#define START_ARP_CALL CppAutoreleasePool __autoreleasePool